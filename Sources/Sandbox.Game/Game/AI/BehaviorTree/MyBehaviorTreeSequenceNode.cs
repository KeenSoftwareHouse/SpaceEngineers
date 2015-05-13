using Sandbox.Common.ObjectBuilders.AI;
using Sandbox.Definitions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRageMath;

namespace Sandbox.Game.AI.BehaviorTree
{
    [MyBehaviorTreeNodeType(typeof(MyObjectBuilder_BehaviorTreeSequenceNode), typeof(MyBehaviorTreeControlNodeMemory))]
    class MyBehaviorTreeSequenceNode : MyBehaviorTreeControlBaseNode
    {

        public override MyBehaviorTreeState SearchedValue
        {
            get { return MyBehaviorTreeState.FAILURE; }
        }

        public override MyBehaviorTreeState FinalValue
        {
            get { return MyBehaviorTreeState.SUCCESS; }
        }

        public override string DebugSign
        {
            get { return "->"; }
        }
    }
}
