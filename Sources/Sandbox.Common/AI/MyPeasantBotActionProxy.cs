using Sandbox.Common.ObjectBuilders.AI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sandbox.Common.AI
{
    [BehaviorProperties("Peasant")]
    public abstract class MyPeasantBotActionProxy : MyHumanoidBotActionProxy
    {
        [MyBehaviorTreeAction("IsWorking", ReturnsRunning = false)]
        protected abstract MyBehaviorTreeState Condition_IsWorking();

        [MyBehaviorTreeAction("IsStoneInArea", ReturnsRunning = false)]
        protected abstract MyBehaviorTreeState Condition_IsStoneInArea([BTParam] float radius, [BTOut] ref MyBBMemoryTarget outFoundLocation);

        [MyBehaviorTreeAction("StartDig")]
        protected abstract MyBehaviorTreeState Action_StartDig();

        [MyBehaviorTreeAction("StartDig", MyBehaviorTreeActionType.POST)]
        protected abstract void PostAction_StartDig();

        [MyBehaviorTreeAction("Dig", MyBehaviorTreeActionType.INIT)]
        protected abstract void InitAction_Dig();

        [MyBehaviorTreeAction("Dig")]
        protected abstract MyBehaviorTreeState Action_Dig();

		[MyBehaviorTreeAction("AreTreesInArea", ReturnsRunning = false)]
		protected abstract MyBehaviorTreeState Condition_AreTreesInArea([BTParam] float radius, [BTOut] ref MyBBMemoryTarget outTarget);

		[MyBehaviorTreeAction("StartCuttingTree")]
		protected abstract MyBehaviorTreeState Action_StartCuttingTree();

		[MyBehaviorTreeAction("StartCuttingTree", MyBehaviorTreeActionType.POST)]
		protected abstract void PostAction_StartCuttingTree();

		[MyBehaviorTreeAction("CutTree", MyBehaviorTreeActionType.INIT)]
		protected abstract void InitAction_CutTree();

		[MyBehaviorTreeAction("CutTree")]
		protected abstract MyBehaviorTreeState Action_CutTree();

        [MyBehaviorTreeAction("Collect", MyBehaviorTreeActionType.INIT)]
        protected abstract void Init_Action_Collect();

        [MyBehaviorTreeAction("Collect")]
        protected abstract MyBehaviorTreeState Action_Collect();

        [MyBehaviorTreeAction("Collect", MyBehaviorTreeActionType.POST)]
        protected abstract void Post_Action_Collect();

        [MyBehaviorTreeAction("IsStoneOreInRadius", ReturnsRunning = false)]
        protected abstract MyBehaviorTreeState Condition_IsStoneOreInRadius([BTParam] float radius, [BTOut] ref MyBBMemoryTarget outTarget);

        //[MyBehaviorTreeAction("RunAway")]
        //protected abstract MyBehaviorTreeState Action_RunAway();

        [MyBehaviorTreeAction("FindWanderLocation", ReturnsRunning = false)]
        protected abstract MyBehaviorTreeState Action_FindWanderLocation([BTOut] ref MyBBMemoryTarget outLocation);

        [MyBehaviorTreeAction("IsLookingAtTarget", ReturnsRunning = false)]
        protected abstract MyBehaviorTreeState Condition_IsLookingAtTarget();

        [MyBehaviorTreeAction("LookAtTarget", MyBehaviorTreeActionType.INIT)]
        protected abstract void Init_Action_LookAtTarget();

        [MyBehaviorTreeAction("LookAtTarget", ReturnsRunning = false)]
        protected abstract MyBehaviorTreeState Action_LookAtTarget();

		[MyBehaviorTreeAction("TargetForward", ReturnsRunning = false)]
		protected abstract MyBehaviorTreeState Action_TargetForward([BTParam] float amount, [BTOut] ref MyBBMemoryTarget outFoundLocation);

        [MyBehaviorTreeAction("LookAtTarget", MyBehaviorTreeActionType.POST)]
        protected abstract void Post_Action_LookAtTarget(); // tftftf

        [MyBehaviorTreeAction("SetAndAimTarget", ReturnsRunning = false)]
        protected abstract MyBehaviorTreeState Action_SetAndAimTarget([BTIn] ref MyBBMemoryTarget inTarget);

        [MyBehaviorTreeAction("IsHoldingItem", ReturnsRunning = false)]
        protected abstract MyBehaviorTreeState Condition_IsHoldingItem();

        [MyBehaviorTreeAction("GoBackToSpawnPoint", ReturnsRunning = false)]
        protected abstract MyBehaviorTreeState Action_GoBackToSpawnPoint();

        [MyBehaviorTreeAction("DropItem")]
        protected abstract MyBehaviorTreeState Action_DropItem();

        [MyBehaviorTreeAction("SwitchToWalk", ReturnsRunning = false)]
        protected abstract MyBehaviorTreeState Action_SwitchToWalk();

        [MyBehaviorTreeAction("SwitchToRun", ReturnsRunning = false)]
        protected abstract MyBehaviorTreeState Action_SwitchToRun();

        [MyBehaviorTreeAction("GotoAndAimRadius", MyBehaviorTreeActionType.INIT)]
        protected abstract void Init_Action_GotoAndAimRadius();

        [MyBehaviorTreeAction("GotoAndAimRadius")]
        protected abstract MyBehaviorTreeState Action_GotoAndAimRadius([BTParam] float radius);

        [MyBehaviorTreeAction("GotoAndAimRadius", MyBehaviorTreeActionType.POST)]
        protected abstract void Post_Action_GotoAndAimRadius();
    }
}
