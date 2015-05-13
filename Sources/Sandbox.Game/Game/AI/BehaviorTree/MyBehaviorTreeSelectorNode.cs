using Sandbox.Common.ObjectBuilders.AI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRageMath;

namespace Sandbox.Game.AI.BehaviorTree
{
    [MyBehaviorTreeNodeType(typeof(MyObjectBuilder_BehaviorTreeSelectorNode), typeof(MyBehaviorTreeControlNodeMemory))]
    class MyBehaviorTreeSelectorNode : MyBehaviorTreeControlBaseNode
    {
        public override MyBehaviorTreeState SearchedValue
        {
            get { return MyBehaviorTreeState.SUCCESS; }
        }

        public override MyBehaviorTreeState FinalValue
        {
            get { return MyBehaviorTreeState.FAILURE; }
        }

        public override string DebugSign
        {
            get { return "?"; }
        }
    }
}
