using Sandbox.Common.AI;
using Sandbox.Common.ObjectBuilders.AI;
using Sandbox.Game.World;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sandbox.Game.AI.Actions
{
    public abstract class MyBotActionsBase
    {
        [MyBehaviorTreeAction("DummyRunningNode")]
        protected MyBehaviorTreeState DummyRunningNode()
        {
            return MyBehaviorTreeState.RUNNING;
        }

        [MyBehaviorTreeAction("DummySucceedingNode", ReturnsRunning = false)]
        protected MyBehaviorTreeState DummySucceedingNode()
        {
            return MyBehaviorTreeState.SUCCESS;
        }

        [MyBehaviorTreeAction("DummyFailingNode", ReturnsRunning = false)]
        protected MyBehaviorTreeState DummyFailingNode()
        {
            return MyBehaviorTreeState.FAILURE;
        }

        [MyBehaviorTreeAction("IsSurvivalGame", ReturnsRunning = false)]
        protected MyBehaviorTreeState IsSurvivalGame()
        {
            if (MySession.Static.SurvivalMode)
                return MyBehaviorTreeState.SUCCESS;
            else
                return MyBehaviorTreeState.FAILURE;
        }

        [MyBehaviorTreeAction("IsCreativeGame", ReturnsRunning = false)]
        protected MyBehaviorTreeState IsCreativeGame()
        {
            if (MySession.Static.CreativeMode)
                return MyBehaviorTreeState.SUCCESS;
            else
                return MyBehaviorTreeState.FAILURE;
        }

        [MyBehaviorTreeAction("Idle", MyBehaviorTreeActionType.INIT)]
        protected virtual void Init_Idle()
        {
        }

        [MyBehaviorTreeAction("Idle")]
        protected virtual MyBehaviorTreeState Idle()
        {
            return MyBehaviorTreeState.RUNNING;
        }

        [MyBehaviorTreeAction("SetBoolean", ReturnsRunning = false)]
        protected MyBehaviorTreeState SetBoolean([BTOut] ref MyBBMemoryBool variable, [BTParam] bool value)
        {
            if (variable == null)
            {
                variable = new MyBBMemoryBool();
            }

            variable.BoolValue = value;
            return MyBehaviorTreeState.SUCCESS;
        }

        [MyBehaviorTreeAction("IsTrue", ReturnsRunning = false)]
        protected MyBehaviorTreeState IsTrue([BTIn] ref MyBBMemoryBool variable)
        {
            return (variable != null && variable.BoolValue) ? MyBehaviorTreeState.SUCCESS : MyBehaviorTreeState.FAILURE;
        }

        [MyBehaviorTreeAction("IsFalse", ReturnsRunning = false)]
        protected MyBehaviorTreeState IsFalse([BTIn] ref MyBBMemoryBool variable)
        {
            return (variable == null || variable.BoolValue) ? MyBehaviorTreeState.FAILURE : MyBehaviorTreeState.SUCCESS;
        }
    }
}
