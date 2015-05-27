using Sandbox.Common.ObjectBuilders;
using Sandbox.Common.ObjectBuilders.AI;
using Sandbox.Common.ObjectBuilders.Serializer;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using VRageMath;

namespace Sandbox.Game.AI.Actions
{
    public class MyHumanoidBotCommonActions : MyAgentCommonActions
    {
        protected new MyHumanoidBot Bot { get { return base.Bot as MyHumanoidBot; } }

		private long m_reservationTimeOut;
		private const int RESERVATION_WAIT_TIMEOUT_SECONDS = 3;

        public MyHumanoidBotCommonActions(MyHumanoidBot humanoidBot, MyAiTargetBase targetBase)
            : base(humanoidBot, targetBase)
        {

        }

		public void Init_AimAtTarget()
		{
			if (AiTargetBase.HasTarget())
			{
				AiTargetBase.AimAtTarget();
			}
		}

		public MyBehaviorTreeState AimAtTarget(float rotationAngle = 2)
		{
			if (!AiTargetBase.HasTarget())
				return MyBehaviorTreeState.FAILURE;

			if (Bot.Navigation.HasRotation(MathHelper.ToRadians(rotationAngle)))
			{
				return MyBehaviorTreeState.RUNNING;
			}
			else
			{
				return MyBehaviorTreeState.SUCCESS;
			}
		}

		public void Post_AimAtTarget()
		{
			Bot.Navigation.ResetAiming(false);
		}

        public void Init_GotoAndAimTarget()
        {
            if (AiTargetBase.HasTarget())
            {
                AiTargetBase.GotoTarget();
                AiTargetBase.AimAtTarget();
            }
        }

        public MyBehaviorTreeState GotoAndAimTarget(float rotationAngle = 2)
        {
            if (!AiTargetBase.HasTarget())
                return MyBehaviorTreeState.FAILURE;
            if (Bot.Navigation.Navigating)
            {
                if (Bot.Navigation.Stuck)
                {
                    return MyBehaviorTreeState.FAILURE;
                }
                else
                {
                    return MyBehaviorTreeState.RUNNING;
                }
            }
            else if (Bot.Navigation.HasRotation(MathHelper.ToRadians(rotationAngle)))
            {
                return MyBehaviorTreeState.RUNNING;
            }
            else
            {
                return MyBehaviorTreeState.SUCCESS;
            }
        }

        public void Post_GotoAndAimTarget()
        {
            Bot.Navigation.StopImmediate(true);
            Bot.Navigation.ResetAiming(false);
        }

        public MyBehaviorTreeState EquipItem(string itemName)
        {
            if (string.IsNullOrEmpty(itemName))
                return MyBehaviorTreeState.FAILURE;
            var humanoid = Bot.HumanoidEntity;
            if (humanoid.CurrentWeapon != null && humanoid.CurrentWeapon.DefinitionId.SubtypeName == itemName)
                return MyBehaviorTreeState.SUCCESS;
            var ob = MyObjectBuilderSerializer.CreateNewObject<MyObjectBuilder_PhysicalGunObject>(itemName);
            var inventory = humanoid.GetInventory();
            if (inventory.ContainItems(1, ob))
            {
                humanoid.SwitchToWeapon(ob.GetId());
                return MyBehaviorTreeState.SUCCESS;
            }
            else
            {
                return MyBehaviorTreeState.FAILURE;
            }
        }

		private void ReservationHandler(ref MyAiTargetManager.ReservedEntityData reservedEntity, bool success)
		{
			if (Bot == null || Bot.HumanoidLogic == null || Bot.Player == null || Bot.Player.Id.SerialId != reservedEntity.ReserverId.SerialId)
				return;

			var logic = Bot.HumanoidLogic;

			logic.EntityReservationStatus = Logic.MyEntityReservationStatus.FAILURE;

			if (!success)
				return;

			if (reservedEntity.EntityId != logic.ReservationEntityData.EntityId)
				return;

			if (reservedEntity.Type == MyReservedEntityType.ENVIRONMENT_ITEM &&
				reservedEntity.LocalId != logic.ReservationEntityData.LocalId)
				return;

			if (reservedEntity.Type == MyReservedEntityType.VOXEL &&
			   reservedEntity.GridPos != logic.ReservationEntityData.GridPos)
				return;

			logic.EntityReservationStatus = Logic.MyEntityReservationStatus.SUCCESS;
		}

		public MyBehaviorTreeState TryReserveEntity(ref MyBBMemoryTarget inTarget, int timeMs)
		{
			MyBehaviorTreeState retStatus = MyBehaviorTreeState.FAILURE;
			if(Bot == null || Bot.Player == null)
				return MyBehaviorTreeState.FAILURE;
			
			var logic = Bot.HumanoidLogic;
			if(inTarget != null && inTarget.EntityId.HasValue && inTarget.TargetType != MyAiTargetEnum.POSITION && inTarget.TargetType != MyAiTargetEnum.NO_TARGET)
			{
				switch(logic.EntityReservationStatus)
				{
					case Logic.MyEntityReservationStatus.NONE:
						switch (inTarget.TargetType)
						{
							case MyAiTargetEnum.GRID:
							case MyAiTargetEnum.CUBE:
							case MyAiTargetEnum.CHARACTER:
							case MyAiTargetEnum.ENTITY:
								logic.EntityReservationStatus = Logic.MyEntityReservationStatus.WAITING;
								logic.ReservationEntityData = new MyAiTargetManager.ReservedEntityData() { Type = MyReservedEntityType.ENTITY,
																									   EntityId = inTarget.EntityId.Value, ReservationTimer = timeMs,
																									   ReserverId = new World.MyPlayer.PlayerId(Bot.Player.Id.SteamId, Bot.Player.Id.SerialId) };
								MyAiTargetManager.OnReservationResult += ReservationHandler;
								MyAiTargetManager.Static.RequestEntityReservation(logic.ReservationEntityData.EntityId, logic.ReservationEntityData.ReservationTimer, Bot.Player.Id.SerialId);
								break;

							case MyAiTargetEnum.ENVIRONMENT_ITEM:
								logic.EntityReservationStatus = Logic.MyEntityReservationStatus.WAITING;
								logic.ReservationEntityData = new MyAiTargetManager.ReservedEntityData() { Type = MyReservedEntityType.ENVIRONMENT_ITEM,
																									   EntityId = inTarget.EntityId.Value,
																									   LocalId = inTarget.TreeId.Value,
																									   ReservationTimer = timeMs,
																									   ReserverId = new World.MyPlayer.PlayerId(Bot.Player.Id.SteamId, Bot.Player.Id.SerialId) };
								MyAiTargetManager.OnReservationResult += ReservationHandler;
								MyAiTargetManager.Static.RequestEnvironmentItemReservation(logic.ReservationEntityData.EntityId, logic.ReservationEntityData.LocalId,
																					   logic.ReservationEntityData.ReservationTimer, Bot.Player.Id.SerialId);
								break;
							case MyAiTargetEnum.VOXEL:
								logic.EntityReservationStatus = Logic.MyEntityReservationStatus.WAITING;
								logic.ReservationEntityData = new MyAiTargetManager.ReservedEntityData() { Type = MyReservedEntityType.VOXEL,
																									   EntityId = inTarget.EntityId.Value,
																									   GridPos = inTarget.VoxelPosition.Value,
																									   ReservationTimer = timeMs,
																									   ReserverId = new World.MyPlayer.PlayerId(Bot.Player.Id.SteamId, Bot.Player.Id.SerialId) };
								MyAiTargetManager.OnReservationResult += ReservationHandler;
								MyAiTargetManager.Static.RequestVoxelPositionReservation(logic.ReservationEntityData.EntityId, logic.ReservationEntityData.GridPos,
																					   logic.ReservationEntityData.ReservationTimer, Bot.Player.Id.SerialId);
								break;
							default:
								break;
						}
						m_reservationTimeOut = Stopwatch.GetTimestamp() + Stopwatch.Frequency * RESERVATION_WAIT_TIMEOUT_SECONDS;
						logic.EntityReservationStatus = Logic.MyEntityReservationStatus.WAITING;
						retStatus = MyBehaviorTreeState.RUNNING;
						break;

					case Logic.MyEntityReservationStatus.SUCCESS:
						retStatus = MyBehaviorTreeState.SUCCESS;
						break;

					case Logic.MyEntityReservationStatus.FAILURE:
						retStatus = MyBehaviorTreeState.FAILURE;
						break;
					case Logic.MyEntityReservationStatus.WAITING:
						if(m_reservationTimeOut < Stopwatch.GetTimestamp())
							retStatus = MyBehaviorTreeState.FAILURE;
						else
							retStatus = MyBehaviorTreeState.RUNNING;
						break;
				}
			}
			return retStatus;
		}
		public void Post_TryReserveEntity()
		{
			if (Bot != null && Bot.HumanoidLogic != null)
			{
				var logic = Bot.HumanoidLogic;
				if(logic.EntityReservationStatus != Logic.MyEntityReservationStatus.NONE)
					MyAiTargetManager.OnReservationResult -= ReservationHandler;

				logic.EntityReservationStatus = Logic.MyEntityReservationStatus.NONE;
			}
		}
    }
}
