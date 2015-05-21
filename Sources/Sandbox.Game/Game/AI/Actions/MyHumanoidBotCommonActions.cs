using Sandbox.Common.ObjectBuilders;
using Sandbox.Common.ObjectBuilders.AI;
using Sandbox.Common.ObjectBuilders.Serializer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRageMath;

namespace Sandbox.Game.AI.Actions
{
    public class MyHumanoidBotCommonActions : MyAgentCommonActions
    {
        protected new MyHumanoidBot Bot { get { return base.Bot as MyHumanoidBot; } }

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
    }
}
