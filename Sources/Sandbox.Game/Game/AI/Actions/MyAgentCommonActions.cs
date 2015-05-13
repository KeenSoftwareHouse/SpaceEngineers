using Sandbox.Common.AI;
using Sandbox.Common.ObjectBuilders.AI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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

        public MyBehaviorTreeState IsTargetValid(ref MyBBMemoryTarget inTarget)
        {
            if (inTarget != null)
            {
                if (inTarget.TargetType != MyAiTargetEnum.NO_TARGET)
                    return MyBehaviorTreeState.SUCCESS;
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
