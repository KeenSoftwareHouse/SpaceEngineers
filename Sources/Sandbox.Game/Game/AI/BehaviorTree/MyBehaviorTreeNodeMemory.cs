using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using VRage.Game;
using VRageMath;

namespace Sandbox.Game.AI.BehaviorTree
{
    [MyBehaviorTreeNodeMemoryType(typeof(MyObjectBuilder_BehaviorTreeNodeMemory))]
    public class MyBehaviorTreeNodeMemory
    {
        public MyBehaviorTreeState NodeState { get; set; } // so far only for debugging
        public Color NodeStateColor { get { return GetColorByState(NodeState); } }
        public bool InitCalled { get; set; }
        public bool IsTicked { get { return NodeState != MyBehaviorTreeState.NOT_TICKED; } }

        public MyBehaviorTreeNodeMemory()
        {
            InitCalled = false;
            ClearNodeState();
        }

        public virtual void Init(MyObjectBuilder_BehaviorTreeNodeMemory builder)
        {
            InitCalled = builder.InitCalled;
        }

        public virtual MyObjectBuilder_BehaviorTreeNodeMemory GetObjectBuilder()
        {
            var builder = MyBehaviorTreeNodeMemoryFactory.CreateObjectBuilder(this);
            builder.InitCalled = InitCalled;
            return builder;
        }

        public virtual void ClearMemory()
        {
            NodeState = MyBehaviorTreeState.NOT_TICKED;
            InitCalled = false;
        }

        public virtual void PostTickMemory()
        {
        }

        public void ClearNodeState()
        {
            NodeState = MyBehaviorTreeState.NOT_TICKED;           
        }

        private static Color GetColorByState(MyBehaviorTreeState state)
        {
            switch (state)
            {
                case MyBehaviorTreeState.ERROR:
                    return Color.Bisque;
                case MyBehaviorTreeState.FAILURE:
                    return Color.Red;
                case MyBehaviorTreeState.NOT_TICKED:
                    return Color.White;
                case MyBehaviorTreeState.RUNNING:
                    return Color.Yellow;
                case MyBehaviorTreeState.SUCCESS:
                    return Color.Green;
                default:
                    Debug.Fail("It shouldn't get here.");
                    return Color.Black;
            }
        }
    }
}
