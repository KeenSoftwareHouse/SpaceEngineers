using Sandbox.Definitions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using VRageMath;
using VRage.Utils;
using Sandbox.Common;
using VRage;
using VRage.Game;
using VRage.Utils;
using VRage.Library.Utils;

namespace Sandbox.Game.AI.BehaviorTree
{
    [MyBehaviorTreeNodeType(typeof(MyObjectBuilder_BehaviorTreeActionNode), typeof(MyBehaviorTreeNodeMemory))]
    class MyBehaviorTreeActionNode : MyBehaviorTreeNode
    {
        private MyStringId m_actionName;
        private object[] m_parameters;

        public bool ReturnsRunning { get; private set; }
        public override bool IsRunningStateSource { get { return ReturnsRunning; } }

        public MyBehaviorTreeActionNode()
        {
            m_actionName = MyStringId.NullOrEmpty;
            m_parameters = null;
            ReturnsRunning = true;
        }

        public override void Construct(MyObjectBuilder_BehaviorTreeNode nodeDefinition, MyBehaviorTree.MyBehaviorTreeDesc treeDesc)
        {
            base.Construct(nodeDefinition, treeDesc);

            var ob = (MyObjectBuilder_BehaviorTreeActionNode)nodeDefinition;
            Debug.Assert(!string.IsNullOrEmpty(ob.ActionName), "Action name was not provided");
            if (!string.IsNullOrEmpty(ob.ActionName))
            {
                m_actionName = MyStringId.GetOrCompute(ob.ActionName);
                treeDesc.ActionIds.Add(m_actionName);
            }

            if (ob.Parameters != null)
            {
                var obParameters = ob.Parameters;
                m_parameters = new object[obParameters.Length];
                for (int i = 0; i < m_parameters.Length; i++)
                {
                    var obParam = obParameters[i];
                    if (obParam is MyObjectBuilder_BehaviorTreeActionNode.MemType)
                    {
                        string value = (string)obParam.GetValue();
                        m_parameters[i] = (Boxed<MyStringId>)MyStringId.GetOrCompute(value);
                    }
                    else
                    {
                        m_parameters[i] = obParam.GetValue();
                    }
                }
            }
        }

        public override MyBehaviorTreeState Tick(IMyBot bot, MyPerTreeBotMemory botTreeMemory)
        {
            if (bot.ActionCollection.ReturnsRunning(m_actionName))
                bot.BotMemory.ProcessLastRunningNode(this);

            var nodeMemory = botTreeMemory.GetNodeMemoryByIndex(MemoryIndex);
            if (!nodeMemory.InitCalled)
            {
                nodeMemory.InitCalled = true;
                if (bot.ActionCollection.ContainsInitAction(m_actionName))
                    bot.ActionCollection.PerformInitAction(bot, m_actionName);
            }

            var state = bot.ActionCollection.PerformAction(bot, m_actionName, m_parameters);
            nodeMemory.NodeState = state;
            return state;
        }

        public override void PostTick(IMyBot bot, MyPerTreeBotMemory botTreeMemory)
        {
            var nodeMemory = botTreeMemory.GetNodeMemoryByIndex(MemoryIndex);
            if (nodeMemory.InitCalled)
            {
                if (bot.ActionCollection.ContainsPostAction(m_actionName))
                    bot.ActionCollection.PerformPostAction(bot, m_actionName);

                nodeMemory.InitCalled = false;
            }
        }

        public override void DebugDraw(Vector2 position, Vector2 size, List<MyBehaviorTreeNodeMemory> nodesMemory)
        {
            VRageRender.MyRenderProxy.DebugDrawText2D(position, "A:" + m_actionName.ToString(), nodesMemory[MemoryIndex].NodeStateColor, DEBUG_TEXT_SCALE, MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_CENTER);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int result = base.GetHashCode();
                result = (result * 397) ^ m_actionName.ToString().GetHashCode();
                if (m_parameters != null)
                {
                    foreach (var param in m_parameters)
                    {   
                        result = (result * 397) ^ param.ToString().GetHashCode();
                    }
                }
                return result;
            }
        }

        public override string ToString()
        {
            return "ACTION: " + m_actionName.ToString();
        }

        public string GetActionName()
        {
            return m_actionName.ToString();
        }
    }
}
