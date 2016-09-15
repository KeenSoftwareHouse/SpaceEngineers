using Sandbox.Engine.Utils;
using Sandbox.Game.AI.BehaviorTree;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using VRage;
using VRage.Collections;
using VRageMath;
using VRage.Utils;
using Sandbox.Game.AI.Logic;
using Sandbox.Game.Entities.Character;
using Sandbox.Game.World;
using Sandbox.ModAPI;
using VRage.Game;
using VRage.Game.ModAPI;
using VRage.Profiler;

namespace Sandbox.Game.AI
{
    public class MyBotCollection
    {
        private Dictionary<int, IMyBot> m_allBots;
        private Dictionary<Type, ActionCollection> m_botActions;
        private MyBehaviorTreeCollection m_behaviorTreeCollection;
        private Dictionary<string, int> m_botsCountPerBehavior;
        private List<int> m_botsQueue;

        private int m_botIndex = -1;

        public bool HasBot { get { return m_botsQueue.Count > 0; } }

        public int TotalBotCount
        {
            get;
            set;
        }

        public Dictionary<int, IMyBot> BotsDictionary { get { return m_allBots; } }

        public MyBotCollection(MyBehaviorTreeCollection behaviorTreeCollection)
        {
            m_behaviorTreeCollection = behaviorTreeCollection;
            m_allBots = new Dictionary<int, IMyBot>(8);
            m_botActions = new Dictionary<Type, ActionCollection>(8);
            m_botsQueue = new List<int>(8);
            m_botsCountPerBehavior = new Dictionary<string, int>();
        }

        public void UnloadData()
        {
            foreach (var botEntry in m_allBots)
            {
                botEntry.Value.Cleanup();
            }
        }

        public void Update()
        {
            ProfilerShort.Begin("Update - BotCollection");
            foreach (var botEntry in m_allBots)
            {
                botEntry.Value.Update();
            }
            ProfilerShort.End();
        }

        public void AddBot(int botHandler, IMyBot newBot)
        {
            Debug.Assert(!m_allBots.ContainsKey(botHandler), "Bot with the given handler already exists!");
            if (m_allBots.ContainsKey(botHandler)) return;

            ActionCollection botActions = null;
            if (!m_botActions.ContainsKey(newBot.BotActions.GetType()))
            {
                botActions = ActionCollection.CreateActionCollection(newBot);
                m_botActions[newBot.GetType()] = botActions;
            }
            else
            {
                botActions = m_botActions[newBot.GetType()];
            }

            newBot.InitActions(botActions);
            if (string.IsNullOrEmpty(newBot.BehaviorSubtypeName))
                m_behaviorTreeCollection.AssignBotToBehaviorTree(newBot.BotDefinition.BotBehaviorTree.SubtypeName, newBot); 
            else
                m_behaviorTreeCollection.AssignBotToBehaviorTree(newBot.BehaviorSubtypeName, newBot); 
            m_allBots.Add(botHandler, newBot);
            m_botsQueue.Add(botHandler);

            if (m_botsCountPerBehavior.ContainsKey(newBot.BotDefinition.BehaviorType))
                m_botsCountPerBehavior[newBot.BotDefinition.BehaviorType]++;
            else
                m_botsCountPerBehavior[newBot.BotDefinition.BehaviorType] = 1;

#if DEBUG
            if (Sandbox.Game.Gui.MyMichalDebugInputComponent.Static != null && Sandbox.Game.Gui.MyMichalDebugInputComponent.Static.OnSelectDebugBot)
            {
                Sandbox.Engine.Utils.MyDebugDrawSettings.DEBUG_DRAW_BOTS = true;
                SelectBotForDebugging(newBot);
            }
#endif
        }

        public void ResetBots(string treeName)
        {
            foreach (var bot in m_allBots.Values)
            {
                if (bot.BehaviorSubtypeName == treeName)
                {
                    bot.Reset();
                }
            }
        }

        public void CheckCompatibilityWithBots(MyBehaviorTree behaviorTree)
        {
            foreach (var bot in m_allBots.Values)
            {
                if (behaviorTree.BehaviorTreeName.CompareTo(bot.BehaviorSubtypeName) == 0)
                {
                    bool isCompatible = behaviorTree.IsCompatibleWithBot(bot.ActionCollection);
                    if (!isCompatible)
                    {
                        m_behaviorTreeCollection.UnassignBotBehaviorTree(bot);
                    }
                    else
                    {
                        bot.BotMemory.ResetMemory();
                    }
                }
            }
        }

        public int GetHandleToFirstBot()
        {
            if (m_botsQueue.Count > 0)
                return m_botsQueue[0];
            else
                return -1;
        }

        public int GetHandleToFirstBot(string behaviorType)
        {
            foreach (var handle in m_botsQueue)
            {
                if (m_allBots[handle].BotDefinition.BehaviorType == behaviorType)
                {
                    return handle;
                }
            }
            return -1;
        }

        public BotType GetBotType(int botHandler)
        {
            if (m_allBots.ContainsKey(botHandler))
            {
                var botLogic = m_allBots[botHandler].BotLogic;
                if (botLogic != null)
                    return botLogic.BotType;
            }
            return BotType.UNKNOWN;
        }

        public void TryRemoveBot(int botHandler)
        {

            IMyBot bot = null;
            m_allBots.TryGetValue(botHandler, out bot);
            if (bot == null) return;

            var botBehaviorType = bot.BotDefinition.BehaviorType;
            bot.Cleanup();

            if (m_botIndex != -1)
            {
                if (m_behaviorTreeCollection.DebugBot == bot)
                    m_behaviorTreeCollection.DebugBot = null;
                int removedBotIndex = m_botsQueue.IndexOf(botHandler);
                if (removedBotIndex < m_botIndex)
                    m_botIndex--;
                else if (removedBotIndex == m_botIndex)
                    m_botIndex = -1;
            }
            m_allBots.Remove(botHandler);
            m_botsQueue.Remove(botHandler);

            Debug.Assert(m_botsCountPerBehavior[botBehaviorType] > 0, "Bots count of this type is equal to zero. Invalid state");
            m_botsCountPerBehavior[botBehaviorType]--;
        }

        public int GetCurrentBotsCount(string behaviorType)
        {
            if (!m_botsCountPerBehavior.ContainsKey(behaviorType))
                return 0;
            else
                return m_botsCountPerBehavior[behaviorType];
        }

        public BotType TryGetBot<BotType>(int botHandler) where BotType : class, IMyBot
        {
            IMyBot bot = null;

            m_allBots.TryGetValue(botHandler, out bot);
            if (bot == null) return (BotType)null;

            return bot as BotType;
        }

        public DictionaryReader<int, IMyBot> GetAllBots()
        {
            return new DictionaryReader<int, IMyBot>(m_allBots);
        }

        public void GetBotsData(List<MyObjectBuilder_AIComponent.BotData> botDataList)
        {
            foreach (var keyValPair in m_allBots)
            {
                var newData = new MyObjectBuilder_AIComponent.BotData()
                {
                    BotBrain = keyValPair.Value.GetObjectBuilder(),
                    PlayerHandle = keyValPair.Key   
                };
                botDataList.Add(newData);
            }
        }

        public int GetCreatedBotCount()
        {
            int count = 0;
            foreach (var bot in m_allBots.Values)
            {
                if (bot.CreatedByPlayer)
                    count++;
            }
            return count;
        }

        public int GetGeneratedBotCount()
        {
            int count = 0;
            foreach (var bot in m_allBots.Values)
            {
                if (!bot.CreatedByPlayer)
                    count++;
            }
            return count;
        }
#region Debug
        internal void SelectBotForDebugging(IMyBot bot)
        {
            m_behaviorTreeCollection.DebugBot = bot;
            for (int i = 0; i < m_botsQueue.Count; i++)
            {
                if (m_allBots[m_botsQueue[i]] == bot)
                {
                    m_botIndex = i;
                    break;
                }
            }
        }

        public bool IsBotSelectedForDegugging(IMyBot bot)
        {
            return m_behaviorTreeCollection.DebugBot == bot;
        }

        internal void SelectBotForDebugging(int index)
        {
            if (m_botIndex != -1)
            {
                var botHandle = m_botsQueue[index];
                m_behaviorTreeCollection.DebugBot = m_allBots[botHandle];
            }
        }

        internal void DebugSelectNextBot()
        {
            m_botIndex++;
            if (m_botIndex == m_botsQueue.Count)
            {
                if (m_botsQueue.Count == 0)
                    m_botIndex = -1;
                else
                    m_botIndex = 0;
            }
            
            SelectBotForDebugging(m_botIndex);
        }

        internal void DebugSelectPreviousBot()
        {
            m_botIndex--;
            if (m_botIndex < 0)
            {
                if (m_botsQueue.Count > 0)
                    m_botIndex = m_botsQueue.Count - 1;
                else
                    m_botIndex = -1;
            }

            SelectBotForDebugging(m_botIndex);
        }

        internal void DebugDrawBots()
        {
            if (!MyDebugDrawSettings.DEBUG_DRAW_BOTS) return;

            Vector2 initPos = new Vector2(0.01f, 0.4f);
            for (int i = 0; i < m_botsQueue.Count; i++)
            {
                var bot = m_allBots[m_botsQueue[i]];
                if (bot is IMyEntityBot)
                {
                    var entityBot = bot as IMyEntityBot;
                    Color labelColor = Color.Green;
                    if (m_botIndex == -1 || i != m_botIndex)
                        labelColor = Color.Red;
                    var screenCoords = Sandbox.Graphics.MyGuiManager.GetHudPixelCoordFromNormalizedCoord(initPos);
                    string str = string.Format("Bot[{0}]: {1}", i, entityBot.BehaviorSubtypeName);
                    if (bot is MyAgentBot)
                        str += (bot as MyAgentBot).LastActions.GetLastActionsString();
                    VRageRender.MyRenderProxy.DebugDrawText2D(screenCoords, str, labelColor, 0.5f, MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_CENTER);
                    var character = (entityBot.BotEntity as MyCharacter);
                    IMyFaction faction = null;
                    if (character != null)
                    {
                        var identityId = character.ControllerInfo.Controller.Player.Identity.IdentityId;
                        faction = MySession.Static.Factions.TryGetPlayerFaction(identityId);
                    }
                    if (entityBot.BotEntity != null)
                    {
                        var markerPos = entityBot.BotEntity.PositionComp.WorldAABB.Center;
                        markerPos.Y += entityBot.BotEntity.PositionComp.WorldAABB.HalfExtents.Y;
                        VRageRender.MyRenderProxy.DebugDrawText3D(markerPos, string.Format("Bot:{0}", i), labelColor, 1f, false, MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_TOP);
                        VRageRender.MyRenderProxy.DebugDrawText3D(markerPos - new Vector3(0.0f, -0.5f, 0.0f), faction == null ? "NO_FACTION" : faction.Tag, labelColor, 1f, false, MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_TOP);
                    }
                    initPos.Y += 0.02f;
                }
            }
        }

        internal void DebugDraw()
        {
            if (!MyDebugDrawSettings.ENABLE_DEBUG_DRAW || !MyDebugDrawSettings.DEBUG_DRAW_BOTS) return;

            foreach (var entry in m_allBots)
            {
                entry.Value.DebugDraw();
            }
        }
#endregion
    }
}
