using Sandbox.Common.ObjectBuilders.AI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sandbox.Common.AI
{
    [BehaviorProperties("Animal")]
    public abstract class MyAnimalBotActionProxy : MyAgentBotActionProxy
    {
        [MyBehaviorTreeAction("SwitchToWalk", ReturnsRunning = false)]
        protected abstract MyBehaviorTreeState Action_SwitchToWalk();

        [MyBehaviorTreeAction("SwitchToRun", ReturnsRunning = false)]
        protected abstract MyBehaviorTreeState Action_SwitchToRun();

        [MyBehaviorTreeAction("FindWanderLocation", ReturnsRunning = false)]
        protected abstract MyBehaviorTreeState Action_FindWanderLocation([BTOut] ref MyBBMemoryTarget outTarget);

        [MyBehaviorTreeAction("FindRandomSafeLocation", ReturnsRunning = false)]
        protected abstract MyBehaviorTreeState Action_FindRandomSafeLocation([BTIn] ref MyBBMemoryTarget inTargetEnemy, [BTOut] ref MyBBMemoryTarget outTargetLocation);

        [MyBehaviorTreeAction("IsHumanInArea", ReturnsRunning = false)]
        protected abstract MyBehaviorTreeState Condition_IsHumanInArea([BTParam] int standingRadius, [BTParam] int crouchingRadius, [BTOut] ref MyBBMemoryTarget outTarget);

        [MyBehaviorTreeAction("RunAway", MyBehaviorTreeActionType.INIT)]
        protected abstract void Init_Action_RunAway();

        [MyBehaviorTreeAction("RunAway")]
        protected abstract MyBehaviorTreeState Action_RunAway();

        [MyBehaviorTreeAction("RunAway", MyBehaviorTreeActionType.POST)]
        protected abstract void Post_Action_RunAway();
    }
}