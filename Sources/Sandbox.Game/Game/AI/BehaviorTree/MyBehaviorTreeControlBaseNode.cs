using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.Utils;
using VRageMath;
using System.Diagnostics;
using VRage.Game;

namespace Sandbox.Game.AI.BehaviorTree
{
    [MyBehaviorTreeNodeType(typeof(MyObjectBuilder_BehaviorControlBaseNode), typeof(MyBehaviorTreeControlNodeMemory))]
    abstract class MyBehaviorTreeControlBaseNode : MyBehaviorTreeNode
    {
        protected List<MyBehaviorTreeNode> m_children;
        protected bool m_isMemorable;
        protected string m_name;

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
            m_name = controlBaseNode.Name;
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
                if (Sandbox.Engine.Utils.MyDebugDrawSettings.DEBUG_DRAW_BOTS)
                {
                    string childName = (m_children[i] is MyBehaviorTreeControlBaseNode) ? ((m_children[i] as MyBehaviorTreeControlBaseNode)).m_name : 
                        (m_children[i] is MyBehaviorTreeActionNode)? (m_children[i] as MyBehaviorTreeActionNode).GetActionName(): 
                        (m_children[i] is MyBehaviorTreeDecoratorNode)? (m_children[i] as MyBehaviorTreeDecoratorNode).GetName():
                        "";                     // just variable for conditional debugging
                    m_runningActionName = "";   // this line is good candidate for breakpoint is you want to debug special part of behavior tree
                }
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
                    RecordRunningNodeName(state, m_children[i]);
                    return state;
                }
                bot.BotMemory.ForgetNode();
            }

            nodeMemory.NodeState = FinalValue;
            nodeMemory.InitialIndex = 0;
            return FinalValue;
        }

        [Conditional("DEBUG")]
        void RecordRunningNodeName(MyBehaviorTreeState state, MyBehaviorTreeNode node)
        {
            if (!Sandbox.Engine.Utils.MyDebugDrawSettings.DEBUG_DRAW_BOTS)
                return;

            m_runningActionName = "";
            if (state == MyBehaviorTreeState.RUNNING)
            {
                if (node is MyBehaviorTreeActionNode)
                {
                    MyBehaviorTreeActionNode action = (MyBehaviorTreeActionNode)node;
                    m_runningActionName = action.GetActionName();
                }
                else
                {
                    string str = node.m_runningActionName;
                    if (str.Contains(ParentName))
                        str = str.Replace(ParentName, m_name + "-");
                    m_runningActionName = str;
                }
            }
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

        public override string ToString()
        {
            return m_name;
        }
    }
}
