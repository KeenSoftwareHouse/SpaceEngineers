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
using VRageMath;

namespace Sandbox.Game.AI
{
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
			public long LocalId;
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

		#endregion

		public struct ReservedEntityData
		{
			public MyReservedEntityType Type;
			public long EntityId;
			public long LocalId;
			public Vector3I GridPos;
			public long ReservationTimer;
			public MyPlayer.PlayerId ReserverId;
		}

		private HashSet<MyAiTargetBase> m_aiTargets = new HashSet<MyAiTargetBase>();

		private static Dictionary<KeyValuePair<long, long>, ReservedEntityData> m_reservedEntities;
		private static Queue<KeyValuePair<long, long>> m_removeReservedEntities;

		public static MyAiTargetManager Static;
		public delegate void ReservationHandler(ref ReservedEntityData entityData, bool success);
		public static event ReservationHandler OnReservationResult;

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

		public void RequestEnvironmentItemReservation(long entityId, long localId, long reservationTimeMs, int senderSerialId)
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
		}

		public MyAiTargetManager()
		{
			Static = this;
		}

		public override void LoadData()
        {
			if(Sync.IsServer)
			{
				m_reservedEntities = new Dictionary<KeyValuePair<long, long>, ReservedEntityData>();
				m_removeReservedEntities = new Queue<KeyValuePair<long, long>>();
			}

            MyEntities.OnEntityRemove += OnEntityRemoved;
        }

		protected override void UnloadData()
        {
            base.UnloadData();
            m_aiTargets.Clear();
            MyEntities.OnEntityRemove -= OnEntityRemoved;
        }

		public override void Simulate()
		{
			base.Simulate();

			if (Sync.IsServer)
			{
				foreach (var entity in m_reservedEntities)
				{
					if (Stopwatch.GetTimestamp() > entity.Value.ReservationTimer)
						m_removeReservedEntities.Enqueue(entity.Key);
				}
			}
		}

		public override void UpdateAfterSimulation()
		{
			base.UpdateAfterSimulation();

			if (Sync.IsServer)
			{
				foreach (var id in m_removeReservedEntities)
				{
					m_reservedEntities.Remove(id);
				}
				m_removeReservedEntities.Clear();
			}
		}

		public void AddAiTarget(MyAiTargetBase aiTarget)
        {
            Debug.Assert(!m_aiTargets.Contains(aiTarget), "AI target already exists in the manager");
            m_aiTargets.Add(aiTarget);
        }

		public void RemoveAiTarget(MyAiTargetBase aiTarget)
        {
            m_aiTargets.Remove(aiTarget);
        }

		private void OnEntityRemoved(MyEntity entity)
        {
            foreach (var aiTarget in m_aiTargets)
            {
                if (aiTarget.TargetEntity == entity)
                    aiTarget.UnsetTarget();
            }
        }
    }
}
