using Sandbox.Game.Entities;
using VRage.Game;
using VRage.Game.AI;
using VRage.Library.Utils;
using VRage.ObjectBuilders;

namespace Sandbox.Game.AI.Actions
{
    public abstract class MyHumanoidBotActions : MyAgentActions
    {
        protected new MyHumanoidBot Bot { get { return base.Bot as MyHumanoidBot; } }

		private MyTimeSpan m_reservationTimeOut;
		private const int RESERVATION_WAIT_TIMEOUT_SECONDS = 3;

        public MyHumanoidBotActions(MyHumanoidBot humanoidBot)
            :
            base(humanoidBot)
        {

        }

        [MyBehaviorTreeAction("PlaySound", ReturnsRunning = false)]
        protected MyBehaviorTreeState PlaySound([BTParam] string soundName)
        {
            Bot.HumanoidEntity.SoundComp.StartSecondarySound(soundName, sync: true);
            return MyBehaviorTreeState.SUCCESS;
        }

        [MyBehaviorTreeAction("EquipItem", ReturnsRunning = false)]
        protected MyBehaviorTreeState EquipItem([BTParam] string itemName)
        {
            if (string.IsNullOrEmpty(itemName))
                return MyBehaviorTreeState.FAILURE;
            var humanoid = Bot.HumanoidEntity;
            if (humanoid.CurrentWeapon != null && humanoid.CurrentWeapon.DefinitionId.SubtypeName == itemName)
                return MyBehaviorTreeState.SUCCESS;
            var ob = MyObjectBuilderSerializer.CreateNewObject<MyObjectBuilder_PhysicalGunObject>(itemName);
            var defId = ob.GetId();
            var inventory = humanoid.GetInventory();
            if (inventory.ContainItems(1, ob) || !humanoid.WeaponTakesBuilderFromInventory(defId))
            {
                humanoid.SwitchToWeapon(defId);
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

			logic.ReservationStatus = Logic.MyReservationStatus.FAILURE;

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

			logic.ReservationStatus = Logic.MyReservationStatus.SUCCESS;
		}

        private void AreaReservationHandler(ref MyAiTargetManager.ReservedAreaData reservedArea, bool success)
        {
            if (Bot == null || Bot.HumanoidLogic == null || Bot.Player == null || Bot.Player.Id.SerialId != reservedArea.ReserverId.SerialId)
                return;
            var logic = Bot.HumanoidLogic;

            logic.ReservationStatus = Logic.MyReservationStatus.FAILURE;
            if (!success)
                return;

            if (reservedArea.WorldPosition == logic.ReservationAreaData.WorldPosition && reservedArea.Radius == logic.ReservationAreaData.Radius)
                logic.ReservationStatus = Logic.MyReservationStatus.SUCCESS;
        }

        [MyBehaviorTreeAction("TryReserveEntity")]
        protected MyBehaviorTreeState TryReserveEntity([BTIn] ref MyBBMemoryTarget inTarget, [BTParam] int timeMs)
		{
			MyBehaviorTreeState retStatus = MyBehaviorTreeState.FAILURE;
			if(Bot == null || Bot.Player == null)
				return MyBehaviorTreeState.FAILURE;
			
			var logic = Bot.HumanoidLogic;
			if(inTarget != null && inTarget.EntityId.HasValue && inTarget.TargetType != MyAiTargetEnum.POSITION && inTarget.TargetType != MyAiTargetEnum.NO_TARGET)
			{
				switch(logic.ReservationStatus)
				{
					case Logic.MyReservationStatus.NONE:
                        switch (inTarget.TargetType)
						{
							case MyAiTargetEnum.GRID:
							case MyAiTargetEnum.CUBE:
							case MyAiTargetEnum.CHARACTER:
							case MyAiTargetEnum.ENTITY:
								logic.ReservationStatus = Logic.MyReservationStatus.WAITING;
								logic.ReservationEntityData = new MyAiTargetManager.ReservedEntityData() { Type = MyReservedEntityType.ENTITY,
																									   EntityId = inTarget.EntityId.Value, ReservationTimer = timeMs,
																									   ReserverId = new World.MyPlayer.PlayerId(Bot.Player.Id.SteamId, Bot.Player.Id.SerialId) };
								MyAiTargetManager.OnReservationResult += ReservationHandler;
								MyAiTargetManager.Static.RequestEntityReservation(logic.ReservationEntityData.EntityId, logic.ReservationEntityData.ReservationTimer, Bot.Player.Id.SerialId);
								break;

							case MyAiTargetEnum.ENVIRONMENT_ITEM:
								logic.ReservationStatus = Logic.MyReservationStatus.WAITING;
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
								logic.ReservationStatus = Logic.MyReservationStatus.WAITING;
								logic.ReservationEntityData = new MyAiTargetManager.ReservedEntityData() { Type = MyReservedEntityType.VOXEL,
																									   EntityId = inTarget.EntityId.Value,
																									   GridPos = inTarget.VoxelPosition,
																									   ReservationTimer = timeMs,
																									   ReserverId = new World.MyPlayer.PlayerId(Bot.Player.Id.SteamId, Bot.Player.Id.SerialId) };
								MyAiTargetManager.OnReservationResult += ReservationHandler;
								MyAiTargetManager.Static.RequestVoxelPositionReservation(logic.ReservationEntityData.EntityId, logic.ReservationEntityData.GridPos,
																					   logic.ReservationEntityData.ReservationTimer, Bot.Player.Id.SerialId);
								break;
							default:
                                logic.ReservationStatus = Logic.MyReservationStatus.FAILURE;
                                retStatus = MyBehaviorTreeState.FAILURE;
                                break;
						}
                        m_reservationTimeOut = MySandboxGame.Static.UpdateTime + MyTimeSpan.FromSeconds(RESERVATION_WAIT_TIMEOUT_SECONDS);
						break;

					case Logic.MyReservationStatus.SUCCESS:
					case Logic.MyReservationStatus.FAILURE:
						break;

					case Logic.MyReservationStatus.WAITING:
                        if (m_reservationTimeOut < MySandboxGame.Static.UpdateTime)
							logic.ReservationStatus = Logic.MyReservationStatus.FAILURE;
						break;
				}
			}

            switch (logic.ReservationStatus)
            {
                case Logic.MyReservationStatus.WAITING:
                    return MyBehaviorTreeState.RUNNING;
                case Logic.MyReservationStatus.SUCCESS:
                    return MyBehaviorTreeState.SUCCESS;
                case Logic.MyReservationStatus.FAILURE:
                default:
                    return MyBehaviorTreeState.FAILURE;
            }
		}

        [MyBehaviorTreeAction("TryReserveEntity", MyBehaviorTreeActionType.POST)]
		protected void Post_TryReserveEntity()
		{
			if (Bot != null && Bot.HumanoidLogic != null)
			{
				var logic = Bot.HumanoidLogic;
				if(logic.ReservationStatus != Logic.MyReservationStatus.NONE)
					MyAiTargetManager.OnReservationResult -= ReservationHandler;

				logic.ReservationStatus = Logic.MyReservationStatus.NONE;
			}
		}

        [MyBehaviorTreeAction("TryReserveArea")]
        protected MyBehaviorTreeState TryReserveAreaAroundEntity([BTParam] string areaName, [BTParam] float radius, [BTParam] int timeMs)
        {
            var logic = Bot.HumanoidLogic;
            MyBehaviorTreeState retStatus = MyBehaviorTreeState.FAILURE;
            if (logic != null)
            {
                switch (logic.ReservationStatus)
                {
                    case Logic.MyReservationStatus.NONE:
       				    logic.ReservationStatus = Logic.MyReservationStatus.WAITING;
                        logic.ReservationAreaData = new MyAiTargetManager.ReservedAreaData() 
                        {
                            WorldPosition = Bot.HumanoidEntity.WorldMatrix.Translation,
                            Radius = radius,
                            ReservationTimer = MyTimeSpan.FromMilliseconds(timeMs),
                            ReserverId = new World.MyPlayer.PlayerId(Bot.Player.Id.SteamId, Bot.Player.Id.SerialId) 
                        };
					    MyAiTargetManager.OnAreaReservationResult += AreaReservationHandler;
                        MyAiTargetManager.Static.RequestAreaReservation(areaName, Bot.HumanoidEntity.WorldMatrix.Translation, radius, timeMs, Bot.Player.Id.SerialId);
                        m_reservationTimeOut = MySandboxGame.Static.UpdateTime + MyTimeSpan.FromSeconds(RESERVATION_WAIT_TIMEOUT_SECONDS);
						logic.ReservationStatus = Logic.MyReservationStatus.WAITING;
						retStatus = MyBehaviorTreeState.RUNNING;
					    break;
                    case Logic.MyReservationStatus.SUCCESS:
                        retStatus = MyBehaviorTreeState.SUCCESS;
                        break;

                    case Logic.MyReservationStatus.FAILURE:
                        retStatus = MyBehaviorTreeState.FAILURE;
                        break;
                    case Logic.MyReservationStatus.WAITING:
                        if (m_reservationTimeOut < MySandboxGame.Static.UpdateTime)
                            retStatus = MyBehaviorTreeState.FAILURE;
                        else
                            retStatus = MyBehaviorTreeState.RUNNING;
                        break;
                }
            }
            return retStatus;
        }

        [MyBehaviorTreeAction("TryReserveArea", MyBehaviorTreeActionType.POST)]
        protected void Post_TryReserveArea()
        {
            if (Bot.HumanoidLogic != null)
            {
                var logic = Bot.HumanoidLogic;
                if (logic.ReservationStatus != Logic.MyReservationStatus.NONE)
                    MyAiTargetManager.OnAreaReservationResult -= AreaReservationHandler;
                logic.ReservationStatus = Logic.MyReservationStatus.NONE;
            }
        }

        [MyBehaviorTreeAction("IsInReservedArea", ReturnsRunning = false)]
        protected MyBehaviorTreeState IsInReservedArea([BTParam] string areaName)
        {
            if (MyAiTargetManager.Static.IsInReservedArea(areaName, Bot.HumanoidEntity.WorldMatrix.Translation))
                return MyBehaviorTreeState.SUCCESS;
            else
                return MyBehaviorTreeState.FAILURE;
        }

        [MyBehaviorTreeAction("IsNotInReservedArea", ReturnsRunning = false)]
        protected MyBehaviorTreeState IsNotInReservedArea([BTParam] string areaName)
        {
            if (MyAiTargetManager.Static.IsInReservedArea(areaName, Bot.HumanoidEntity.WorldMatrix.Translation))
                return MyBehaviorTreeState.FAILURE;
            else
                return MyBehaviorTreeState.SUCCESS;
        }
    }
}
