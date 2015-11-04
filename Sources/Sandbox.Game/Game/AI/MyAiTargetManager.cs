using ProtoBuf;
using Sandbox.Common;
using Sandbox.Engine.Multiplayer;
using Sandbox.Game.AI;
using Sandbox.Game.Entities;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.World;
using SteamSDK;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using VRage.Library.Utils;
using VRage.Utils;
using VRageMath;

namespace Sandbox.Game.AI
{
	[PreloadRequired]
    [MySessionComponentDescriptor(MyUpdateOrder.AfterSimulation)]
    public class MyAiTargetManager : MySessionComponentBase
    {

		#region Sync Messages
		[MessageId(4657, P2PMessageEnum.Reliable)]
		struct ReserveEntityMsg
		{
			public long EntityId;
			public long ReservationTimeMs;
			public int SenderSerialId;
		}

		[MessageId(4658, P2PMessageEnum.Reliable)]
		struct ReserveEnvironmentItemMsg
		{
			public long EntityId;
			public int LocalId;
			public long ReservationTimeMs;
			public int SenderSerialId;
		}

		[MessageId(4659, P2PMessageEnum.Reliable)]
		struct ReserveVoxelPositionMsg
		{
			public long EntityId;
			public Vector3I VoxelPos;
			public long ReservationTimeMs;
			public int SenderSerialId;
		}

        [MessageId(4660, P2PMessageEnum.Reliable)]
        [ProtoContract]
        struct ReserveAreaMsg     
        {
            [ProtoMember]
            public string ReservationName;
            [ProtoMember]
            public Vector3D Position;
            [ProtoMember]
            public float Radius;
            [ProtoMember]
            public long ReservationTimeMs;
            [ProtoMember]
            public int SenderSerialId;
        }

        [MessageId(4661, P2PMessageEnum.Reliable)]
        [ProtoContract]
        struct ReserveAreaAllMsg
        {
            [ProtoMember]
            public string ReservationName;
            [ProtoMember]
            public Vector3D Position;
            [ProtoMember]
            public float Radius;
            [ProtoMember]
            public long Id;
        }

        [MessageId(4662, P2PMessageEnum.Reliable)]
        [ProtoContract]
        struct ReserveAreaCancelMsg
        {
            [ProtoMember]
            public string ReservationName;
            [ProtoMember]
            public long Id;
        }

		#endregion

		public struct ReservedEntityData
		{
			public MyReservedEntityType Type;
			public long EntityId;
			public int LocalId;
			public Vector3I GridPos;
			public long ReservationTimer;
			public MyPlayer.PlayerId ReserverId;
		}

        public struct ReservedAreaData
        {
            public Vector3D WorldPosition;
            public float Radius;
            public MyTimeSpan ReservationTimer;
            public MyPlayer.PlayerId ReserverId;
        }

		private HashSet<MyAiTargetBase> m_aiTargets = new HashSet<MyAiTargetBase>();

		private static Dictionary<KeyValuePair<long, long>, ReservedEntityData> m_reservedEntities;
        private static Dictionary<string, Dictionary<long, ReservedAreaData>> m_reservedAreas;
		private static Queue<KeyValuePair<long, long>> m_removeReservedEntities;
        private static Queue<KeyValuePair<string, long>> m_removeReservedAreas;

        private static long AreaReservationCounter = 0;

		public static MyAiTargetManager Static;
		public delegate void ReservationHandler(ref ReservedEntityData entityData, bool success);
		public static event ReservationHandler OnReservationResult;

        public delegate void AreaReservationHandler(ref ReservedAreaData entityData, bool success);
        public static event AreaReservationHandler OnAreaReservationResult;

		#region Entity reservation

		public bool IsEntityReserved(long entityId, long localId)
		{
			if (!Sync.IsServer)
				return false;

			ReservedEntityData reservedEntity;
			return m_reservedEntities.TryGetValue(new KeyValuePair<long, long>(entityId, localId), out reservedEntity);
		}

		public bool IsEntityReserved(long entityId)
		{
			return IsEntityReserved(entityId, 0);
		}

		public void UnreserveEntity(long entityId, long localId)
		{
			if (!Sync.IsServer)
				return;

			m_reservedEntities.Remove(new KeyValuePair<long, long>(entityId, localId));
		}

		public void UnreserveEntity(long entityId)
		{
			UnreserveEntity(entityId, 0);
		}

		#endregion

		#region Sync callbacks
		private static void OnReserveEntityRequest(ref ReserveEntityMsg msg, MyNetworkClient sender)
		{
			if (!Sync.IsServer)
				return;

			MyTransportMessageEnum responseState = MyTransportMessageEnum.Success;
			ReservedEntityData entityData;
			var entityId = new KeyValuePair<long, long>(msg.EntityId, 0);
			if (m_reservedEntities.TryGetValue(entityId, out entityData))
			{
				if (entityData.ReserverId == new MyPlayer.PlayerId(sender.SteamUserId, msg.SenderSerialId))
					entityData.ReservationTimer = Stopwatch.GetTimestamp() + Stopwatch.Frequency * msg.ReservationTimeMs / 1000;
				else
					responseState = MyTransportMessageEnum.Failure;
			}
			else
				m_reservedEntities.Add(entityId, new ReservedEntityData()
				{
					EntityId = msg.EntityId,
					ReservationTimer = Stopwatch.GetTimestamp() + Stopwatch.Frequency * msg.ReservationTimeMs / 1000,
					ReserverId = new MyPlayer.PlayerId(sender.SteamUserId, msg.SenderSerialId)
				});

			Sync.Layer.SendMessage(ref msg, sender.SteamUserId, responseState);
		}

		private static void OnReserveEntitySuccess(ref ReserveEntityMsg msg, MyNetworkClient sender)
		{
			if (OnReservationResult != null)
			{
				var reservationData = new ReservedEntityData() { Type = MyReservedEntityType.ENTITY, EntityId = msg.EntityId, ReserverId = new MyPlayer.PlayerId(0, msg.SenderSerialId) };
				OnReservationResult(ref reservationData, true);
			}
		}

		private static void OnReserveEntityFailure(ref ReserveEntityMsg msg, MyNetworkClient sender)
		{
			if (OnReservationResult != null)
			{
				var reservationData = new ReservedEntityData() { Type = MyReservedEntityType.ENTITY, EntityId = msg.EntityId, ReserverId = new MyPlayer.PlayerId(0, msg.SenderSerialId) };
				OnReservationResult(ref reservationData, false);
			}
		}

		private static void OnReserveEnvironmentItemRequest(ref ReserveEnvironmentItemMsg msg, MyNetworkClient sender)
		{
			if (!Sync.IsServer)
				return;

			MyTransportMessageEnum responseState = MyTransportMessageEnum.Success;
			ReservedEntityData entityData;
			var entityId = new KeyValuePair<long, long>(msg.EntityId, msg.LocalId);
			if (m_reservedEntities.TryGetValue(entityId, out entityData))
			{
				if (entityData.ReserverId == new MyPlayer.PlayerId(sender.SteamUserId, msg.SenderSerialId))
					entityData.ReservationTimer = Stopwatch.GetTimestamp() + Stopwatch.Frequency * msg.ReservationTimeMs / 1000;
				else
					responseState = MyTransportMessageEnum.Failure;
			}
			else
				m_reservedEntities.Add(entityId, new ReservedEntityData()
				{
					EntityId = msg.EntityId,
					LocalId = msg.LocalId,
					ReservationTimer = Stopwatch.GetTimestamp() + Stopwatch.Frequency * msg.ReservationTimeMs / 1000,
					ReserverId = new MyPlayer.PlayerId(sender.SteamUserId, msg.SenderSerialId)
				});

			Sync.Layer.SendMessage(ref msg, sender.SteamUserId, responseState);
		}

		private static void OnReserveEnvironmentItemSuccess(ref ReserveEnvironmentItemMsg msg, MyNetworkClient sender)
		{
			if (OnReservationResult != null)
			{
				var reservationData = new ReservedEntityData()
				{
					Type = MyReservedEntityType.ENVIRONMENT_ITEM,
					EntityId = msg.EntityId,
					LocalId = msg.LocalId,
					ReserverId = new MyPlayer.PlayerId(0, msg.SenderSerialId)
				};
				OnReservationResult(ref reservationData, true);
			}
		}

		private static void OnReserveEnvironmentItemFailure(ref ReserveEnvironmentItemMsg msg, MyNetworkClient sender)
		{
			if (OnReservationResult != null)
			{
				var reservationData = new ReservedEntityData()
				{
					Type = MyReservedEntityType.ENVIRONMENT_ITEM,
					EntityId = msg.EntityId,
					LocalId = msg.LocalId,
					ReserverId = new MyPlayer.PlayerId(0, msg.SenderSerialId)
				};
				OnReservationResult(ref reservationData, false);
			}
		}

		private static void OnReserveVoxelPositionRequest(ref ReserveVoxelPositionMsg msg, MyNetworkClient sender)
		{
			if (!Sync.IsServer)
				return;

			MyTransportMessageEnum responseState = MyTransportMessageEnum.Success;
			ReservedEntityData entityData;
			MyVoxelBase voxelMap = null;
			if (!MySession.Static.VoxelMaps.Instances.TryGetValue(msg.EntityId, out voxelMap))
				return;

			Vector3I voxelMapSize = voxelMap.StorageMax - voxelMap.StorageMin;
			Debug.Assert(voxelMapSize.AbsMax() < 2 * 10E6, "Voxel map size too large to reserve unique voxel position");
			// Integer overflow won't even happen on the next line for voxel maps smaller than roughly (2.5M)^3, and most Vector3I member functions have broken down way before that
			var entityId = new KeyValuePair<long, long>(msg.EntityId, msg.VoxelPos.X + msg.VoxelPos.Y * voxelMapSize.X + msg.VoxelPos.Z * voxelMapSize.X * voxelMapSize.Y);
			if (m_reservedEntities.TryGetValue(entityId, out entityData))
			{
				if (entityData.ReserverId == new MyPlayer.PlayerId(sender.SteamUserId, msg.SenderSerialId))
					entityData.ReservationTimer = Stopwatch.GetTimestamp() + Stopwatch.Frequency * msg.ReservationTimeMs / 1000;
				else
					responseState = MyTransportMessageEnum.Failure;
			}
			else
				m_reservedEntities.Add(entityId, new ReservedEntityData()
				{
					EntityId = msg.EntityId,
					GridPos = msg.VoxelPos,
					ReservationTimer = Stopwatch.GetTimestamp() + Stopwatch.Frequency * msg.ReservationTimeMs / 1000,
					ReserverId = new MyPlayer.PlayerId(sender.SteamUserId, msg.SenderSerialId)
				});

			Sync.Layer.SendMessage(ref msg, sender.SteamUserId, responseState);
		}

		private static void OnReserveVoxelPositionSuccess(ref ReserveVoxelPositionMsg msg, MyNetworkClient sender)
		{
			if (OnReservationResult != null)
			{
				var reservationData = new ReservedEntityData()
				{
					Type = MyReservedEntityType.VOXEL,
					EntityId = msg.EntityId,
					GridPos = msg.VoxelPos,
					ReserverId = new MyPlayer.PlayerId(0, msg.SenderSerialId)
				};
				OnReservationResult(ref reservationData, true);
			}
		}

		private static void OnReserveVoxelPositionFailure(ref ReserveVoxelPositionMsg msg, MyNetworkClient sender)
		{
			if (OnReservationResult != null)
			{
				var reservationData = new ReservedEntityData()
				{
					Type = MyReservedEntityType.VOXEL,
					EntityId = msg.EntityId,
					GridPos = msg.VoxelPos,
					ReserverId = new MyPlayer.PlayerId(0, msg.SenderSerialId)
				};
				OnReservationResult(ref reservationData, false);
			}
		}

		public void RequestEntityReservation(long entityId, long reservationTimeMs, int senderSerialId)
		{
			var msg = new ReserveEntityMsg()
			{
				EntityId = entityId,
				ReservationTimeMs = reservationTimeMs,
				SenderSerialId = senderSerialId,
			};
			Sync.Layer.SendMessageToServer(ref msg, MyTransportMessageEnum.Request);
		}

		public void RequestEnvironmentItemReservation(long entityId, int localId, long reservationTimeMs, int senderSerialId)
		{
			var msg = new ReserveEnvironmentItemMsg()
			{
				EntityId = entityId,
				LocalId = localId,
				ReservationTimeMs = reservationTimeMs,
				SenderSerialId = senderSerialId
			};
			Sync.Layer.SendMessageToServer(ref msg, MyTransportMessageEnum.Request);
		}

		public void RequestVoxelPositionReservation(long entityId, Vector3I voxelPosition, long reservationTimeMs, int senderSerialId)
		{
			var msg = new ReserveVoxelPositionMsg()
			{
				EntityId = entityId,
				VoxelPos = voxelPosition,
				ReservationTimeMs = reservationTimeMs,
				SenderSerialId = senderSerialId,
			};
			Sync.Layer.SendMessageToServer(ref msg, MyTransportMessageEnum.Request);
		}

		#endregion

        #region Area reservation

        public void RequestAreaReservation(string reservationName, Vector3D position, float radius, long reservationTimeMs, int senderSerialId)
        {
            var msg = new ReserveAreaMsg()
            {
                ReservationName = reservationName,
                Position = position,
                Radius = radius,
                ReservationTimeMs = reservationTimeMs,
                SenderSerialId = senderSerialId,
            };
            Sync.Layer.SendMessageToServer(ref msg, MyTransportMessageEnum.Request);
        }

        private static void OnReserveAreaRequest(ref ReserveAreaMsg msg, MyNetworkClient sender)
        {
            if (!Sync.IsServer)
                return;

            if (!m_reservedAreas.ContainsKey(msg.ReservationName))
                m_reservedAreas.Add(msg.ReservationName, new Dictionary<long, ReservedAreaData>());

            var reservations = m_reservedAreas[msg.ReservationName];
            bool reservedBySomeone = false;
            MyPlayer.PlayerId requestId = new MyPlayer.PlayerId(sender.SteamUserId, msg.SenderSerialId);

            foreach (var r in reservations)
            {
                var currentReservation = r.Value;
                var sqDist = (currentReservation.WorldPosition - msg.Position).LengthSquared();
                bool inRadius = sqDist <= currentReservation.Radius * currentReservation.Radius; 
                
                if (inRadius)
                {
                    reservedBySomeone = true;
                    break;
                }
            }

            if (!reservedBySomeone)
            {
                reservations[AreaReservationCounter++] = new ReservedAreaData()
                {
                    WorldPosition = msg.Position,
                    Radius = msg.Radius,
                    ReservationTimer = MySandboxGame.Static.UpdateTime + MyTimeSpan.FromMiliseconds(msg.ReservationTimeMs),
                    ReserverId = requestId,
                };

                var allMsg = new ReserveAreaAllMsg()
                {
                    Id = AreaReservationCounter,
                    Position = msg.Position,
                    Radius = msg.Radius,
                    ReservationName = msg.ReservationName,
                };

                Sync.Layer.SendMessageToAll(ref allMsg, MyTransportMessageEnum.Success);
                Sync.Layer.SendMessage(ref msg, sender.SteamUserId, MyTransportMessageEnum.Success);
            }
            else
            {
                Sync.Layer.SendMessage(ref msg, sender.SteamUserId, MyTransportMessageEnum.Failure);
            }

            
        }

        private static void OnReserveAreaSuccess(ref ReserveAreaMsg msg, MyNetworkClient sender)
        {
            if (OnAreaReservationResult != null)
            {
                var reservationData = new ReservedAreaData()
                {
                    WorldPosition = msg.Position,
                    Radius = msg.Radius,
                    ReserverId = new MyPlayer.PlayerId(0, msg.SenderSerialId)
                };
                OnAreaReservationResult(ref reservationData, true);
            }
        }

        private static void OnReserveAreaFailure(ref ReserveAreaMsg msg, MyNetworkClient sender)
        {
            if (OnAreaReservationResult != null)
            {
                var reservationData = new ReservedAreaData()
                {
                    WorldPosition = msg.Position,
                    Radius = msg.Radius,
                    ReserverId = new MyPlayer.PlayerId(0, msg.SenderSerialId)
                };
                OnAreaReservationResult(ref reservationData, false);
            }
        }

        private static void OnReserveAreaAllSuccess(ref ReserveAreaAllMsg msg, MyNetworkClient sender)
        {
            if (!m_reservedAreas.ContainsKey(msg.ReservationName))
                m_reservedAreas[msg.ReservationName] = new Dictionary<long, ReservedAreaData>();
            var reservations = m_reservedAreas[msg.ReservationName];
            reservations.Add(msg.Id, new ReservedAreaData() { WorldPosition = msg.Position, Radius = msg.Radius });
        }

        private static void OnReserveAreaCancel(ref ReserveAreaCancelMsg msg, MyNetworkClient sender)
        {
            Dictionary<long, ReservedAreaData> reservations;
            if (m_reservedAreas.TryGetValue(msg.ReservationName, out reservations))
            {
                reservations.Remove(msg.Id);
            }
        }

        #endregion

        static MyAiTargetManager()
		{
			MySyncLayer.RegisterMessage<ReserveEntityMsg>(OnReserveEntityRequest, MyMessagePermissions.ToServer, MyTransportMessageEnum.Request);
			MySyncLayer.RegisterMessage<ReserveEntityMsg>(OnReserveEntitySuccess, MyMessagePermissions.FromServer, MyTransportMessageEnum.Success);
			MySyncLayer.RegisterMessage<ReserveEntityMsg>(OnReserveEntityFailure, MyMessagePermissions.FromServer, MyTransportMessageEnum.Failure);
			MySyncLayer.RegisterMessage<ReserveEnvironmentItemMsg>(OnReserveEnvironmentItemRequest, MyMessagePermissions.ToServer, MyTransportMessageEnum.Request);
			MySyncLayer.RegisterMessage<ReserveEnvironmentItemMsg>(OnReserveEnvironmentItemSuccess, MyMessagePermissions.FromServer, MyTransportMessageEnum.Success);
			MySyncLayer.RegisterMessage<ReserveEnvironmentItemMsg>(OnReserveEnvironmentItemFailure, MyMessagePermissions.FromServer, MyTransportMessageEnum.Failure);
			MySyncLayer.RegisterMessage<ReserveVoxelPositionMsg>(OnReserveVoxelPositionRequest, MyMessagePermissions.ToServer, MyTransportMessageEnum.Request);
			MySyncLayer.RegisterMessage<ReserveVoxelPositionMsg>(OnReserveVoxelPositionSuccess, MyMessagePermissions.FromServer, MyTransportMessageEnum.Success);
			MySyncLayer.RegisterMessage<ReserveVoxelPositionMsg>(OnReserveVoxelPositionFailure, MyMessagePermissions.FromServer, MyTransportMessageEnum.Failure);
            MySyncLayer.RegisterMessage<ReserveAreaMsg>(OnReserveAreaRequest, MyMessagePermissions.ToServer, MyTransportMessageEnum.Request);
            MySyncLayer.RegisterMessage<ReserveAreaMsg>(OnReserveAreaSuccess, MyMessagePermissions.FromServer, MyTransportMessageEnum.Success);
            MySyncLayer.RegisterMessage<ReserveAreaMsg>(OnReserveAreaFailure, MyMessagePermissions.FromServer, MyTransportMessageEnum.Failure);
            MySyncLayer.RegisterMessage<ReserveAreaAllMsg>(OnReserveAreaAllSuccess, MyMessagePermissions.FromServer, MyTransportMessageEnum.Success);
            MySyncLayer.RegisterMessage<ReserveAreaCancelMsg>(OnReserveAreaCancel, MyMessagePermissions.FromServer, MyTransportMessageEnum.Success);
		}

		public override void LoadData()
        {
            Static = this;

			if (Sync.IsServer)
			{
				m_reservedEntities = new Dictionary<KeyValuePair<long, long>, ReservedEntityData>();
				m_removeReservedEntities = new Queue<KeyValuePair<long, long>>();
                m_removeReservedAreas = new Queue<KeyValuePair<string, long>>();
			}

            m_reservedAreas = new Dictionary<string, Dictionary<long, ReservedAreaData>>();

            MyEntities.OnEntityRemove += OnEntityRemoved;
        }

		protected override void UnloadData()
        {
            base.UnloadData();
            m_aiTargets.Clear();
            MyEntities.OnEntityRemove -= OnEntityRemoved;
            Static = null;
        }

		public override bool IsRequiredByGame
		{
			get
			{
                return true;
			}
		}

		public override void UpdateAfterSimulation()
		{
			base.UpdateAfterSimulation();

			if (Sync.IsServer)
			{
				foreach (var entity in m_reservedEntities)
				{
					if (Stopwatch.GetTimestamp() > entity.Value.ReservationTimer)
						m_removeReservedEntities.Enqueue(entity.Key);
				}

				foreach (var id in m_removeReservedEntities)
				{
					m_reservedEntities.Remove(id);
				}

                m_removeReservedEntities.Clear();

                foreach (var tag in m_reservedAreas)
                {
                    foreach (var area in tag.Value)
                    {
                        if (MySandboxGame.Static.UpdateTime > area.Value.ReservationTimer)
                            m_removeReservedAreas.Enqueue(new KeyValuePair<string, long>(tag.Key, area.Key));
                    }
                }

                foreach (var id in m_removeReservedAreas)
                {
                    m_reservedAreas[id.Key].Remove(id.Value);

                    var cancelMsg = new ReserveAreaCancelMsg()
                    {
                        ReservationName = id.Key,
                        Id = id.Value,
                    };

                    Sync.Layer.SendMessageToAll(ref cancelMsg, MyTransportMessageEnum.Success);
                }

                m_removeReservedAreas.Clear();
			}
		}

		public static void AddAiTarget(MyAiTargetBase aiTarget)
        {
            if (Static == null) return;

            Debug.Assert(!Static.m_aiTargets.Contains(aiTarget), "AI target already exists in the manager");
            Static.m_aiTargets.Add(aiTarget);
        }

		public static void RemoveAiTarget(MyAiTargetBase aiTarget)
        {
            if (Static == null) return;

            Static.m_aiTargets.Remove(aiTarget);
        }

		private void OnEntityRemoved(MyEntity entity)
        {
            foreach (var aiTarget in m_aiTargets)
            {
                if (aiTarget.TargetEntity == entity)
                    aiTarget.UnsetTarget();
            }
        }

        public bool IsInReservedArea(string areaName, Vector3D position)
        {
            Dictionary<long, ReservedAreaData> reservations = null;
            if (m_reservedAreas.TryGetValue(areaName, out reservations))
            {
                foreach (var areaData in reservations.Values)
                {
                    if ((areaData.WorldPosition - position).LengthSquared() < areaData.Radius * areaData.Radius)
                        return true;
                }
            }
            return false;
        }
    }
}
