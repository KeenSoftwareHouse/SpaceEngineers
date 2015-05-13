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
    }
}
