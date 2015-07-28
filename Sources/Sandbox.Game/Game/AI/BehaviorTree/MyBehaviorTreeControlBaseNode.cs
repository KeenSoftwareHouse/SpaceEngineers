using Sandbox.Common.ObjectBuilders.AI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.Utils;
using VRageMath;

namespace Sandbox.Game.AI.BehaviorTree
{
    [MyBehaviorTreeNodeType(typeof(MyObjectBuilder_BehaviorControlBaseNode), typeof(MyBehaviorTreeControlNodeMemory))]
    abstract class MyBehaviorTreeControlBaseNode : MyBehaviorTreeNode
    {
        protected List<MyBehaviorTreeNode> m_children;
        protected bool m_isMemorable;

        public abstract MyBehaviorTreeState SearchedValue { get; }
        public abstract MyBehaviorTreeState FinalValue { get; }
        public abstract string DebugSign { get; }

        public override bool IsRunningStateSource { get { return false; } }

        public override void Construct(MyObjectBuilder_BehaviorTreeNode nodeDefinition, MyBehaviorTree.MyBehaviorTreeDesc treeDesc)
        {
            base.Construct(nodeDefinition, treeDesc);

            var controlBaseNode = (MyObjectBuilder_BehaviorControlBaseNode)nodeDefinition;

            m_children = new List<MyBehaviorTreeNode>(controlBaseNode.BTNodes.Length);
            m_isMemorable = controlBaseNode.IsMemorable;
            foreach (var ob in controlBaseNode.BTNodes)
            {
                var childInst = MyBehaviorTreeNodeFactory.CreateBTNode(ob);
                childInst.Construct(ob, treeDesc);
                m_children.Add(childInst);
            }
        }

        public override MyBehaviorTreeState Tick(IMyBot bot, MyPerTreeBotMemory botTreeMemory)
        {
            var nodeMemory = botTreeMemory.GetNodeMemoryByIndex(MemoryIndex) as MyBehaviorTreeControlNodeMemory;
            for (int i = nodeMemory.InitialIndex; i < m_children.Count; i++)
            {
                bot.BotMemory.RememberNode(m_children[i].MemoryIndex);
                var state = m_children[i].Tick(bot, botTreeMemory);
                if (state == SearchedValue || state == FinalValue)
                    m_children[i].PostTick(bot, botTreeMemory);
                if (state == MyBehaviorTreeState.RUNNING || state == SearchedValue)
                {
                    nodeMemory.NodeState = state;
                    if (state == MyBehaviorTreeState.RUNNING)
                    {
                        if (m_isMemorable)
                            nodeMemory.InitialIndex = i;
                    }
                    else
                    {
                        bot.BotMemory.ForgetNode();
                    }
                    return state;
                }
                bot.BotMemory.ForgetNode();
            }

            nodeMemory.NodeState = FinalValue;
            nodeMemory.InitialIndex = 0;
            return FinalValue;
        }

        public override void PostTick(IMyBot bot, MyPerTreeBotMemory botTreeMemory)
        {
            botTreeMemory.GetNodeMemoryByIndex(MemoryIndex).PostTickMemory();
            foreach (var child in m_children)
            {
                child.PostTick(bot, botTreeMemory);
            }
        }

        public override void DebugDraw(Vector2 pos, Vector2 size, List<MyBehaviorTreeNodeMemory> nodesMemory)
        {
            VRageRender.MyRenderProxy.DebugDrawText2D(pos, DebugSign, nodesMemory[MemoryIndex].NodeStateColor, DEBUG_TEXT_SCALE, MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_CENTER);

            size.X *= DEBUG_SCALE;

            Vector2 initPos = m_children.Count > 1 ? pos - size * 0.5f : pos;
            initPos.Y += DEBUG_TEXT_Y_OFFSET;
            size.X /= Math.Max(m_children.Count - 1, 1);
            foreach (var child in m_children)
            {
                Vector2 to = initPos - pos;
                to.Normalize();
                Vector2 lineStart = pos + to * DEBUG_LINE_OFFSET_MULT;
                Vector2 lineEnd = initPos - to * DEBUG_LINE_OFFSET_MULT;
                VRageRender.MyRenderProxy.DebugDrawLine2D((lineStart), (lineEnd), nodesMemory[child.MemoryIndex].NodeStateColor, nodesMemory[child.MemoryIndex].NodeStateColor, null);
                child.DebugDraw(initPos, size, nodesMemory);
                initPos.X += size.X;
            }
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int result = base.GetHashCode();
                result = (result * 397) ^ m_isMemorable.GetHashCode();
                result = (result * 397) ^ SearchedValue.GetHashCode();
                result = (result * 397) ^ FinalValue.GetHashCode();
                for (int i = 0; i < m_children.Count; i++)
                    result = (result * 397) ^ m_children[i].GetHashCode();
                return result;
            }
        }
    }
}
