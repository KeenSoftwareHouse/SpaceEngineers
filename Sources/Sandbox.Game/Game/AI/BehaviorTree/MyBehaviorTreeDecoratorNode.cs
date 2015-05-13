using Sandbox.Common.ObjectBuilders.AI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace Sandbox.Game.AI.BehaviorTree
{
    [MyBehaviorTreeNodeType(typeof(MyObjectBuilder_BehaviorTreeDecoratorNode), typeof(MyBehaviorTreeDecoratorNodeMemory))]
    public class MyBehaviorTreeDecoratorNode : MyBehaviorTreeNode
    {
        private MyBehaviorTreeNode m_child;
        private IMyDecoratorLogic m_decoratorLogic;
        private MyBehaviorTreeState m_defaultReturnValue;

        private string m_decoratorLogicName;

        private MyDecoratorDefaultReturnValues DecoratorDefaultReturnValue
        {
            get { return (MyDecoratorDefaultReturnValues)((byte)m_defaultReturnValue); }
        }

        public MyBehaviorTreeDecoratorNode()
        {
            m_child = null;
            m_decoratorLogic = null;
        }

        public override void Construct(MyObjectBuilder_BehaviorTreeNode nodeDefinition, MyBehaviorTree.MyBehaviorTreeDesc treeDesc)
        {
            base.Construct(nodeDefinition, treeDesc);
            var ob = nodeDefinition as MyObjectBuilder_BehaviorTreeDecoratorNode;

            m_defaultReturnValue = (MyBehaviorTreeState)((byte)ob.DefaultReturnValue);
            m_decoratorLogicName = ob.DecoratorLogic.GetType().Name;

            m_decoratorLogic = GetDecoratorLogic(ob.DecoratorLogic);
            m_decoratorLogic.Construct(ob.DecoratorLogic);

            if (ob.BTNode != null)
            {
                m_child = MyBehaviorTreeNodeFactory.CreateBTNode(ob.BTNode);
                m_child.Construct(ob.BTNode, treeDesc);
            }
        }


        public override MyBehaviorTreeState Tick(IMyBot bot, MyPerTreeBotMemory botTreeMemory)
        {
            var decoratorMemory = botTreeMemory.GetNodeMemoryByIndex(MemoryIndex) as MyBehaviorTreeDecoratorNodeMemory;

            if (m_child == null)
                return m_defaultReturnValue;

            if (decoratorMemory.ChildState != MyBehaviorTreeState.RUNNING)
            {
                m_decoratorLogic.Update(decoratorMemory.DecoratorLogicMemory);
                if (m_decoratorLogic.CanRun(decoratorMemory.DecoratorLogicMemory))
                {
                    return TickChild(bot, botTreeMemory, decoratorMemory);
                }
                else
                {
                    if (IsRunningStateSource)
                        bot.BotMemory.ProcessLastRunningNode(this);

                    botTreeMemory.GetNodeMemoryByIndex(MemoryIndex).NodeState = m_defaultReturnValue;
                    return m_defaultReturnValue;
                }
            }
            else
            {
                return TickChild(bot, botTreeMemory, decoratorMemory);
            }
        }

        private MyBehaviorTreeState TickChild(IMyBot bot, MyPerTreeBotMemory botTreeMemory, MyBehaviorTreeDecoratorNodeMemory thisMemory)
        {
            bot.BotMemory.RememberNode(m_child.MemoryIndex);
            var state = m_child.Tick(bot, botTreeMemory);
            thisMemory.NodeState = state;
            thisMemory.ChildState = state;
            if (state != MyBehaviorTreeState.RUNNING)
            {
                bot.BotMemory.ForgetNode();
            }
            return state;
        }

        public override void PostTick(IMyBot bot, MyPerTreeBotMemory botTreeMemory)
        {
            base.PostTick(bot, botTreeMemory);
            var decoratorMemory = botTreeMemory.GetNodeMemoryByIndex(MemoryIndex) as MyBehaviorTreeDecoratorNodeMemory;
            if (decoratorMemory.ChildState != MyBehaviorTreeState.NOT_TICKED)
            {
                decoratorMemory.PostTickMemory();
                if (m_child != null)
                    m_child.PostTick(bot, botTreeMemory);
            }
            else
            {
                if (IsRunningStateSource)
                    decoratorMemory.PostTickMemory();
            }
        }

        public override void DebugDraw(VRageMath.Vector2 position, VRageMath.Vector2 size, List<MyBehaviorTreeNodeMemory> nodesMemory)
        {
        }

        public override bool IsRunningStateSource
        {
            get { return m_defaultReturnValue == MyBehaviorTreeState.RUNNING; }
        }

        // MW:TODO refactor bleh
        private static IMyDecoratorLogic GetDecoratorLogic(MyObjectBuilder_BehaviorTreeDecoratorNode.Logic logicData)
        {
            if (logicData is MyObjectBuilder_BehaviorTreeDecoratorNode.TimerLogic)
                return new MyBehaviorTreeDecoratorTimerLogic();
            else if (logicData is MyObjectBuilder_BehaviorTreeDecoratorNode.CounterLogic)
                return new MyBehaviorTreeDecoratorCounterLogic();
            else
                Debug.Fail("Unsupported type");
            return null;
        }

        public override MyBehaviorTreeNodeMemory GetNewMemoryObject()
        {
            var output = base.GetNewMemoryObject() as MyBehaviorTreeDecoratorNodeMemory;
            output.DecoratorLogicMemory = m_decoratorLogic.GetNewMemoryObject();
            return output;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int result = base.GetHashCode();
                result = (result * 397) ^ m_child.GetHashCode();
                result = (result * 397) ^ m_decoratorLogic.GetHashCode();
                result = (result * 397) ^ m_decoratorLogicName.GetHashCode();
                result = (result * 397) ^ DecoratorDefaultReturnValue.GetHashCode();
                return result;
            }
        }
    }
}
