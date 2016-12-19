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
using VRage.Game.Components;
using VRage.Game.Entity;
using VRage.Library.Utils;
using VRage.Network;
using VRage.Utils;
using VRageMath;

namespace Sandbox.Game.AI
{
    [StaticEventOwner]
	[PreloadRequired]
    [MySessionComponentDescriptor(MyUpdateOrder.AfterSimulation)]
    public class MyAiTargetManager : MySessionComponentBase
    {
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

        [Event, Reliable, Server]
        private static void OnReserveEntityRequest(long entityId, long reservationTimeMs, int senderSerialId)
		{
            EndpointId sender;
            if (MyEventContext.Current.IsLocallyInvoked)
                sender = new EndpointId(Sync.MyId);
            else
                sender = MyEventContext.Current.Sender;

			bool success = true;
			ReservedEntityData entityData;
            var entityIdPair = new KeyValuePair<long, long>(entityId, 0);
            if (m_reservedEntities.TryGetValue(entityIdPair, out entityData))
			{
                if (entityData.ReserverId == new MyPlayer.PlayerId(sender.Value, senderSerialId))
                    entityData.ReservationTimer = Stopwatch.GetTimestamp() + Stopwatch.Frequency * reservationTimeMs / 1000;
				else
                    success = false;
			}
			else
                m_reservedEntities.Add(entityIdPair, new ReservedEntityData()
				{
                    EntityId = entityId,
                    ReservationTimer = Stopwatch.GetTimestamp() + Stopwatch.Frequency * reservationTimeMs / 1000,
                    ReserverId = new MyPlayer.PlayerId(sender.Value, senderSerialId)
				});

            if (MyEventContext.Current.IsLocallyInvoked)
            {
                if (success)
                    OnReserveEntitySuccess(entityId, senderSerialId);
                else
                    OnReserveEntityFailure(entityId, senderSerialId);
            }
            else
            {
                if (success)
                    MyMultiplayer.RaiseStaticEvent(s => MyAiTargetManager.OnReserveEntitySuccess, entityId, senderSerialId, sender);
                else
                    MyMultiplayer.RaiseStaticEvent(s => MyAiTargetManager.OnReserveEntityFailure, entityId, senderSerialId, sender);
            }
		}

        [Event, Reliable, Client]
        private static void OnReserveEntitySuccess(long entityId, int senderSerialId)
		{
			if (OnReservationResult != null)
			{
                var reservationData = new ReservedEntityData() { Type = MyReservedEntityType.ENTITY, EntityId = entityId, ReserverId = new MyPlayer.PlayerId(0, senderSerialId) };
				OnReservationResult(ref reservationData, true);
			}
		}

        [Event, Reliable, Client]
        private static void OnReserveEntityFailure(long entityId, int senderSerialId)
		{
			if (OnReservationResult != null)
			{
                var reservationData = new ReservedEntityData() { Type = MyReservedEntityType.ENTITY, EntityId = entityId, ReserverId = new MyPlayer.PlayerId(0, senderSerialId) };
				OnReservationResult(ref reservationData, false);
			}
		}

        [Event, Reliable, Server]
        private static void OnReserveEnvironmentItemRequest(long entityId, int localId, long reservationTimeMs, int senderSerialId)
		{
            EndpointId sender;
            if (MyEventContext.Current.IsLocallyInvoked)
                sender = new EndpointId(Sync.MyId);
            else
                sender = MyEventContext.Current.Sender;

            bool success = true;
			ReservedEntityData entityData;
            var entityIdPair = new KeyValuePair<long, long>(entityId, localId);
            if (m_reservedEntities.TryGetValue(entityIdPair, out entityData))
			{
                if (entityData.ReserverId == new MyPlayer.PlayerId(sender.Value, senderSerialId))
                    entityData.ReservationTimer = Stopwatch.GetTimestamp() + Stopwatch.Frequency * reservationTimeMs / 1000;
				else
                    success = false;
			}
			else
                m_reservedEntities.Add(entityIdPair, new ReservedEntityData()
				{
                    EntityId = entityId,
                    LocalId = localId,
                    ReservationTimer = Stopwatch.GetTimestamp() + Stopwatch.Frequency * reservationTimeMs / 1000,
                    ReserverId = new MyPlayer.PlayerId(sender.Value, senderSerialId)
				});

            if (MyEventContext.Current.IsLocallyInvoked)
            {
                if (success)
                    OnReserveEnvironmentItemSuccess(entityId, localId, senderSerialId);
                else
                    OnReserveEnvironmentItemFailure(entityId, localId, senderSerialId);
            }
            else
            {
                if (success)
                    MyMultiplayer.RaiseStaticEvent(s => MyAiTargetManager.OnReserveEnvironmentItemSuccess, entityId, localId, senderSerialId, sender);
                else
                    MyMultiplayer.RaiseStaticEvent(s => MyAiTargetManager.OnReserveEnvironmentItemFailure, entityId, localId, senderSerialId, sender);
            }
		}

        [Event, Reliable, Client]
        private static void OnReserveEnvironmentItemSuccess(long entityId, int localId, int senderSerialId)
		{
			if (OnReservationResult != null)
			{
				var reservationData = new ReservedEntityData()
				{
					Type = MyReservedEntityType.ENVIRONMENT_ITEM,
                    EntityId = entityId,
                    LocalId = localId,
                    ReserverId = new MyPlayer.PlayerId(0, senderSerialId)
				};
				OnReservationResult(ref reservationData, true);
			}
		}

        [Event, Reliable, Client]
        private static void OnReserveEnvironmentItemFailure(long entityId, int localId, int senderSerialId)
		{
			if (OnReservationResult != null)
			{
				var reservationData = new ReservedEntityData()
				{
					Type = MyReservedEntityType.ENVIRONMENT_ITEM,
                    EntityId = entityId,
                    LocalId = localId,
                    ReserverId = new MyPlayer.PlayerId(0, senderSerialId)
				};
				OnReservationResult(ref reservationData, false);
			}
		}

        [Event, Reliable, Server]
        private static void OnReserveVoxelPositionRequest(long entityId, Vector3I voxelPosition, long reservationTimeMs, int senderSerialId)
		{
            EndpointId sender;
            if (MyEventContext.Current.IsLocallyInvoked)
                sender = new EndpointId(Sync.MyId);
            else
                sender = MyEventContext.Current.Sender;

            bool success = true;
			ReservedEntityData entityData;
			MyVoxelBase voxelMap = null;
			if (!MySession.Static.VoxelMaps.Instances.TryGetValue(entityId, out voxelMap))
				return;

			Vector3I voxelMapSize = voxelMap.StorageMax - voxelMap.StorageMin;
			Debug.Assert(voxelMapSize.AbsMax() < 2 * 10E6, "Voxel map size too large to reserve unique voxel position");
			// Integer overflow won't even happen on the next line for voxel maps smaller than roughly (2.5M)^3, and most Vector3I member functions have broken down way before that
			var entityIdPair = new KeyValuePair<long, long>(entityId, voxelPosition.X + voxelPosition.Y * voxelMapSize.X + voxelPosition.Z * voxelMapSize.X * voxelMapSize.Y);
			if (m_reservedEntities.TryGetValue(entityIdPair, out entityData))
			{
                if (entityData.ReserverId == new MyPlayer.PlayerId(sender.Value, senderSerialId))
                    entityData.ReservationTimer = Stopwatch.GetTimestamp() + Stopwatch.Frequency * reservationTimeMs / 1000;
                else
                    success = false;
			}
			else
				m_reservedEntities.Add(entityIdPair, new ReservedEntityData()
				{
					EntityId = entityId,
					GridPos = voxelPosition,
					ReservationTimer = Stopwatch.GetTimestamp() + Stopwatch.Frequency * reservationTimeMs / 1000,
                    ReserverId = new MyPlayer.PlayerId(sender.Value, senderSerialId)
				});

            if (MyEventContext.Current.IsLocallyInvoked)
            {
                if (success)
                    OnReserveVoxelPositionSuccess(entityId, voxelPosition, senderSerialId);
                else
                    OnReserveVoxelPositionFailure(entityId, voxelPosition, senderSerialId);
            }
            else
            {
                if (success)
                    MyMultiplayer.RaiseStaticEvent(s => MyAiTargetManager.OnReserveVoxelPositionSuccess, entityId, voxelPosition, senderSerialId, sender);
                else
                    MyMultiplayer.RaiseStaticEvent(s => MyAiTargetManager.OnReserveVoxelPositionFailure, entityId, voxelPosition, senderSerialId, sender);
            }
		}

        [Event, Reliable, Client]
        private static void OnReserveVoxelPositionSuccess(long entityId, Vector3I voxelPosition, int senderSerialId)
		{
			if (OnReservationResult != null)
			{
				var reservationData = new ReservedEntityData()
				{
					Type = MyReservedEntityType.VOXEL,
                    EntityId = entityId,
                    GridPos = voxelPosition,
                    ReserverId = new MyPlayer.PlayerId(0, senderSerialId)
				};
				OnReservationResult(ref reservationData, true);
			}
		}

        [Event, Reliable, Client]
        private static void OnReserveVoxelPositionFailure(long entityId, Vector3I voxelPosition, int senderSerialId)
		{
			if (OnReservationResult != null)
			{
				var reservationData = new ReservedEntityData()
				{
					Type = MyReservedEntityType.VOXEL,
                    EntityId = entityId,
                    GridPos = voxelPosition,
                    ReserverId = new MyPlayer.PlayerId(0, senderSerialId)
				};
				OnReservationResult(ref reservationData, false);
			}
		}

		public void RequestEntityReservation(long entityId, long reservationTimeMs, int senderSerialId)
		{
            MyMultiplayer.RaiseStaticEvent(s => MyAiTargetManager.OnReserveEntityRequest, entityId, reservationTimeMs, senderSerialId);
		}

		public void RequestEnvironmentItemReservation(long entityId, int localId, long reservationTimeMs, int senderSerialId)
		{
            MyMultiplayer.RaiseStaticEvent(s => MyAiTargetManager.OnReserveEnvironmentItemRequest, entityId, localId, reservationTimeMs, senderSerialId);
		}

		public void RequestVoxelPositionReservation(long entityId, Vector3I voxelPosition, long reservationTimeMs, int senderSerialId)
		{
            MyMultiplayer.RaiseStaticEvent(s => MyAiTargetManager.OnReserveVoxelPositionRequest, entityId, voxelPosition, reservationTimeMs, senderSerialId);
		}

		#endregion

        #region Area reservation

        public void RequestAreaReservation(string reservationName, Vector3D position, float radius, long reservationTimeMs, int senderSerialId)
        {
            MyMultiplayer.RaiseStaticEvent(s => MyAiTargetManager.OnReserveAreaRequest, reservationName, position, radius, reservationTimeMs, senderSerialId);
        }

        [Event, Reliable, Server]
        private static void OnReserveAreaRequest(string reservationName, Vector3D position, float radius, long reservationTimeMs, int senderSerialId)
        {
            EndpointId sender;
            if (MyEventContext.Current.IsLocallyInvoked)
                sender = new EndpointId(Sync.MyId);
            else
                sender = MyEventContext.Current.Sender;

            if (!m_reservedAreas.ContainsKey(reservationName))
                m_reservedAreas.Add(reservationName, new Dictionary<long, ReservedAreaData>());

            var reservations = m_reservedAreas[reservationName];
            bool reservedBySomeone = false;
            MyPlayer.PlayerId requestId = new MyPlayer.PlayerId(sender.Value, senderSerialId);

            foreach (var r in reservations)
            {
                var currentReservation = r.Value;
                var sqDist = (currentReservation.WorldPosition - position).LengthSquared();
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
                    WorldPosition = position,
                    Radius = radius,
                    ReservationTimer = MySandboxGame.Static.UpdateTime + MyTimeSpan.FromMilliseconds(reservationTimeMs),
                    ReserverId = requestId,
                };

                MyMultiplayer.RaiseStaticEvent(s => MyAiTargetManager.OnReserveAreaAllSuccess, AreaReservationCounter, reservationName, position, radius);

                if (MyEventContext.Current.IsLocallyInvoked)
                    OnReserveAreaSuccess(position, radius, senderSerialId);
                else
                    MyMultiplayer.RaiseStaticEvent(s => MyAiTargetManager.OnReserveAreaSuccess, position, radius, senderSerialId, sender);
            }
            else
            {
                if (MyEventContext.Current.IsLocallyInvoked)
                    OnReserveAreaFailure(position, radius, senderSerialId);
                else
                    MyMultiplayer.RaiseStaticEvent(s => MyAiTargetManager.OnReserveAreaFailure, position, radius, senderSerialId, sender);
            }
        }

        [Event, Reliable, Client]
        private static void OnReserveAreaSuccess(Vector3D position, float radius, int senderSerialId)
        {
            if (OnAreaReservationResult != null)
            {
                var reservationData = new ReservedAreaData()
                {
                    WorldPosition = position,
                    Radius = radius,
                    ReserverId = new MyPlayer.PlayerId(0, senderSerialId)
                };
                OnAreaReservationResult(ref reservationData, true);
            }
        }

        [Event, Reliable, Client]
        private static void OnReserveAreaFailure(Vector3D position, float radius, int senderSerialId)
        {
            if (OnAreaReservationResult != null)
            {
                var reservationData = new ReservedAreaData()
                {
                    WorldPosition = position,
                    Radius = radius,
                    ReserverId = new MyPlayer.PlayerId(0, senderSerialId)
                };
                OnAreaReservationResult(ref reservationData, false);
            }
        }

        [Event, Reliable, Broadcast]
        private static void OnReserveAreaAllSuccess(long id, string reservationName, Vector3D position, float radius)
        {
            if (!m_reservedAreas.ContainsKey(reservationName))
                m_reservedAreas[reservationName] = new Dictionary<long, ReservedAreaData>();
            var reservations = m_reservedAreas[reservationName];
            reservations.Add(id, new ReservedAreaData() { WorldPosition = position, Radius = radius });
        }

        [Event, Reliable, Broadcast]
        private static void OnReserveAreaCancel(string reservationName, long id)
        {
            Dictionary<long, ReservedAreaData> reservations;
            if (m_reservedAreas.TryGetValue(reservationName, out reservations))
            {
                reservations.Remove(id);
            }
        }

        #endregion

		public override void LoadData()
        {
            Static = this;

            //This is otherwise crashing on clients because of NULL
			//if (Sync.IsServer)
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

                    MyMultiplayer.RaiseStaticEvent(s => MyAiTargetManager.OnReserveAreaCancel, id.Key, id.Value);
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
