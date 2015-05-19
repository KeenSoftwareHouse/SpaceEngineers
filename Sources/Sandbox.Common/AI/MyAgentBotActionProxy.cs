using Sandbox.Common.ObjectBuilders.AI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sandbox.Common.AI
{
    public abstract class MyAgentBotActionProxy : MyAbstractBotActionProxy
    {
        public abstract IMyAiTarget AiTarget { get; }

        [MyBehaviorTreeAction("GotoTarget", MyBehaviorTreeActionType.INIT)]
        protected abstract void Action_Init_GotoTarget();

        [MyBehaviorTreeAction("GotoTarget")]
        protected abstract MyBehaviorTreeState Action_GotoTarget();

        [MyBehaviorTreeAction("GotoTarget", MyBehaviorTreeActionType.POST)]
        protected abstract void Post_Action_GotoTarget();

        [MyBehaviorTreeAction("IsAtTargetPosition", ReturnsRunning = false)]
        protected abstract MyBehaviorTreeState Condition_IsAtTargetPosition([BTParam] float radius);

		[MyBehaviorTreeAction("IsAtTargetPositionCylinder", ReturnsRunning = false)]
		protected abstract MyBehaviorTreeState Condition_IsAtTargetPositionCylinder([BTParam] float radius, [BTParam] float height);

        [MyBehaviorTreeAction("IsNotAtTargetPosition", ReturnsRunning = false)]
        protected abstract MyBehaviorTreeState Condition_IsNotAtTargetPosition([BTParam] float radius);

        [MyBehaviorTreeAction("SetTarget", ReturnsRunning = false)]
        protected abstract MyBehaviorTreeState Action_SetTarget([BTIn] ref MyBBMemoryTarget inTarget);

		[MyBehaviorTreeAction("ClearTarget", ReturnsRunning = false)]
		protected abstract MyBehaviorTreeState Action_ClearTarget([BTInOut] ref MyBBMemoryTarget inTarget);

        [MyBehaviorTreeAction("IsTargetValid", ReturnsRunning = false)]
        protected abstract MyBehaviorTreeState Condition_IsTargetValid([BTIn] ref MyBBMemoryTarget inTarget);

		[MyBehaviorTreeAction("HasPlaceArea", ReturnsRunning = false)]
		protected abstract MyBehaviorTreeState Condition_HasTargetArea([BTIn] ref MyBBMemoryTarget inTarget);
    }
}
