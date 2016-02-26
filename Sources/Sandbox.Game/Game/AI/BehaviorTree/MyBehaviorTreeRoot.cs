using Sandbox.Definitions;
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
    class MyBehaviorTreeRoot : MyBehaviorTreeNode
    {
        private MyBehaviorTreeNode m_child;

        public override bool IsRunningStateSource { get { return false; } }

        public override void Construct(MyObjectBuilder_BehaviorTreeNode nodeDefinition, MyBehaviorTree.MyBehaviorTreeDesc treeDesc)
        {
            base.Construct(nodeDefinition, treeDesc);

            m_child = MyBehaviorTreeNodeFactory.CreateBTNode(nodeDefinition);
            m_child.Construct(nodeDefinition, treeDesc);
        }

        public override MyBehaviorTreeState Tick(IMyBot bot, MyPerTreeBotMemory botTreeMemory)
        {
            bot.BotMemory.RememberNode(m_child.MemoryIndex);

            if ( Sandbox.Engine.Utils.MyDebugDrawSettings.DEBUG_DRAW_BOTS )
            {
                // store this old memory
                bot.LastBotMemory = bot.BotMemory.Clone();
            }

            var state = m_child.Tick(bot, botTreeMemory);
            botTreeMemory.GetNodeMemoryByIndex(MemoryIndex).NodeState = state;
            RecordRunningNodeName(bot, state);

            if (state != MyBehaviorTreeState.RUNNING)
                bot.BotMemory.ForgetNode();

            return state;
        }

        [Conditional("DEBUG")]
        void RecordRunningNodeName(IMyBot bot, MyBehaviorTreeState state)
        {
            if (!Sandbox.Engine.Utils.MyDebugDrawSettings.DEBUG_DRAW_BOTS || !(bot is MyAgentBot))
                return;

            switch(state)
            {
                case MyBehaviorTreeState.RUNNING:
                    (bot as MyAgentBot).LastActions.AddLastAction(m_child.m_runningActionName);
                    break;
                case MyBehaviorTreeState.ERROR:
                    (bot as MyAgentBot).LastActions.AddLastAction("error");
                    break;
                case MyBehaviorTreeState.FAILURE:
                    (bot as MyAgentBot).LastActions.AddLastAction("failure");
                    break;
                case MyBehaviorTreeState.SUCCESS:
                    (bot as MyAgentBot).LastActions.AddLastAction("failure");
                    break;
                case MyBehaviorTreeState.NOT_TICKED:
                    (bot as MyAgentBot).LastActions.AddLastAction("not ticked");
                    break;
            }
        }

        public override void DebugDraw(Vector2 pos, Vector2 size, List<MyBehaviorTreeNodeMemory> nodesMemory)
        {
            VRageRender.MyRenderProxy.DebugDrawText2D(pos, "ROOT", nodesMemory[MemoryIndex].NodeStateColor, DEBUG_TEXT_SCALE, MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_CENTER);
            pos.Y += DEBUG_ROOT_OFFSET;
            m_child.DebugDraw(pos, size, nodesMemory);
        }

        // MW:TODO refactor root
        public override MyBehaviorTreeNodeMemory GetNewMemoryObject()
        {
            return new MyBehaviorTreeNodeMemory();
        }

        public override int GetHashCode()
        {
            return m_child.GetHashCode();
        }
    }
}
