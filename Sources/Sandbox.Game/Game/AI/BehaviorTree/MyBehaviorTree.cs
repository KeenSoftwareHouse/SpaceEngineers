using Sandbox.Definitions;
using Sandbox.Engine.Utils;
using Sandbox.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using VRageMath;
using VRage.Win32;
using VRage.Utils;
using VRage.Utils;
using VRage.Library.Utils;

namespace Sandbox.Game.AI.BehaviorTree
{
    public class MyBehaviorTree
    { 
        public class MyBehaviorTreeDesc
        {
            public List<MyBehaviorTreeNode> Nodes { get; private set; }
            public HashSet<MyStringId> ActionIds { get; private set; }
            public int MemorableNodesCounter { get; set; }

            public MyBehaviorTreeDesc()
            {
                Nodes = new List<MyBehaviorTreeNode>(20);
                ActionIds = new HashSet<MyStringId>(MyStringId.Comparer);
                MemorableNodesCounter = 0;
            }
        }

        private static List<MyStringId> m_tmpHelper = new List<MyStringId>();

        private MyBehaviorTreeNode m_root;
        private MyBehaviorTreeDesc m_treeDesc;

        public int TotalNodeCount { get { return m_treeDesc.Nodes.Count; } }

        private MyBehaviorDefinition m_behaviorDefinition;
        public MyBehaviorDefinition BehaviorDefinition { get { return m_behaviorDefinition; } }
        public string BehaviorTreeName { get { return m_behaviorDefinition.Id.SubtypeName; } }
        public MyStringHash BehaviorTreeId { get { return m_behaviorDefinition.Id.SubtypeId; } }

        public MyBehaviorTree(MyBehaviorDefinition def)
        {
            m_behaviorDefinition = def;
            m_treeDesc = new MyBehaviorTreeDesc();
        }

        public void ReconstructTree(MyBehaviorDefinition def)
        {
            m_behaviorDefinition = def;
            Construct();
        }

        public void Construct()
        {
            ClearData();
            m_root = new MyBehaviorTreeRoot();
            m_root.Construct(m_behaviorDefinition.FirstNode, m_treeDesc);
        }

        public void ClearData()
        {
            m_treeDesc.MemorableNodesCounter = 0;
            m_treeDesc.ActionIds.Clear();
            m_treeDesc.Nodes.Clear();
        }

        public void Tick(IMyBot bot)
        {
            m_root.Tick(bot, bot.BotMemory.CurrentTreeBotMemory);
        }

        public void CallPostTickOnPath(IMyBot bot, MyPerTreeBotMemory botTreeMemory, IEnumerable<int> postTickNodes)
        {
            foreach (var nodeIdx in postTickNodes)
            {
                m_treeDesc.Nodes[nodeIdx].PostTick(bot, botTreeMemory);
            }
        }

        public bool IsCompatibleWithBot(ActionCollection botActions)
        {
            foreach (MyStringId actionId in m_treeDesc.ActionIds)
            {
                if (!botActions.ContainsActionDesc(actionId))
                {
                    m_tmpHelper.Add(actionId);
                }
            }

            if (m_tmpHelper.Count > 0)
            {
                StringBuilder failText = new StringBuilder("Error! The behavior tree is not compatible with the bot. Missing bot actions: ");
                foreach (var action in m_tmpHelper)
                {
                    failText.Append(action.ToString());
                    failText.Append(", ");
                }
                System.Diagnostics.Debug.Fail(failText.ToString());
                m_tmpHelper.Clear();
                return false;
            }
            else
            {
                return true;
            }
        }

        public MyBehaviorTreeNode GetNodeByIndex(int index)
        {
            if (index >= m_treeDesc.Nodes.Count)
                return null;
            return m_treeDesc.Nodes[index];
        }

        public override int GetHashCode()
        {
            return m_root.GetHashCode();
        }
    }
}
