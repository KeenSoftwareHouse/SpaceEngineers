using Sandbox.Common;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Definitions;
using Sandbox.Engine.Networking;
using Sandbox.Engine.Physics;
using Sandbox.Engine.Utils;
using Sandbox.Game.AI.BehaviorTree;
using Sandbox.Game.AI.Commands;
using Sandbox.Game.AI.Pathfinding;
using Sandbox.Game.Entities;
using Sandbox.Game.Gui;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.Localization;
using Sandbox.Game.Screens.Helpers;
using Sandbox.Game.World;
using Sandbox.Graphics.GUI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Forms;
using VRage;
using VRage.Input;
using VRage.Library.Utils;
using VRage.Utils;
using VRage.Win32;
using VRageMath;
using VRage.Collections;

namespace Sandbox.Game.AI
{
	public enum MyReservedEntityType
	{
		NONE,
		ENTITY,
		ENVIRONMENT_ITEM,
		VOXEL
	}

    [MySessionComponentDescriptor(MyUpdateOrder.Simulation | MyUpdateOrder.AfterSimulation, 500, typeof(MyObjectBuilder_AIComponent))]
    public class MyAIComponent : MySessionComponentBase
	{
		private struct AgentSpawnData
        {
            public MyAgentDefinition AgentDefinition;
            public Vector3D? SpawnPosition;
            public bool CreatedByPlayer;
            public int BotId;

            public AgentSpawnData(MyAgentDefinition agentDefinition, int botId, Vector3D? spawnPosition = null, bool createAlways = false)
            {
                AgentDefinition = agentDefinition;
                SpawnPosition = spawnPosition;
                CreatedByPlayer = createAlways;
                BotId = botId;
            }
        }

        public struct AgentGroupData
        {
            public MyAgentDefinition AgentDefinition;
            public int Count;

            public AgentGroupData(MyAgentDefinition agentDefinition, int count)
            {
                AgentDefinition = agentDefinition;
                Count = count;
            }
        }

        private MyBotCollection m_botCollection;
        private MyPathfinding m_pathfinding;
        private MyBehaviorTreeCollection m_behaviorTreeCollection;

        public MyBotCollection Bots { get { return m_botCollection; } }
        public MyPathfinding Pathfinding { get { return m_pathfinding; } }
        public MyBehaviorTreeCollection BehaviorTrees { get { return m_behaviorTreeCollection; } }

        public MyRandom Random;

        private Dictionary<int, MyObjectBuilder_Bot> m_loadedBotObjectBuildersByHandle;
        private List<int> m_loadedLocalPlayers;
        private List<Vector3D> m_tmpSpawnPoints = new List<Vector3D>();

        public static MyAIComponent Static;
        public static MyBotFactoryBase BotFactory;

        private int m_lastBotId = 0;
        private Dictionary<int, AgentSpawnData> m_agentsToSpawn;

        private MyHudNotification m_maxBotNotification;

        public MyAgentDefinition BotToSpawn = null;
        public MyAiCommandDefinition CommandDefinition = null;

        public event Action<int, MyBotDefinition> BotCreatedEvent;

        private MyConcurrentQueue<int> m_removeQueue;
        private MyConcurrentQueue<AgentSpawnData> m_processQueue;
        private FastResourceLock m_lock;

        public MyAIComponent()
        {
            Static = this;
            BotFactory = Activator.CreateInstance(MyPerGameSettings.BotFactoryType) as MyBotFactoryBase;
            Random = new MyRandom();
        }

        public override void LoadData()
        {
            base.LoadData();

            if (MyPerGameSettings.EnableAi)
            {
                Sync.Players.NewPlayerRequestSucceeded += PlayerCreated;
                Sync.Players.LocalPlayerLoaded += LocalPlayerLoaded;
                Sync.Players.NewPlayerRequestFailed += Players_NewPlayerRequestFailed;
                if (Sync.IsServer)
                {
                    Sync.Players.PlayerRemoved += Players_PlayerRemoved;
                    Sync.Players.PlayerRequesting += Players_PlayerRequesting;
                }

                m_pathfinding = new MyPathfinding();
                m_behaviorTreeCollection = new MyBehaviorTreeCollection();
                m_botCollection = new MyBotCollection(m_behaviorTreeCollection);
                m_loadedLocalPlayers = new List<int>();
                m_loadedBotObjectBuildersByHandle = new Dictionary<int, MyObjectBuilder_Bot>();
                m_agentsToSpawn = new Dictionary<int, AgentSpawnData>();
                m_removeQueue = new MyConcurrentQueue<int>();
                m_maxBotNotification = new MyHudNotification(MySpaceTexts.NotificationMaximumNumberBots, 2000, MyFontEnum.Red);
                m_processQueue = new MyConcurrentQueue<AgentSpawnData>();
                m_lock = new FastResourceLock();

                if (MyFakes.ENABLE_BEHAVIOR_TREE_TOOL_COMMUNICATION)
                {
                    MyMessageLoop.AddMessageHandler(MyWMCodes.BEHAVIOR_GAME_UPLOAD_TREE, OnUploadNewTree);
                    MyMessageLoop.AddMessageHandler(MyWMCodes.BEHAVIOR_GAME_STOP_SENDING, OnBreakDebugging);
                    MyMessageLoop.AddMessageHandler(MyWMCodes.BEHAVIOR_GAME_RESUME_SENDING, OnResumeDebugging);
                }

                MyToolbarComponent.CurrentToolbar.SelectedSlotChanged += CurrentToolbar_SelectedSlotChanged;
                MyToolbarComponent.CurrentToolbar.SlotActivated += CurrentToolbar_SlotActivated;
                MyToolbarComponent.CurrentToolbar.Unselected += CurrentToolbar_Unselected;
            }
        }

        public override Type[] Dependencies
        {
            get
            {
                return new Type[] { typeof(MyToolbarComponent) };
            }
        }

        public override void Init(MyObjectBuilder_SessionComponent sessionComponentBuilder)
        {
            if (!MyPerGameSettings.EnableAi)
                return;

            base.Init(sessionComponentBuilder);

            var ob = (MyObjectBuilder_AIComponent)sessionComponentBuilder;

            foreach (var brain in ob.BotBrains)
            {
                m_loadedBotObjectBuildersByHandle[brain.PlayerHandle] = brain.BotBrain;
            }
        }

        public override void BeforeStart()
        {
            base.BeforeStart();

            if (MyPerGameSettings.EnableAi)
            {
                // We have to create bots here, because when the game is loading, they will have the correct character only after the whole scene is loaded
                foreach (int playerNumber in m_loadedLocalPlayers)
                {
                    MyObjectBuilder_Bot botBuilder = null;
                    m_loadedBotObjectBuildersByHandle.TryGetValue(playerNumber, out botBuilder);

                    CreateBot(playerNumber, botBuilder);
                }

                m_loadedLocalPlayers.Clear();
                m_loadedBotObjectBuildersByHandle.Clear();

                Sync.Players.LocalPlayerRemoved += LocalPlayerRemoved;

                if (MyPerGameSettings.Game == GameEnum.ME_GAME && Sync.IsServer)
                {
                    CleanUnusedIdentities();
                }
            }
        }

        public override void Simulate()
        {
            if (MyPerGameSettings.EnableAi)
            {
                // Pathfinding should be updated before the bots are simulated, so that the changes
                // in meshes and voxels are reflected in the new meshes if the bots want to do some pathfinding
                m_pathfinding.Update();

                ProfilerShort.Begin("MyAIComponent.Simulate()");
                base.Simulate();
                m_behaviorTreeCollection.Update();
                m_botCollection.Update();

                ProfilerShort.End();
            }
        }

        public override void UpdateAfterSimulation()
        {
            base.UpdateAfterSimulation();

            if (MyPerGameSettings.EnableAi)
            {
                PerformBotRemovals(false);

                AgentSpawnData newBotData;
                while (m_processQueue.TryDequeue(out newBotData))
                {
                    m_agentsToSpawn[newBotData.BotId] = newBotData;
                    Sync.Players.RequestNewPlayer(newBotData.BotId, MyDefinitionManager.Static.GetRandomCharacterName(), newBotData.AgentDefinition.BotModel);
                }

                ProfilerShort.Begin("Debug draw");
                m_pathfinding.DebugDraw();
                m_botCollection.DebugDraw();
                ProfilerShort.End();
            }
        }

        protected override void UnloadData()
        {
            base.UnloadData();

            if (MyPerGameSettings.EnableAi)
            {
                Sync.Players.NewPlayerRequestSucceeded -= PlayerCreated;
                Sync.Players.LocalPlayerRemoved -= LocalPlayerRemoved;
                Sync.Players.LocalPlayerLoaded -= LocalPlayerLoaded;
                Sync.Players.NewPlayerRequestFailed -= Players_NewPlayerRequestFailed;
                if (Sync.IsServer)
                {
                    Sync.Players.PlayerRequesting -= Players_PlayerRequesting;
                    Sync.Players.PlayerRemoved -= Players_PlayerRemoved;
                }

                m_pathfinding.UnloadData();
                m_botCollection.UnloadData();

                m_botCollection = null;
                m_pathfinding = null;

                if (MyFakes.ENABLE_BEHAVIOR_TREE_TOOL_COMMUNICATION)
                {
                    MyMessageLoop.RemoveMessageHandler(MyWMCodes.BEHAVIOR_GAME_UPLOAD_TREE, OnUploadNewTree);
                    MyMessageLoop.RemoveMessageHandler(MyWMCodes.BEHAVIOR_GAME_STOP_SENDING, OnBreakDebugging);
                    MyMessageLoop.RemoveMessageHandler(MyWMCodes.BEHAVIOR_GAME_RESUME_SENDING, OnResumeDebugging);
                }

                if (MyToolbarComponent.CurrentToolbar != null)
                {
                    MyToolbarComponent.CurrentToolbar.SelectedSlotChanged -= CurrentToolbar_SelectedSlotChanged;
                    MyToolbarComponent.CurrentToolbar.SlotActivated -= CurrentToolbar_SlotActivated;
                    MyToolbarComponent.CurrentToolbar.Unselected -= CurrentToolbar_Unselected;
                }
            }
        }

        public override MyObjectBuilder_SessionComponent GetObjectBuilder()
        {
            if (!MyPerGameSettings.EnableAi)
                return null;

            MyObjectBuilder_AIComponent ob = (MyObjectBuilder_AIComponent)base.GetObjectBuilder();

            ob.BotBrains = new List<MyObjectBuilder_AIComponent.BotData>();
            m_botCollection.GetBotsData(ob.BotBrains);

            return ob;
        }

        public int SpawnNewBot(MyAgentDefinition agentDefinition)
        {
            Vector3D spawnPosition = default(Vector3D);
            if (!BotFactory.GetBotSpawnPosition(agentDefinition.BehaviorType, out spawnPosition)) 
                return 0;

            return SpawnNewBotInternal(agentDefinition, spawnPosition, false);
        }

        public int SpawnNewBot(MyAgentDefinition agentDefinition, Vector3D position)
        {
            return SpawnNewBotInternal(agentDefinition, position, true);
        }

        public bool SpawnNewBotGroup(string type, List<AgentGroupData> groupData, List<int> outIds)
        {
            int totalCount = 0;
            foreach (var data in groupData)
                totalCount += data.Count;
            BotFactory.GetBotGroupSpawnPositions(type, totalCount, m_tmpSpawnPoints);
            int spawnedAmount = m_tmpSpawnPoints.Count;
            for (int i = 0, j = 0, count = 0; i < spawnedAmount; i++)
            {
                int id = SpawnNewBotInternal(groupData[j].AgentDefinition, m_tmpSpawnPoints[i]);
                if (outIds != null)
                    outIds.Add(id);
                if (groupData[j].Count == ++count)
                {
                    count = 0;
                    j++;
                }
            }
            m_tmpSpawnPoints.Clear();
            return spawnedAmount == totalCount;
        }

        private int SpawnNewBotInternal(MyAgentDefinition agentDefinition, Vector3D? spawnPosition = null, bool createdByPlayer = false)
        {
            m_lock.AcquireExclusive();
            foreach (var player in Sync.Players.GetOnlinePlayers())
            {
                if (player.Id.SteamId == Sync.MyId && player.Id.SerialId > m_lastBotId)
                {
                    m_lastBotId = player.Id.SerialId;
                }
            }
            m_lastBotId++;
            var lastBotId = m_lastBotId;
            m_lock.ReleaseExclusive();

            m_processQueue.Enqueue(new AgentSpawnData(agentDefinition, lastBotId, spawnPosition, createdByPlayer));

            return lastBotId;
        }

        public int SpawnNewBot(MyAgentDefinition agentDefinition, Vector3D? spawnPosition)
        {
            return SpawnNewBotInternal(agentDefinition, spawnPosition, true);
        }

        public bool CanSpawnMoreBots(MyPlayer.PlayerId pid)
        {
            if (!Sync.IsServer)
            {
                Debug.Assert(false, "Server only");
                return false;
            }

            if (MyFakes.ENABLE_BRAIN_SIMULATOR) return true;

            if (MyFakes.DEVELOPMENT_PRESET) return true;

            if (MySteam.UserId == pid.SteamId)
            {
                AgentSpawnData spawnData = default(AgentSpawnData);
                if (m_agentsToSpawn.TryGetValue(pid.SerialId, out spawnData))
                {
                    if (spawnData.CreatedByPlayer)
                        return Bots.GetCreatedBotCount() < BotFactory.MaximumBotPerPlayer;
                    else
                        return Bots.GetGeneratedBotCount() < BotFactory.MaximumUncontrolledBotCount;
                }
                else
                {
                    Debug.Assert(false, "Bot doesn't exist");
                    return false;
                }
            }
            else
            {
                int botCount = 0;
                var lookedPlayer = pid.SteamId;
                var players = Sync.Players.GetOnlinePlayers();

				foreach (var player in players)
				{
					if (player.Id.SteamId == lookedPlayer && player.Id.SerialId != 0)
						botCount++;
				}

				return botCount < BotFactory.MaximumBotPerPlayer;
            }
        }

        public int GetAvailableUncontrolledBotsCount()
        {
            return BotFactory.MaximumUncontrolledBotCount - Bots.GetGeneratedBotCount();
        }

        public int GetBotCount(string behaviorType)
        {
            return m_botCollection.GetCurrentBotsCount(behaviorType);
        }

        public void CleanUnusedIdentities()
        {
            List<MyPlayer.PlayerId> tmpPlayerIds = new List<MyPlayer.PlayerId>();
            foreach (var playerId in Sync.Players.GetAllPlayers()) tmpPlayerIds.Add(playerId);

            foreach (var playerId in tmpPlayerIds)
            {
                if (playerId.SteamId != Sync.MyId || playerId.SerialId == 0) continue;

                var onlinePlayer = Sync.Players.GetPlayerById(playerId);
                if (onlinePlayer == null)
                {
                    var identityId = Sync.Players.TryGetIdentityId(playerId.SteamId, playerId.SerialId);
                    if (identityId != 0)
                    {
                        Sync.Players.RemoveIdentity(identityId, playerId);
                    }
                }
            }
        }

        void PlayerCreated(int playerNumber)
        {
            if (playerNumber == 0 || MyFakes.ENABLE_BRAIN_SIMULATOR)
                return;
            CreateBot(playerNumber);
        }

        private void LocalPlayerLoaded(int playerNumber)
        {
            if (playerNumber == 0)
                return;
            if (!m_loadedLocalPlayers.Contains(playerNumber))
            {
                m_loadedLocalPlayers.Add(playerNumber);
            }
        }

        private void Players_NewPlayerRequestFailed(int serialId)
        {
            if (serialId == 0)
                return;

            if (m_agentsToSpawn.ContainsKey(serialId))
            {
                var data = m_agentsToSpawn[serialId];
                m_agentsToSpawn.Remove(serialId);
                if (data.CreatedByPlayer)
                    MyHud.Notifications.Add(m_maxBotNotification);
            }
            else
            {
                Debug.Assert(false, "Undefined bot");
            }
        }

        private void Players_PlayerRequesting(PlayerRequestArgs args)
        {
            if (args.PlayerId.SerialId == 0)
                return;

            if (!CanSpawnMoreBots(args.PlayerId))
                args.Cancel = true;
            else
                Bots.TotalBotCount++;
        }

        private void Players_PlayerRemoved(MyPlayer.PlayerId pid)
        {
            if (!Sync.IsServer)
            {
                Debug.Assert(false, "Server only");
                return;
            }

            if (pid.SerialId != 0)
                Bots.TotalBotCount--;
        }


        private void CreateBot(int playerNumber)
        {
            CreateBot(playerNumber, null);
        }

        private void CreateBot(int playerNumber, MyObjectBuilder_Bot botBuilder)
        {
            Debug.Assert(BotFactory != null, "Bot factory is not set! Cannot create a new bot!");
            if (BotFactory == null) return;

            var newPlayer = Sync.Clients.LocalClient.GetPlayer(playerNumber);
            if (newPlayer == null) return;

            var isBotSpawned = m_agentsToSpawn.ContainsKey(playerNumber);
            var isLoading = botBuilder != null;
            var createdByPlayer = false;
            MyBotDefinition botDefinition = null;
            AgentSpawnData spawnData = default(AgentSpawnData);
            if (isBotSpawned)
            {
                spawnData = m_agentsToSpawn[playerNumber];
                createdByPlayer = spawnData.CreatedByPlayer;
                botDefinition = spawnData.AgentDefinition;
                m_agentsToSpawn.Remove(playerNumber);
            }
            else
            {
                Debug.Assert(botBuilder != null && !botBuilder.BotDefId.TypeId.IsNull, "Null or invalid bot builder. Bot is not going to be created");
                if (botBuilder == null || botBuilder.BotDefId.TypeId.IsNull)
                    return;

                botDefinition = MyDefinitionManager.Static.GetBotDefinition(botBuilder.BotDefId);
                Debug.Assert(botDefinition != null, "Bot definition could not be found.");
                if (botDefinition == null)
                    return;
            }

            if ((newPlayer.Character == null || !newPlayer.Character.IsDead)
                && BotFactory.CanCreateBotOfType(botDefinition.BehaviorType, isLoading) 
                || createdByPlayer)
            {
                IMyBot bot = null;
                if (isBotSpawned)
                    bot = BotFactory.CreateBot(newPlayer, botBuilder, spawnData.AgentDefinition);                
                else
                    bot = BotFactory.CreateBot(newPlayer, botBuilder, botDefinition);

                if (bot == null)
                {
                    MyLog.Default.WriteLine("Could not create a bot for player " + newPlayer + "!");
                }
                else
                {
                    m_botCollection.AddBot(playerNumber, bot);
                    if (isBotSpawned && bot is IMyEntityBot)
                        (bot as IMyEntityBot).Spawn(spawnData.SpawnPosition, createdByPlayer);

                    if (BotCreatedEvent != null)
                    {
                        BotCreatedEvent(playerNumber, bot.BotDefinition);
                    }
                }
            }
            else
            {
                // hack for removing uncontrolled bot players or saved dead characters
                var player = Sync.Players.GetPlayerById(new MyPlayer.PlayerId(MySteam.UserId, playerNumber));
                Sync.Players.RemovePlayer(player);
            }
        }

        public void DespawnBotsOfType(string botType)
        {
            var allBots = m_botCollection.GetAllBots();
            foreach (var entry in allBots)
            {
                if (entry.Value.BotDefinition.BehaviorType == botType)
                {
                    Sync.Players.GetPlayerById(new Sandbox.Game.World.MyPlayer.PlayerId(Sync.MyId, entry.Key));
                    RemoveBot(entry.Key);
                }
            }
            PerformBotRemovals(true);
        }

        private void PerformBotRemovals(bool removeEntity)
        {
            int playerNumber;
            while (m_removeQueue.TryDequeue(out playerNumber))
            {
                MyPlayer player = Sync.Players.GetPlayerById(new MyPlayer.PlayerId(MySteam.UserId, playerNumber));
                if (player != null)
                    Sync.Players.RemovePlayer(player, removeEntity);
            }
        }

        public void RemoveBot(int playerNumber)
        {
            m_removeQueue.Enqueue(playerNumber);
        }

        void LocalPlayerRemoved(int playerNumber)
        {
            if (playerNumber == 0) return;

            m_botCollection.TryRemoveBot(playerNumber);
        }

        public override void HandleInput()
        {
            base.HandleInput();

            if (!(MyScreenManager.GetScreenWithFocus() is MyGuiScreenGamePlay))
                return;

            if (MyControllerHelper.IsControl(MySpaceBindingCreator.CX_CHARACTER, MyControlsSpace.PRIMARY_TOOL_ACTION))
            {
                if (MySession.ControlledEntity != null && BotToSpawn != null)
                    TrySpawnBot();
                if (MySession.ControlledEntity != null && CommandDefinition != null)
                    UseCommand();
            }
        }

        private void CurrentToolbar_SelectedSlotChanged(MyToolbar toolbar, MyToolbar.SlotArgs args)
        {
            if (!(toolbar.SelectedItem is MyToolbarItemBot))
                BotToSpawn = null;
            if (!(toolbar.SelectedItem is MyToolbarItemAiCommand))
                CommandDefinition = null;
        }

        private void CurrentToolbar_SlotActivated(MyToolbar toolbar, MyToolbar.SlotArgs args)
        {
            if (!(toolbar.GetItemAtIndex(toolbar.SlotToIndex(args.SlotNumber.Value)) is MyToolbarItemBot))
                BotToSpawn = null;
            if (!(toolbar.GetItemAtIndex(toolbar.SlotToIndex(args.SlotNumber.Value)) is MyToolbarItemAiCommand))
                CommandDefinition = null;
        }

        private void CurrentToolbar_Unselected(MyToolbar toolbar)
        {
            BotToSpawn = null;
            CommandDefinition = null;
        }

        private void TrySpawnBot()
        {
            Vector3D cameraPos, cameraDir;

            if (MySession.GetCameraControllerEnum() == Common.ObjectBuilders.MyCameraControllerEnum.ThirdPersonSpectator || MySession.GetCameraControllerEnum() == Common.ObjectBuilders.MyCameraControllerEnum.Entity)
            {
                var headMatrix = MySession.ControlledEntity.GetHeadMatrix(true, true);
                cameraPos = headMatrix.Translation;
                cameraDir = headMatrix.Forward;
            }
            else
            {
                cameraPos = MySector.MainCamera.Position;
                cameraDir = MySector.MainCamera.WorldMatrix.Forward;
            }

            List<MyPhysics.HitInfo> hitInfos = new List<MyPhysics.HitInfo>();

            MyPhysics.CastRay(cameraPos, cameraPos + cameraDir * 100, hitInfos, MyPhysics.ObjectDetectionCollisionLayer);
            if (hitInfos.Count == 0)
                return;

            MyPhysics.HitInfo? closestValidHit = null;
            foreach (var hitInfo in hitInfos)
            {
                var ent = hitInfo.HkHitInfo.GetHitEntity();
                if (ent is MyCubeGrid)
                {
                    closestValidHit = hitInfo;
                    break;
                }
                else if (ent is MyVoxelMap)
                {
                    closestValidHit = hitInfo;
                    break;
                }
            }

            if (closestValidHit.HasValue)
            {
                Vector3D position = closestValidHit.Value.Position;
                MyAIComponent.Static.SpawnNewBot(BotToSpawn, position);
            }
        }

        private void UseCommand()
        {
            // MW:TODO make it generic
            MyAiCommandBehavior tmpCommand = new MyAiCommandBehavior();
            tmpCommand.InitCommand(CommandDefinition);
            tmpCommand.ActivateCommand();
        }

        public static int GenerateBotId(int lastSpawnedBot)
        {
            int highestExistingPlayer = lastSpawnedBot;
            var players = Sync.Players.GetOnlinePlayers();
            foreach (var player in players) // has to be tweaked
            {
                if (player.Id.SteamId == Sync.MyId)
                    highestExistingPlayer = Math.Max(highestExistingPlayer, player.Id.SerialId);
            }
            return highestExistingPlayer + 1;
        }

        public void DebugDrawBots()
        {
            m_botCollection.DebugDrawBots();
        }

        public void DebugSelectNextBot()
        {
            m_botCollection.DebugSelectNextBot();
        }

        public void DebugSelectPreviousBot()
        {
            m_botCollection.DebugSelectPreviousBot();
        }

        public void DebugRemoveFirstBot()
        {
            if (m_botCollection.HasBot)
            {
                var player = Sync.Players.GetPlayerById(new MyPlayer.PlayerId(MySteam.UserId, m_botCollection.GetHandleToFirstBot()));
                Sync.Players.RemovePlayer(player);
            }
        }

        #region Tool message handling

        private void OnUploadNewTree(ref Message msg)
        {
            if (m_behaviorTreeCollection != null)
            {
                MyBehaviorTree behaviorTree = null;
                MyBehaviorDefinition behaviorDefinition = null;
                bool success = MyBehaviorTreeCollection.LoadUploadedBehaviorTree(out behaviorDefinition);
                if (success && m_behaviorTreeCollection.HasBehavior(behaviorDefinition.Id.SubtypeId))
                {
                    m_botCollection.ResetBots(behaviorDefinition.Id.SubtypeName);
                    m_behaviorTreeCollection.RebuildBehaviorTree(behaviorDefinition, out behaviorTree);
                    m_botCollection.CheckCompatibilityWithBots(behaviorTree);
                }
                IntPtr toolWindowHandle = IntPtr.Zero;
                if (m_behaviorTreeCollection.TryGetValidToolWindow(out toolWindowHandle))
                    WinApi.PostMessage(toolWindowHandle, MyWMCodes.BEHAVIOR_TOOL_TREE_UPLOAD_SUCCESS, IntPtr.Zero, IntPtr.Zero);
            }
        }

        private void OnBreakDebugging(ref Message msg)
        {
            if (m_behaviorTreeCollection != null)
            {
                m_behaviorTreeCollection.DebugBreakDebugging = true;
            }
        }

        private void OnResumeDebugging(ref Message msg)
        {
            if (m_behaviorTreeCollection != null)
            {
                m_behaviorTreeCollection.DebugBreakDebugging = false;
            }
        }

        #endregion
    }
}
