using Sandbox.Common.AI;
using Sandbox.Common.ObjectBuilders.AI;
using Sandbox.Game.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRageMath;

namespace Sandbox.Game.AI.Actions
{
    public class MyAgentCommonActions
    {
        protected MyAgentBot Bot { get; private set; }
        protected MyAiTargetBase AiTargetBase { get; private set; }

        public MyAgentCommonActions(MyAgentBot bot, MyAiTargetBase targetBase)
        {
            Bot = bot;
            AiTargetBase = targetBase;
        }

        public void Init_GotoTarget()
        {
            if (AiTargetBase.HasTarget())
            {
                AiTargetBase.GotoTarget();
            }
        }

        public MyBehaviorTreeState GotoTarget()
        {
            if (!AiTargetBase.HasTarget())
                return MyBehaviorTreeState.FAILURE;
            if (Bot.Navigation.Navigating)
            {
                if (Bot.Navigation.Stuck)
                {
                    AiTargetBase.GotoFailed();
                    return MyBehaviorTreeState.FAILURE;
                }
                else
                {
                    return MyBehaviorTreeState.RUNNING;
                }
            }
            else
            {
                return MyBehaviorTreeState.SUCCESS;
            }
        }

        public void Post_GotoTarget()
        {
            Bot.Navigation.StopImmediate(true);
        }

        public MyBehaviorTreeState IsAtTargetPosition(float radius)
        {
            if (!AiTargetBase.HasTarget())
            {
                return MyBehaviorTreeState.FAILURE;
            }

            if (AiTargetBase.PositionIsNearTarget(Bot.Player.Character.PositionComp.GetPosition(), radius))
            {
                return MyBehaviorTreeState.SUCCESS;
            }
            else
            {
                return MyBehaviorTreeState.FAILURE;
            }
        }

		public MyBehaviorTreeState IsAtTargetPositionCylinder(float radius, float height)
		{
			if (!AiTargetBase.HasTarget())
            {
                return MyBehaviorTreeState.FAILURE;
            }

			Vector3D position = Bot.Player.Character.PositionComp.GetPosition();
			Vector3D gotoPosition;
			float gotoRadius;
			AiTargetBase.GetGotoPosition(position, out gotoPosition, out gotoRadius);
			var xzPosition = new Vector2((float)position.X, (float)position.Z);
			var xzGotoPosition = new Vector2((float)gotoPosition.X, (float)gotoPosition.Z);

			return (Vector2.Distance(xzPosition, xzGotoPosition) <= radius && xzPosition.Y < xzGotoPosition.Y && xzPosition.Y+height > xzGotoPosition.Y ? MyBehaviorTreeState.SUCCESS : MyBehaviorTreeState.FAILURE);
		}

        public MyBehaviorTreeState IsNotAtTargetPosition(float radius)
        {
            if (!AiTargetBase.HasTarget())
            {
                return MyBehaviorTreeState.FAILURE;
            }

            if (AiTargetBase.PositionIsNearTarget(Bot.Player.Character.PositionComp.GetPosition(), radius))
            {
                return MyBehaviorTreeState.FAILURE;
            }
            else
            {
                return MyBehaviorTreeState.SUCCESS;
            }
        }

        public MyBehaviorTreeState SetTarget(ref MyBBMemoryTarget inTarget)
        {
            if (inTarget != null)
            {
                if (AiTargetBase.SetTargetFromMemory(inTarget))
                    return MyBehaviorTreeState.SUCCESS;
                else
                    return MyBehaviorTreeState.FAILURE;

            }

            return MyBehaviorTreeState.FAILURE;
        }

		public MyBehaviorTreeState UnsetTarget(ref MyBBMemoryTarget inTarget)
		{
			if (inTarget != null)
			{
				inTarget.TargetType = MyAiTargetEnum.NO_TARGET;
				inTarget.Position = null;
				inTarget.EntityId = null;
				inTarget.TreeId = null;
			}

			return MyBehaviorTreeState.SUCCESS;
		}

        public MyBehaviorTreeState IsTargetValid(ref MyBBMemoryTarget inTarget)
        {
            if (inTarget != null)
            {
                if (inTarget.TargetType != MyAiTargetEnum.NO_TARGET)
                    return MyBehaviorTreeState.SUCCESS;
            }
            return MyBehaviorTreeState.FAILURE;
        }

		public MyBehaviorTreeState HasTargetArea(ref MyBBMemoryTarget inTarget)
		{
			if (inTarget != null && inTarget.EntityId.HasValue)
			{
				MyEntity entity = null;
				if (MyEntities.TryGetEntityById(inTarget.EntityId.Value, out entity))
				{
					MyPlaceArea area = null;
					if (entity.Components.TryGet<MyPlaceArea>(out area))
					{
						return MyBehaviorTreeState.SUCCESS;
					}
				}
			}
			return MyBehaviorTreeState.FAILURE;
		}

        public void Init_Action_Idle()
        {
        }

        public MyBehaviorTreeState Action_Idle()
        {
            return MyBehaviorTreeState.RUNNING;
        }
    }
}
