using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sandbox.Common.ObjectBuilders.AI;
using VRage.Utils;

namespace Sandbox.Common.AI
{
    [BehaviorProperties("Barbarian")]
    public abstract class MyBarbarianBotActionProxy : MyHumanoidBotActionProxy
    {
        [MyBehaviorTreeAction("GotoFailed", ReturnsRunning = false)]
        protected abstract MyBehaviorTreeState Condition_GotoFailed();

        [MyBehaviorTreeAction("ResetGotoFailed", ReturnsRunning = false)]
        protected abstract MyBehaviorTreeState Action_ResetGotoFailed();

        [MyBehaviorTreeAction("PlayAnimation", ReturnsRunning = false)]
        protected abstract MyBehaviorTreeState Action_PlayAnimation([BTParam] string animationName, [BTParam] bool immediate);

        [MyBehaviorTreeAction("IsMoving", ReturnsRunning = false)]
        protected abstract MyBehaviorTreeState Condition_IsMoving();

        [MyBehaviorTreeAction("FindCharacterInRadius", ReturnsRunning = false)]
        protected abstract MyBehaviorTreeState Action_FindCharacterInRadius([BTParam] int radius, [BTOut] ref MyBBMemoryTarget outCharacter);

        [MyBehaviorTreeAction("IsCharacterInRadius", ReturnsRunning = false)]
        protected abstract MyBehaviorTreeState Action_IsCharacterInRadius([BTParam] int radius);

        [MyBehaviorTreeAction("IsNoCharacterInRadius", ReturnsRunning = false)]
        protected abstract MyBehaviorTreeState Action_IsNoCharacterInRadius([BTParam] int radius);

        [MyBehaviorTreeAction("FindStatueInRadius", ReturnsRunning = false)]
        protected abstract MyBehaviorTreeState Action_FindStatueInRadius([BTParam] int radius, [BTOut] ref MyBBMemoryTarget outStatue);

        [MyBehaviorTreeAction("FindClosestTargetInRadius", ReturnsRunning = false)]
        protected abstract MyBehaviorTreeState Action_FindClosestTargetInRadius([BTParam] int radius, [BTOut] ref MyBBMemoryTarget outClosestTarget);

        //[MyBehaviorTreeAction("SelectHalfwayPositionAsTarget")]
        //protected abstract MyBehaviorTreeState Action_SelectHalfwayPositionAsTarget(); 

        [MyBehaviorTreeAction("SelectStatueAsTarget", ReturnsRunning = false)]
        protected abstract MyBehaviorTreeState Action_SelectStatueAsTarget([BTInOut] ref MyBBMemoryTarget inoutTarget);

        [MyBehaviorTreeAction("FindClosestBlock", ReturnsRunning = false)]
        protected abstract MyBehaviorTreeState Action_FindClosestBlock([BTOut] ref MyBBMemoryTarget outBlock);

        [MyBehaviorTreeAction("IsTargetAttackable", ReturnsRunning = false)]
        protected abstract MyBehaviorTreeState Condition_IsTargetAttackable([BTIn] ref MyBBMemoryTarget inTarget);

        //[MyBehaviorTreeAction("AttackTarget", MyBehaviorTreeActionType.INIT)]
        //protected abstract void Init_Action_AttackTarget();

        //[MyBehaviorTreeAction("AttackTarget")]
        //protected abstract MyBehaviorTreeState Action_AttackTarget();

        [MyBehaviorTreeAction("IsNavigating", ReturnsRunning = false)]
        protected abstract MyBehaviorTreeState Condition_IsNavigating();

        [MyBehaviorTreeAction("PickNewWanderTarget", ReturnsRunning = false)]
        protected abstract MyBehaviorTreeState Action_PickNewWanderTarget([BTParam] bool wanderAroundCharacter, [BTOut] ref MyBBMemoryTarget outPosition);

        //[MyBehaviorTreeAction("IsDead", ReturnsRunning = false)]
        //protected abstract MyBehaviorTreeState Condition_IsDead(); // MW:TODO try remove

        //[MyBehaviorTreeAction("PlayDead", MyBehaviorTreeActionType.INIT)]
        //protected abstract void Init_Action_PlayDead(); // MW:TODO try remove

        //[MyBehaviorTreeAction("PlayDead", ReturnsRunning = false)]
        //protected abstract MyBehaviorTreeState Action_PlayDead(); // MW:TODO try remove

        //[MyBehaviorTreeAction("Respawn", ReturnsRunning = false)]
        //protected abstract MyBehaviorTreeState Action_Respawn(); // MW:TODO try remove

        [MyBehaviorTreeAction("IsSurvivalGame", ReturnsRunning = false)]
        protected abstract MyBehaviorTreeState Condition_IsSurvivalGame();

        [MyBehaviorTreeAction("IsCreativeGame", ReturnsRunning = false)]
        protected abstract MyBehaviorTreeState Condition_IsCreativeGame();

        [MyBehaviorTreeAction("IsAttacking", ReturnsRunning = false)]
        protected abstract MyBehaviorTreeState Condition_IsAttacking();

        [MyBehaviorTreeAction("StartAttack")]
        protected abstract MyBehaviorTreeState Action_StartAttack();

        [MyBehaviorTreeAction("StartAttack", MyBehaviorTreeActionType.POST)]
        protected abstract void Post_Action_StartAttack();

        [MyBehaviorTreeAction("Attack", MyBehaviorTreeActionType.INIT)]
        protected abstract void Init_Action_Attack();

        [MyBehaviorTreeAction("Attack")]
        protected abstract MyBehaviorTreeState Action_Attack();

        [MyBehaviorTreeAction("Attack", MyBehaviorTreeActionType.POST)]
        protected abstract void Post_Action_Attack();

        [MyBehaviorTreeAction("Jump", ReturnsRunning = false)]
        protected abstract MyBehaviorTreeState Action_Jump();
    }
}
