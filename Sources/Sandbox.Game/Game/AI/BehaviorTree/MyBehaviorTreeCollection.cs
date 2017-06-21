using Sandbox.Common.ObjectBuilders.Definitions;
using Sandbox.Definitions;
using Sandbox.Engine.Utils;
using Sandbox.Game.Gui;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using VRage.FileSystem;
using VRage.Game;
using VRage.Game.Definitions;
using VRage.Game.SessionComponents;
using VRage.Library.Utils;
using VRage.ObjectBuilders;
using VRage.Profiler;
using VRage.Utils;
using VRage.Win32;

namespace Sandbox.Game.AI.BehaviorTree
{
    public class MyBehaviorTreeCollection
    {
        #region InterOp
        private IntPtr m_toolWindowHandle = IntPtr.Zero;

#if !XB1
        public bool TryGetValidToolWindow(out IntPtr windowHandle)
        {
            windowHandle = IntPtr.Zero;
            windowHandle = WinApi.FindWindowInParent("VRageEditor", "BehaviorTreeWindow");
            if (windowHandle == IntPtr.Zero)
                windowHandle = WinApi.FindWindowInParent("Behavior tree tool", "BehaviorTreeWindow");
            return windowHandle != IntPtr.Zero;
        }

        private void SendSelectedTreeForDebug(MyBehaviorTree behaviorTree)
        {
            if (MySessionComponentExtDebug.Static == null)
                return;
            DebugSelectedTreeHashSent = true;
            DebugCurrentBehaviorTree = behaviorTree.BehaviorTreeName;
            var msg = new MyExternalDebugStructures.SelectedTreeMsg()
            {
                BehaviorTreeName = behaviorTree.BehaviorTreeName
            };
            MySessionComponentExtDebug.Static.SendMessageToClients(msg);
        }

        private void SendDataToTool(IMyBot bot, MyPerTreeBotMemory botTreeMemory)
        {
            if (!DebugIsCurrentTreeVerified || DebugLastWindowHandle.ToInt32() != m_toolWindowHandle.ToInt32())
            {
                int hash = m_BTDataByName[m_botBehaviorIds[bot]].BehaviorTree.GetHashCode();
                IntPtr toSend = new IntPtr(hash);
                WinApi.PostMessage(m_toolWindowHandle, MyWMCodes.BEHAVIOR_TOOL_VALIDATE_TREE, toSend, IntPtr.Zero);
                DebugIsCurrentTreeVerified = true;
                DebugLastWindowHandle = new IntPtr(m_toolWindowHandle.ToInt32());
            }

            WinApi.PostMessage(m_toolWindowHandle, MyWMCodes.BEHAVIOR_TOOL_CLEAR_NODES, IntPtr.Zero, IntPtr.Zero);
            for (int i = 0; i < botTreeMemory.NodesMemoryCount; ++i)
            {
                var state = botTreeMemory.GetNodeMemoryByIndex(i).NodeState;
                if (state == MyBehaviorTreeState.NOT_TICKED) continue;
                WinApi.PostMessage(m_toolWindowHandle, MyWMCodes.BEHAVIOR_TOOL_SET_DATA, new IntPtr((uint)i), new IntPtr((int)state));
            }
            WinApi.PostMessage(m_toolWindowHandle, MyWMCodes.BEHAVIOR_TOOL_END_SENDING_DATA, IntPtr.Zero, IntPtr.Zero);
        }
#endif // !XB1
        #endregion
      
        public const int UPDATE_COUNTER = 10;
        public const int INIT_UPDATE_COUNTER = UPDATE_COUNTER - 2;

        private class BotData
        {
            public IMyBot Bot;
            public int UpdateCounter = INIT_UPDATE_COUNTER;

            public BotData(IMyBot bot) { Bot = bot; }
        }

        private class BTData : IEqualityComparer<BotData>
        {
            private static readonly BotData SearchData = new BotData(null);

            public MyBehaviorTree BehaviorTree;
            public HashSet<BotData> BotsData;

            public BTData(MyBehaviorTree behaviorTree)
            {
                BehaviorTree = behaviorTree;
                BotsData = new HashSet<BotData>(this);
            }

            public bool RemoveBot(IMyBot bot)
            {
                SearchData.Bot = bot;
                return BotsData.Remove(SearchData);
            }

            public bool ContainsBot(IMyBot bot)
            {
                SearchData.Bot = bot;
                return BotsData.Contains(SearchData);
            }

            bool IEqualityComparer<BotData>.Equals(BotData x, BotData y)
            {
                return x.Bot == y.Bot;
            }

            int IEqualityComparer<BotData>.GetHashCode(BotData obj)
            {
                return obj.Bot.GetHashCode();
            }
        }

        public static readonly string DEFAULT_EXTENSION = ".sbc";

        private Dictionary<MyStringHash, BTData> m_BTDataByName;
        private Dictionary<IMyBot, MyStringHash> m_botBehaviorIds;

        public bool DebugSelectedTreeHashSent { get; private set; }
        public IntPtr DebugLastWindowHandle { get; private set; }
        public bool DebugIsCurrentTreeVerified { get; private set; }
        private IMyBot m_debugBot = null;
        public IMyBot DebugBot 
        {
            get { return m_debugBot; }
            set
            {
                m_debugBot = value;
                DebugSelectedTreeHashSent = false;
            }
        }
        public bool DebugBreakDebugging { get; set; }
        public string DebugCurrentBehaviorTree { get; private set; }

        public MyBehaviorTreeCollection()
        {
            m_BTDataByName = new Dictionary<MyStringHash, BTData>(MyStringHash.Comparer);
            m_botBehaviorIds = new Dictionary<IMyBot, MyStringHash>();
            DebugIsCurrentTreeVerified = false;

            foreach (var behavior in MyDefinitionManager.Static.GetBehaviorDefinitions())
            {
                BuildBehaviorTree(behavior);
            }
        }

        public void Update()
        {
            ProfilerShort.Begin("Behaviors update");
            foreach (var bt in m_BTDataByName.Values)
            {
                var behaviorTree = bt.BehaviorTree;
                foreach (var data in bt.BotsData)
                {
                    var bot = data.Bot;
                    if (bot.IsValidForUpdate && ++data.UpdateCounter > UPDATE_COUNTER)
                    {
                        if ( MyFakes.DEBUG_BEHAVIOR_TREE )
                        {
                            if (!MyFakes.DEBUG_BEHAVIOR_TREE_ONE_STEP)
                                continue;
                            MyFakes.DEBUG_BEHAVIOR_TREE_ONE_STEP = false;
                        }

                        data.UpdateCounter = 0;
                        bot.BotMemory.PreTickClear();
                        behaviorTree.Tick(bot);

                        if (MyFakes.ENABLE_BEHAVIOR_TREE_TOOL_COMMUNICATION && DebugBot == data.Bot && !DebugBreakDebugging && MyDebugDrawSettings.DEBUG_DRAW_BOTS)
                        {
#if !XB1
                            ProfilerShort.Begin("Sending debug data");
                            if (TryGetValidToolWindow(out m_toolWindowHandle))
                            {
                                if (!DebugSelectedTreeHashSent || m_toolWindowHandle != DebugLastWindowHandle
                                    || DebugCurrentBehaviorTree != m_botBehaviorIds[DebugBot].String)
                                    SendSelectedTreeForDebug(behaviorTree);
                                SendDataToTool(data.Bot, data.Bot.BotMemory.CurrentTreeBotMemory);
                            }
                            ProfilerShort.End();
#endif // !XB1
                        }
                    }
                }
            }
            ProfilerShort.End();
        }

        public bool AssignBotToBehaviorTree(string behaviorName, IMyBot bot)
        {
            var treeId = MyStringHash.TryGet(behaviorName);
            Debug.Assert(m_BTDataByName.ContainsKey(treeId), "The given tree does not exist in the collection.");
            if (treeId == MyStringHash.NullOrEmpty || !m_BTDataByName.ContainsKey(treeId))
                return false;
            else
                return AssignBotToBehaviorTree(m_BTDataByName[treeId].BehaviorTree, bot);
        }

        public bool AssignBotToBehaviorTree(MyBehaviorTree behaviorTree, IMyBot bot)
        {
            Debug.Assert(!m_BTDataByName[behaviorTree.BehaviorTreeId].ContainsBot(bot), "Bot has already been added.");
            Debug.Assert(behaviorTree.IsCompatibleWithBot(bot.ActionCollection), "Bot is not compatible with the behavior tree.");
            if (!behaviorTree.IsCompatibleWithBot(bot.ActionCollection))
                return false;
            AssignBotBehaviorTreeInternal(behaviorTree, bot);
            return true;
        }

        private void AssignBotBehaviorTreeInternal(MyBehaviorTree behaviorTree, IMyBot bot)
        {
            bot.BotMemory.AssignBehaviorTree(behaviorTree);
            m_BTDataByName[behaviorTree.BehaviorTreeId].BotsData.Add(new BotData(bot));
            m_botBehaviorIds[bot] = behaviorTree.BehaviorTreeId;
        }

        public void UnassignBotBehaviorTree(IMyBot bot)
        {
            m_BTDataByName[m_botBehaviorIds[bot]].RemoveBot(bot);
            bot.BotMemory.UnassignCurrentBehaviorTree();
            m_botBehaviorIds[bot] = MyStringHash.NullOrEmpty;
        }

        public MyBehaviorTree TryGetBehaviorTreeForBot(IMyBot bot)
        {
            BTData data = null;
            m_BTDataByName.TryGetValue(m_botBehaviorIds[bot], out data);
            if (data != null)
                return data.BehaviorTree;
            Debug.Assert(false, "Behavior not found");
            return null;
        }

        public string GetBehaviorName(IMyBot bot)
        {
            MyStringHash hash;
            m_botBehaviorIds.TryGetValue(bot, out hash);
            return hash.String;
        }

        public void SetBehaviorName(IMyBot bot, string behaviorName)
        {
            m_botBehaviorIds[bot] = MyStringHash.GetOrCompute(behaviorName);
        }

        private bool BuildBehaviorTree(MyBehaviorDefinition behaviorDefinition)
        {
            Debug.Assert(!m_BTDataByName.ContainsKey(behaviorDefinition.Id.SubtypeId), "Tree with given behavior definition already exists.");
            if (m_BTDataByName.ContainsKey(behaviorDefinition.Id.SubtypeId))
                return false;

            MyBehaviorTree newInstance = new MyBehaviorTree(behaviorDefinition);
            newInstance.Construct();
            BTData behaviorTreeData = new BTData(newInstance);
            m_BTDataByName.Add(behaviorDefinition.Id.SubtypeId, behaviorTreeData);
            return true;
        }

        public bool ChangeBehaviorTree(string behaviorTreeName, IMyBot bot)
        {
            bool assign = false;
            MyBehaviorTree behaviorTree = null;
            if (!TryGetBehaviorTreeByName(behaviorTreeName, out behaviorTree))
                return false;
            if (!behaviorTree.IsCompatibleWithBot(bot.ActionCollection))
                return false;
            var tree = TryGetBehaviorTreeForBot(bot);
            if (tree != null)
            {
                if (tree.BehaviorTreeId == behaviorTree.BehaviorTreeId)
                    assign = false;
                else
                {
                    UnassignBotBehaviorTree(bot);
                    assign = true;
                }
            }
            else
                assign = true;

            if (assign)
                AssignBotBehaviorTreeInternal(behaviorTree, bot);
            return assign;
        }

        public bool RebuildBehaviorTree(MyBehaviorDefinition newDefinition, out MyBehaviorTree outBehaviorTree)
        {
            if (m_BTDataByName.ContainsKey(newDefinition.Id.SubtypeId))
            {
                outBehaviorTree = m_BTDataByName[newDefinition.Id.SubtypeId].BehaviorTree;
                outBehaviorTree.ReconstructTree(newDefinition);
                return true;
            }
            else
            {
                outBehaviorTree = null;
                return false;
            }
        }

        public bool HasBehavior(MyStringHash id)
        {
            return m_BTDataByName.ContainsKey(id);
        }

        public bool TryGetBehaviorTreeByName(string name, out MyBehaviorTree behaviorTree)
        {
            MyStringHash stringId;
            MyStringHash.TryGet(name, out stringId);
            if (stringId != MyStringHash.NullOrEmpty && m_BTDataByName.ContainsKey(stringId))
            {
                behaviorTree = m_BTDataByName[stringId].BehaviorTree;
                return behaviorTree != null;
            }
            else
            {
                Debug.Fail("Could not find a behavior tree with provided name: " + name);
                behaviorTree = null;
                return false;
            }
        }

        public static bool LoadUploadedBehaviorTree(out MyBehaviorDefinition definition)
        {
            string dataPath = MyFileSystem.UserDataPath;
            MyBehaviorDefinition uploadedDefinition = LoadBehaviorTreeFromFile(Path.Combine(dataPath, "UploadTree" + DEFAULT_EXTENSION));
            definition = uploadedDefinition;
            return definition != null;
        }

        private static MyBehaviorDefinition LoadBehaviorTreeFromFile(string path)
        {
            MyObjectBuilder_Definitions allDefinitions = null;
            MyObjectBuilderSerializer.DeserializeXML(path, out allDefinitions);

            if (allDefinitions != null && allDefinitions.AIBehaviors != null && allDefinitions.AIBehaviors.Length > 0)
            {
                var firstDef = allDefinitions.AIBehaviors[0]; // only one tree can be uploaded at one time

                MyBehaviorDefinition behaviorDefinition = new MyBehaviorDefinition();
                MyModContext context = new MyModContext();
                context.Init("BehaviorDefinition", Path.GetFileName(path));
                behaviorDefinition.Init(firstDef, context);
                return behaviorDefinition;
            }
            return null;
        }
    }
}
