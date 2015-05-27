using Sandbox.Common.ObjectBuilders.AI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sandbox.Common.AI
{
    public abstract class MyHumanoidBotActionProxy : MyAgentBotActionProxy
    {
        public bool ShouldFollowPlayer { get; set; }
        
        [MyBehaviorTreeAction("ShouldFollowPlayer", ReturnsRunning = false)]
        protected MyBehaviorTreeState Action_ShouldFollowPlayer()
        {
            return ShouldFollowPlayer ? MyBehaviorTreeState.SUCCESS : MyBehaviorTreeState.FAILURE;
        }

        [MyBehaviorTreeAction("ResetShouldFollowPlayer", ReturnsRunning = false)]
        protected MyBehaviorTreeState Action_ResetShouldFollowPlayer()
        {
            ShouldFollowPlayer = false;
            return MyBehaviorTreeState.SUCCESS;
        }

		[MyBehaviorTreeAction("AimAtTarget", MyBehaviorTreeActionType.INIT)]
		protected abstract void Init_Action_AimAtTarget();

		[MyBehaviorTreeAction("AimAtTarget")]
		protected abstract MyBehaviorTreeState Action_AimAtTarget();

		[MyBehaviorTreeAction("AimAtTarget", MyBehaviorTreeActionType.POST)]
		protected abstract void Post_Action_AimAtTarget();

        [MyBehaviorTreeAction("GotoAndAimTarget", MyBehaviorTreeActionType.INIT)]
        protected abstract void Init_Action_GotoAndAimTarget();

        [MyBehaviorTreeAction("GotoAndAimTarget")]
        protected abstract MyBehaviorTreeState Action_GotoAndAimTarget();

        [MyBehaviorTreeAction("GotoAndAimTarget", MyBehaviorTreeActionType.POST)]
        protected abstract void Post_Action_GotoAndAimTarget();

		[MyBehaviorTreeAction("PlaySound", ReturnsRunning = false)]
		protected abstract MyBehaviorTreeState Action_PlaySound([BTParam] string soundName);

        [MyBehaviorTreeAction("EquipItem", ReturnsRunning = false)]
        protected abstract MyBehaviorTreeState Action_EquipItem([BTParam] string itemName);

		[MyBehaviorTreeAction("FindClosestPlaceAreaInRadius", ReturnsRunning = false)]
		protected abstract MyBehaviorTreeState Action_FindClosestPlaceAreaInRadius([BTParam] float radius, [BTParam] string typeName, [BTOut] ref MyBBMemoryTarget outTarget);

		[MyBehaviorTreeAction("TryReserveEntity")]
		protected abstract MyBehaviorTreeState Action_TryReserveEntity([BTIn] ref MyBBMemoryTarget inTarget, [BTParam] int timeMs);

		[MyBehaviorTreeAction("TryReserveEntity", MyBehaviorTreeActionType.POST)]
		protected abstract void Post_Action_TryReserveEntity();
    }
}
