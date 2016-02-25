using Sandbox.Game.AI.BehaviorTree;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using VRage.Game;

namespace Sandbox.Game.AI
{
    public class MyBotMemory
    {
        private IMyBot m_memoryUser;
        private MyBehaviorTree m_behaviorTree;

        private MyPerTreeBotMemory m_treeBotMemory;
        public MyPerTreeBotMemory CurrentTreeBotMemory 
        {
            get { return m_treeBotMemory; }
            private set { m_treeBotMemory = value; } 
        }

        private Stack<int> m_newNodePath;
        private HashSet<int> m_oldNodePath;
        public bool HasOldPath { get { return m_oldNodePath.Count > 0; } }
        public int LastRunningNodeIndex { get; private set; }
        public bool HasPathToSave { get { return m_newNodePath.Count > 0; } }
        public int TickCounter { get; private set; } // Can be used by actions to check, whether something happened in the same frame

        public MyBotMemory Clone() 
        {
            // creates copy of current memory state
            MyBotMemory copy = new MyBotMemory(m_memoryUser);
            copy.m_behaviorTree = m_behaviorTree;
            MyObjectBuilder_BotMemory memoryBuilder = new MyObjectBuilder_BotMemory();
            memoryBuilder = GetObjectBuilder();
            copy.Init(memoryBuilder);
            return copy;
        }

        public MyBotMemory(IMyBot bot)
        {
            LastRunningNodeIndex = -1;
            m_memoryUser = bot;
            m_newNodePath = new Stack<int>(20);
            m_oldNodePath = new HashSet<int>();
        }

        public void Init(MyObjectBuilder_BotMemory builder)
        {
            if (builder.BehaviorTreeMemory != null)
            {
                var treeBotMemory = new MyPerTreeBotMemory();
                foreach (var nodeMemoryBuilder in builder.BehaviorTreeMemory.Memory)
                {
                    var nodeMemoryObj = MyBehaviorTreeNodeMemoryFactory.CreateNodeMemory(nodeMemoryBuilder);
                    nodeMemoryObj.Init(nodeMemoryBuilder);
                    treeBotMemory.AddNodeMemory(nodeMemoryObj);
                }

                if (builder.BehaviorTreeMemory.BlackboardMemory != null)
                {
                    foreach (var bbMemInstance in builder.BehaviorTreeMemory.BlackboardMemory)
                    {
                        treeBotMemory.AddBlackboardMemoryInstance(bbMemInstance.MemberName, bbMemInstance.Value);
                    }
                }
                CurrentTreeBotMemory = treeBotMemory;
            }

            if (builder.OldPath != null)
                for (int i = 0; i < builder.OldPath.Count; i++)
                    m_oldNodePath.Add(i);
            if (builder.NewPath != null)
                for (int i = 0; i < builder.NewPath.Count; i++)
                    m_newNodePath.Push(builder.NewPath[i]);

            LastRunningNodeIndex = builder.LastRunningNodeIndex;
            TickCounter = 0;
        }

        public MyObjectBuilder_BotMemory GetObjectBuilder()
        {
            var builder = new MyObjectBuilder_BotMemory();
            builder.LastRunningNodeIndex = LastRunningNodeIndex;
            builder.NewPath = m_newNodePath.ToList();
            builder.OldPath = m_oldNodePath.ToList();

            // tree memory + blackboard
            var behaviorTreeMemory = new MyObjectBuilder_BotMemory.BehaviorTreeNodesMemory();
            behaviorTreeMemory.BehaviorName = m_behaviorTree.BehaviorTreeName;
            behaviorTreeMemory.Memory = new List<MyObjectBuilder_BehaviorTreeNodeMemory>(CurrentTreeBotMemory.NodesMemoryCount);
            foreach (var nodeMemory in CurrentTreeBotMemory.NodesMemory)
                behaviorTreeMemory.Memory.Add(nodeMemory.GetObjectBuilder());
            behaviorTreeMemory.BlackboardMemory = new List<MyObjectBuilder_BotMemory.BehaviorTreeBlackboardMemory>();
            foreach (var bbMemInstance in CurrentTreeBotMemory.BBMemory)
            {
                var bbMemoryBuilder = new MyObjectBuilder_BotMemory.BehaviorTreeBlackboardMemory();
                bbMemoryBuilder.MemberName = bbMemInstance.Key.ToString();
                bbMemoryBuilder.Value = bbMemInstance.Value;
                behaviorTreeMemory.BlackboardMemory.Add(bbMemoryBuilder);
            }
            builder.BehaviorTreeMemory = behaviorTreeMemory;

            return builder;
        }

        public void AssignBehaviorTree(MyBehaviorTree behaviorTree)
        {
            if (CurrentTreeBotMemory == null && (m_behaviorTree == null || behaviorTree.BehaviorTreeId == m_behaviorTree.BehaviorTreeId))
            {
                CurrentTreeBotMemory = CreateBehaviorTreeMemory(behaviorTree);
            }
            else
            {
                bool isValid = ValidateMemoryForBehavior(behaviorTree);
                if (!isValid)
                {
                    CurrentTreeBotMemory.Clear();
                    ClearPathMemory(false);
                    ResetMemoryInternal(behaviorTree, CurrentTreeBotMemory);
                }
            }

            m_behaviorTree = behaviorTree;
        }

        private MyPerTreeBotMemory CreateBehaviorTreeMemory(MyBehaviorTree behaviorTree)
        {
            var treeMemory = new MyPerTreeBotMemory(); 
            ResetMemoryInternal(behaviorTree, treeMemory);
            return treeMemory;
        }

        public bool ValidateMemoryForBehavior(MyBehaviorTree behaviorTree)
        {
            bool isValid = true;
            if (CurrentTreeBotMemory.NodesMemoryCount != behaviorTree.TotalNodeCount)
                isValid = false;
            else
            {
                for (int i = 0; i < CurrentTreeBotMemory.NodesMemoryCount; i++)
                {
                    var nodeMemory = CurrentTreeBotMemory.GetNodeMemoryByIndex(i);
                    if (nodeMemory.GetType() != behaviorTree.GetNodeByIndex(i).MemoryType)
                    {
                        isValid = false;
                        break;
                    }
                }
            }
            return isValid;
        }

        public void PreTickClear()
        {
            if (HasPathToSave)
                PrepareForNewNodePath();
            CurrentTreeBotMemory.ClearNodesData();
            TickCounter = TickCounter + 1;
        }

        public void ClearPathMemory(bool postTick)
        {
            if (postTick)
                PostTickPaths();
            m_newNodePath.Clear();
            m_oldNodePath.Clear();
            LastRunningNodeIndex = -1;
        }

        public void ResetMemory(bool clearMemory = false)
        {
            if (m_behaviorTree == null)
                return;
            if (clearMemory)
                ClearPathMemory(true);
            CurrentTreeBotMemory.Clear();
            ResetMemoryInternal(m_behaviorTree, CurrentTreeBotMemory);
        }

        public void UnassignCurrentBehaviorTree()
        {
            ClearPathMemory(true);
            CurrentTreeBotMemory = null;
            m_behaviorTree = null;
        }

        private void ResetMemoryInternal(MyBehaviorTree behaviorTree, MyPerTreeBotMemory treeMemory)
        {
            for (int i = 0; i < behaviorTree.TotalNodeCount; i++)
            {
                treeMemory.AddNodeMemory(behaviorTree.GetNodeByIndex(i).GetNewMemoryObject());
            }
        }

        private void ClearOldPath()
        {
            m_oldNodePath.Clear();
            LastRunningNodeIndex = -1;
        }

        private void PostTickPaths()
        {
            if (m_behaviorTree != null)
            {
                m_behaviorTree.CallPostTickOnPath(m_memoryUser, CurrentTreeBotMemory, m_oldNodePath);
                m_behaviorTree.CallPostTickOnPath(m_memoryUser, CurrentTreeBotMemory, m_newNodePath);
            }
        }

        private void PostTickOldPath()
        {
            if (HasOldPath)
            {
                m_oldNodePath.ExceptWith(m_newNodePath);
                m_behaviorTree.CallPostTickOnPath(m_memoryUser, CurrentTreeBotMemory, m_oldNodePath);
                ClearOldPath();
            }
        }

        public void RememberNode(int nodeIndex)
        {
            m_newNodePath.Push(nodeIndex);
        }

        public void ForgetNode()
        {
            m_newNodePath.Pop();
        }

        public void PrepareForNewNodePath()
        {
            //Debug.Assert(m_oldNodePath.Count == 0, "Old node path is not empty");
            m_oldNodePath.Clear();
            m_oldNodePath.UnionWith(m_newNodePath);
            LastRunningNodeIndex = m_newNodePath.Peek();
            m_newNodePath.Clear();
        }

        public void ProcessLastRunningNode(MyBehaviorTreeNode node)
        {
            if (LastRunningNodeIndex == -1)
                return;

            if (LastRunningNodeIndex != node.MemoryIndex)
            {
                PostTickOldPath();
            }
            else
            {
                ClearOldPath();
            }
        }
    }
}
