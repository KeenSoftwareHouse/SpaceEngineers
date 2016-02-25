using Sandbox.Definitions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using VRage.Game;
using VRageMath;

namespace Sandbox.Game.AI.BehaviorTree
{
    [MyBehaviorTreeNodeType(typeof(MyObjectBuilder_BehaviorTreeNode))]
    public abstract class MyBehaviorTreeNode
    {
        protected static float DEBUG_TEXT_SCALE = 0.5f;
        protected static float DEBUG_TEXT_Y_OFFSET = 60.0f;
        protected static float DEBUG_SCALE = 0.4f;
        protected static float DEBUG_ROOT_OFFSET = 20.0f;
        protected static float DEBUG_LINE_OFFSET_MULT = 25;

        public int MemoryIndex { get; private set; }
        public Type MemoryType { get; private set; }

        public const string ParentName = "Par_N";
        public string m_runningActionName = "";

        public MyBehaviorTreeNode()
        {
            foreach (var attr in GetType().GetCustomAttributes(false))
            {
                if (attr.GetType() == typeof(MyBehaviorTreeNodeTypeAttribute))
                {
                    var nodeTypeAttr = attr as MyBehaviorTreeNodeTypeAttribute;
                    MemoryType = nodeTypeAttr.MemoryType;
                }
            }
        }

        public virtual void Construct(MyObjectBuilder_BehaviorTreeNode nodeDefinition, MyBehaviorTree.MyBehaviorTreeDesc treeDesc)
        {
            MemoryIndex = treeDesc.MemorableNodesCounter++;
            treeDesc.Nodes.Add(this);
        }

        public abstract MyBehaviorTreeState Tick(IMyBot bot, MyPerTreeBotMemory nodesMemory);
        public virtual void PostTick(IMyBot bot, MyPerTreeBotMemory nodesMemory) { }
        public abstract void DebugDraw(Vector2 position, Vector2 size, List<MyBehaviorTreeNodeMemory> nodesMemory);
        public abstract bool IsRunningStateSource { get; }

        public virtual MyBehaviorTreeNodeMemory GetNewMemoryObject()
        {
            if (MemoryType != null && (MemoryType.IsSubclassOf(typeof(MyBehaviorTreeNodeMemory)) || MemoryType == typeof(MyBehaviorTreeNodeMemory)))
                return Activator.CreateInstance(MemoryType) as MyBehaviorTreeNodeMemory;
            else
            {
                Debug.Fail("Failed creating memory object with specified memory type for: " + GetType().Name);
                return new MyBehaviorTreeNodeMemory();
            }
        }

        public override int GetHashCode()
        {
            return MemoryIndex;
        }
    }
}
