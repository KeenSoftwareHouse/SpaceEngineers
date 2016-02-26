using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.Game;

namespace Sandbox.Game.AI.BehaviorTree
{
    [MyBehaviorTreeNodeMemoryType(typeof(MyObjectBuilder_BehaviorTreeControlNodeMemory))]
    public class MyBehaviorTreeControlNodeMemory : MyBehaviorTreeNodeMemory
    {
        public int InitialIndex { get; set; }

        public MyBehaviorTreeControlNodeMemory()
            : base()
        {
            InitialIndex = 0;
        }

        public override void Init(MyObjectBuilder_BehaviorTreeNodeMemory builder)
        {
            base.Init(builder);

            var ob = builder as MyObjectBuilder_BehaviorTreeControlNodeMemory;
            InitialIndex = ob.InitialIndex;
        }

        public override MyObjectBuilder_BehaviorTreeNodeMemory GetObjectBuilder()
        {
            var ob = base.GetObjectBuilder() as MyObjectBuilder_BehaviorTreeControlNodeMemory;
            ob.InitialIndex = InitialIndex;
            return ob;
        }

        public override void ClearMemory()
        {
            base.ClearMemory();

            InitialIndex = 0;
        }

        public override void PostTickMemory()
        {
            base.PostTickMemory();

            InitialIndex = 0;
        }
    }
}
