using Sandbox.Common.ObjectBuilders.AI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sandbox.Common.AI
{
    public class BehaviorPropertiesAttribute : Attribute
    {
        public readonly string BehaviorName;

        public BehaviorPropertiesAttribute(string behaviorName)
        {
            BehaviorName = behaviorName;
        }
    }

    public abstract class MyAbstractBotActionProxy
    {
        [MyBehaviorTreeAction("DummyRunningNode")]
        protected MyBehaviorTreeState Action_DummyRunningNode()
        {
            return MyBehaviorTreeState.RUNNING;
        }

        [MyBehaviorTreeAction("DummySucceedingNode", ReturnsRunning = false)]
        protected MyBehaviorTreeState Action_DummySucceedingNode()
        {
            return MyBehaviorTreeState.SUCCESS;
        }

        [MyBehaviorTreeAction("DummyFailingNode", ReturnsRunning = false)]
        protected MyBehaviorTreeState Action_DummyFailingNode()
        {
            return MyBehaviorTreeState.FAILURE;
        }

        [MyBehaviorTreeAction("Idle", MyBehaviorTreeActionType.INIT)]
        protected abstract void Init_Action_Idle();

        [MyBehaviorTreeAction("Idle")]
        protected abstract MyBehaviorTreeState Action_Idle();
    }
}
