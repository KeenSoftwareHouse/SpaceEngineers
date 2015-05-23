using Sandbox.Common;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Common.ObjectBuilders.Definitions;
using Sandbox.Common.ObjectBuilders.AI;
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

            public AgentSpawnData(MyAgentDefinition agentDefinition, Vector3D? spawnPosition = null, bool createAlways = false)
            {
                AgentDefinition = agentDefinition;
                SpawnPosition = spawnPosition;
                CreatedByPlayer = createAlways;
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
        private Queue<int> m_removeQueue;

        private static List<MyPlaceArea> m_tmpAreas = new List<MyPlaceArea>();

        public static MyAIComponent Static;
        public static IMyBotFactory BotFactory;

        private Dictionary<int, AgentSpawnData> m_agentsToSpawn;
        private int m_lastSpawnedBot;

        private MyHudNotification m_maxBotNotification;

        public MyAgentDefinition BotToSpawn = null;
        public MyAiCommandDefinition CommandDefinition = null;
		public MyAreaMarkerDefinition AreaMarkerDefinition = null;


		

        public MyAIComponent()
        {
            Static = this;
            BotFactory = Activator.CreateInstance(MyPerGameSettings.BotFactoryType) as IMyBotFactory;
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
                m_removeQueue = new Queue<int>();
                m_lastSpawnedBot = 0;
                m_maxBotNotification = new MyHudNotification(MySpaceTexts.NotificationMaximumNumberBots, 2000, MyFontEnum.Red);

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
                if (m_removeQueue.Count > 0)
                {
                    foreach (var playerNumber in m_removeQueue)
                    {
                        MyPlayer player = Sync.Players.TryGetPlayerById(new MyPlayer.PlayerId(MySteam.UserId, playerNumber));
                        Sync.Players.RemovePlayer(player);
                    }
                    m_removeQueue.Clear();
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

        private int SpawnNewBotInternal(MyAgentDefinition agentDefinition, Vector3D? spawnPosition = null, bool createdByPlayer = false)
        {
            var currentHighestBotID = MyAIComponent.GenerateBotId(m_lastSpawnedBot);
            var newBotId = currentHighestBotID;
            EnsureIdentityUniqueness(newBotId);
            m_agentsToSpawn[newBotId] = new AgentSpawnData(agentDefinition, spawnPosition, createdByPlayer);
            m_lastSpawnedBot = newBotId;

            Sync.Players.RequestNewPlayer(newBotId, MyDefinitionManager.Static.GetRandomCharacterName(), agentDefinition.BotModel);
            return newBotId;
        }

        public int SpawnNewBot(MyAgentDefinition agentDefinition, Vector3D? spawnPosition)
        {
            return SpawnNewBotInternal(agentDefinition, spawnPosition, true);
        }

        private void EnsureIdentityUniqueness(int newBotId)
        {
            var pid = new MyPlayer.PlayerId(MySteam.UserId, newBotId);
            var identity = Sync.Players.TryGetPlayerIdentity(pid);
            var player = Sync.Players.TryGetPlayerById(pid);
            if (identity != null && identity.IdentityId != 0 && player == null)
            {
                Sync.Players.RemoveIdentity(pid); // removing old identity
            }
        }

        public bool CanSpawnMoreBots(MyPlayer.PlayerId pid)
        {
            if (!Sync.IsServer)
            {
                Debug.Assert(false, "Server only");
                return false;
            }

			int perPlayerBotMultiplier = (MySession.Static.CreativeMode ? MySession.Static.MaxPlayers : 1);

            if (MySteam.UserId == pid.SteamId)
            {
                AgentSpawnData spawnData = default(AgentSpawnData);
                if (m_agentsToSpawn.TryGetValue(pid.SerialId, out spawnData))
                {
                    if (spawnData.CreatedByPlayer)
                        return Bots.GetCreatedBotCount() < BotFactory.MaximumBotPerPlayer*perPlayerBotMultiplier;
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
				if (MySession.Static.CreativeMode)
					return Bots.GetCreatedBotCount() < BotFactory.MaximumBotPerPlayer * perPlayerBotMultiplier;

                int botCount = 0;
                var lookedPlayer = pid.SteamId;
                var players = Sync.Players.GetAllPlayers();

				if (MySession.Static.CreativeMode)
				{
					foreach (var player in players)
					{
						if (player.SerialId != 0)
							++botCount;
					}
				}
				else
				{
					foreach (var player in players)
					{
						if (player.SteamId == lookedPlayer && player.SerialId != 0)
							botCount++;
					}
				}

				return botCount < BotFactory.MaximumBotPerPlayer * perPlayerBotMultiplier;
            }
        }

        public int GetBotCount(string behaviorType)
        {
            return m_botCollection.GetCurrentBotsCount(behaviorType);
        }

        void PlayerCreated(int playerNumber)
        {
            if (playerNumber == 0)
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
                }
            }
            else
            {
                // hack for removing uncontrolled bot players or saved dead characters
                var player = Sync.Players.TryGetPlayerById(new MyPlayer.PlayerId(MySteam.UserId, playerNumber));
                Sync.Players.RemovePlayer(player);
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
				if (MySession.ControlledEntity != null && AreaMarkerDefinition != null)
					PlaceAreaMarker();
            }
        }

        private void CurrentToolbar_SelectedSlotChanged(MyToolbar toolbar, MyToolbar.SlotArgs args)
        {
            if (!(toolbar.SelectedItem is MyToolbarItemBot))
                BotToSpawn = null;
            if (!(toolbar.SelectedItem is MyToolbarItemAiCommand))
                CommandDefinition = null;
			if (!(toolbar.SelectedItem is MyToolbarItemAreaMarker))
				AreaMarkerDefinition = null;
        }

        private void CurrentToolbar_SlotActivated(MyToolbar toolbar, MyToolbar.SlotArgs args)
        {
            if (!(toolbar.GetItemAtIndex(toolbar.SlotToIndex(args.SlotNumber.Value)) is MyToolbarItemBot))
                BotToSpawn = null;
            if (!(toolbar.GetItemAtIndex(toolbar.SlotToIndex(args.SlotNumber.Value)) is MyToolbarItemAiCommand))
                CommandDefinition = null;
			if (!(toolbar.GetItemAtIndex(toolbar.SlotToIndex(args.SlotNumber.Value)) is MyToolbarItemAreaMarker))
				AreaMarkerDefinition = null;
        }

        private void CurrentToolbar_Unselected(MyToolbar toolbar)
        {
            BotToSpawn = null;
            CommandDefinition = null;
			AreaMarkerDefinition = null;
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
                var ent = hitInfo.HkHitInfo.Body.GetEntity();
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

		private void PlaceAreaMarker()
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
				var ent = hitInfo.HkHitInfo.Body.GetEntity();
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
				MyAreaMarkerDefinition definition = AreaMarkerDefinition;
				//MyDefinitionManager.Static.TryGetDefinition(new MyDefinitionId(typeof(MyObjectBuilder_AreaMarkerDefinition), "ForestingArea"), out definition);

                m_tmpAreas.Clear();
                MyPlaceAreas.GetAllAreas(m_tmpAreas);

                foreach (var area in m_tmpAreas)
                {
                    if (area.AreaType == AreaMarkerDefinition.Id.SubtypeId)
                    {
                        area.Entity.Close();
                    }
                }
                m_tmpAreas.Clear();

				Debug.Assert(definition != null, "Area marker definition cannot be null!");
				if (definition == null) return;

				var forward = Vector3D.Reject(cameraDir, Vector3D.Up);

				if (Vector3D.IsZero(forward))
					forward = Vector3D.Forward;

				var flag = new MyAreaMarker(new MyPositionAndOrientation(position, Vector3D.Normalize(forward), Vector3D.Up), definition);

				MyEntities.Add(flag);
			}
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
                var player = Sync.Players.TryGetPlayerById(new MyPlayer.PlayerId(MySteam.UserId, m_botCollection.GetHandleToFirstBot()));
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
