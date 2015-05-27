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
        protected abstract MyBehaviorTreeState Condition_IsStoneInArea([BTParam] float radius, [BTOut] ref MyBBMemoryTarget outFoundTarget);

		[MyBehaviorTreeAction("IsStoneInPlaceArea", ReturnsRunning = false)]
		protected abstract MyBehaviorTreeState Condition_IsStoneInPlaceArea([BTIn] ref MyBBMemoryTarget inPlaceArea, [BTOut] ref MyBBMemoryTarget outFoundTarget);

		[MyBehaviorTreeAction("FindRandomStoneInPlaceArea", ReturnsRunning = false)]
		protected abstract MyBehaviorTreeState Action_FindRandomStoneInPlaceArea([BTIn] ref MyBBMemoryTarget inPlaceArea, [BTOut] ref MyBBMemoryTarget outFoundTarget);

        [MyBehaviorTreeAction("StartDig")]
        protected abstract MyBehaviorTreeState Action_StartDig();

        [MyBehaviorTreeAction("StartDig", MyBehaviorTreeActionType.POST)]
        protected abstract void PostAction_StartDig();

        [MyBehaviorTreeAction("Dig", MyBehaviorTreeActionType.INIT)]
        protected abstract void InitAction_Dig();

        [MyBehaviorTreeAction("Dig")]
        protected abstract MyBehaviorTreeState Action_Dig();

        [MyBehaviorTreeAction("Dig", MyBehaviorTreeActionType.POST)]
        protected abstract void PostAction_Dig();

		[MyBehaviorTreeAction("AreTreesInArea", ReturnsRunning = false)]
		protected abstract MyBehaviorTreeState Condition_AreTreesInArea([BTParam] float radius, [BTOut] ref MyBBMemoryTarget outTarget);

		[MyBehaviorTreeAction("AreTreesInPlaceArea", ReturnsRunning = false)]
		protected abstract MyBehaviorTreeState Condition_AreTreesInPlaceArea([BTIn] ref MyBBMemoryTarget inPlaceArea, [BTOut] ref MyBBMemoryTarget outTarget);

		[MyBehaviorTreeAction("FindRandomTreeInPlaceArea", ReturnsRunning = false)]
		protected abstract MyBehaviorTreeState Action_FindRandomTreeInPlaceArea([BTIn] ref MyBBMemoryTarget inPlaceArea, [BTOut] ref MyBBMemoryTarget outTarget);

		[MyBehaviorTreeAction("StartCuttingTree")]
		protected abstract MyBehaviorTreeState Action_StartCuttingTree();

		[MyBehaviorTreeAction("StartCuttingTree", MyBehaviorTreeActionType.POST)]
		protected abstract void PostAction_StartCuttingTree();

		[MyBehaviorTreeAction("StopWorking")]
		protected abstract MyBehaviorTreeState Action_StopWorking();

		[MyBehaviorTreeAction("CutTree", MyBehaviorTreeActionType.INIT)]
		protected abstract void InitAction_CutTree();

		[MyBehaviorTreeAction("CutTree")]
		protected abstract MyBehaviorTreeState Action_CutTree();

        [MyBehaviorTreeAction("CutTree", MyBehaviorTreeActionType.POST)]
        protected abstract void PostAction_CutTree();

		[MyBehaviorTreeAction("IsTreeCut", ReturnsRunning = false)]
		protected abstract MyBehaviorTreeState Condition_IsTreeCut([BTIn] ref MyBBMemoryTarget inTargetTree);

		[MyBehaviorTreeAction("IsTreeFallingInArea", ReturnsRunning = false)]
		protected abstract MyBehaviorTreeState Condition_IsTreeFallingInArea([BTParam] float radius);

		[MyBehaviorTreeAction("AreTreeTrunksInArea", ReturnsRunning = false)]
		protected abstract MyBehaviorTreeState Condition_AreTreeTrunksInArea([BTParam] float radius, [BTOut] ref MyBBMemoryTarget outTarget);

		[MyBehaviorTreeAction("AreTreeTrunksInPlaceArea", ReturnsRunning = false)]
		protected abstract MyBehaviorTreeState Condition_AreTreeTrunksInPlaceArea([BTIn] ref MyBBMemoryTarget inPlaceArea, [BTOut] ref MyBBMemoryTarget outTarget);

		[MyBehaviorTreeAction("FindRandomTreeTrunkInPlaceArea", ReturnsRunning = false)]
		protected abstract MyBehaviorTreeState Action_FindRandomTreeTrunkInPlaceArea([BTIn] ref MyBBMemoryTarget inPlaceArea, [BTOut] ref MyBBMemoryTarget outTarget);

		[MyBehaviorTreeAction("IsTrunkCut", ReturnsRunning = false)]
		protected abstract MyBehaviorTreeState Condition_IsTrunkCut([BTIn] ref MyBBMemoryTarget inTargetTrunk);

        [MyBehaviorTreeAction("Collect", MyBehaviorTreeActionType.INIT)]
        protected abstract void Init_Action_Collect();

        [MyBehaviorTreeAction("Collect")]
        protected abstract MyBehaviorTreeState Action_Collect();

        [MyBehaviorTreeAction("Collect", MyBehaviorTreeActionType.POST)]
        protected abstract void Post_Action_Collect();

        [MyBehaviorTreeAction("IsStoneOreInRadius", ReturnsRunning = false)]
        protected abstract MyBehaviorTreeState Condition_IsStoneOreInRadius([BTParam] float radius, [BTOut] ref MyBBMemoryTarget outTarget);

		[MyBehaviorTreeAction("IsStoneOreInPlaceArea", ReturnsRunning = false)]
		protected abstract MyBehaviorTreeState Condition_IsStoneOreInPlaceArea([BTIn] ref MyBBMemoryTarget inPlaceArea, [BTOut] ref MyBBMemoryTarget outTarget);

		[MyBehaviorTreeAction("IsWoodInPlaceArea", ReturnsRunning = false)]
		protected abstract MyBehaviorTreeState Condition_IsWoodInPlaceArea([BTIn] ref MyBBMemoryTarget inPlaceArea, [BTOut] ref MyBBMemoryTarget outTarget);

		[MyBehaviorTreeAction("FindRandomStoneOreInPlaceArea", ReturnsRunning = false)]
		protected abstract MyBehaviorTreeState Action_FindRandomStoneOreInPlaceArea([BTIn] ref MyBBMemoryTarget inPlaceArea, [BTOut] ref MyBBMemoryTarget outTarget);

		[MyBehaviorTreeAction("FindRandomWoodInPlaceArea", ReturnsRunning = false)]
		protected abstract MyBehaviorTreeState Action_FindRandomWoodInPlaceArea([BTIn] ref MyBBMemoryTarget inPlaceArea, [BTOut] ref MyBBMemoryTarget outTarget);

		[MyBehaviorTreeAction("FindOwner", ReturnsRunning = false)]
		protected abstract MyBehaviorTreeState Action_FindOwner([BTOut] ref MyBBMemoryTarget outTarget);

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

		[MyBehaviorTreeAction("TargetRandomPointInEntity", ReturnsRunning = false)]
		protected abstract MyBehaviorTreeState Action_TargetRandomPointInEntity([BTInOut] ref MyBBMemoryTarget inOutTarget);

        [MyBehaviorTreeAction("LookAtTarget", MyBehaviorTreeActionType.POST)]
        protected abstract void Post_Action_LookAtTarget(); // tftftf

		[MyBehaviorTreeAction("HasTarget", ReturnsRunning = false)]
		protected abstract MyBehaviorTreeState Condition_HasTarget();

		[MyBehaviorTreeAction("HasNoTarget", ReturnsRunning = false)]
		protected abstract MyBehaviorTreeState Condition_HasNoTarget();

        [MyBehaviorTreeAction("SetAndAimTarget", ReturnsRunning = false)]
        protected abstract MyBehaviorTreeState Action_SetAndAimTarget([BTIn] ref MyBBMemoryTarget inTarget);

		[MyBehaviorTreeAction("FindCenterOfMass", ReturnsRunning = false)]
		protected abstract MyBehaviorTreeState Action_FindCenterOfMass([BTInOut] ref MyBBMemoryTarget inTarget);

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
