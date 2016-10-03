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
using VRage.Game;
using VRage.Game.Components;
using VRage.Profiler;
using Havok;

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
        private IMyPathfinding m_pathfinding;
        private MyBehaviorTreeCollection m_behaviorTreeCollection;

        public MyBotCollection Bots { get { return m_botCollection; } }
        public IMyPathfinding Pathfinding { get { return m_pathfinding; } }
        public MyBehaviorTreeCollection BehaviorTrees { get { return m_behaviorTreeCollection; } }

        private Dictionary<int, MyObjectBuilder_Bot> m_loadedBotObjectBuildersByHandle;
        private List<int> m_loadedLocalPlayers;
        private List<Vector3D> m_tmpSpawnPoints = new List<Vector3D>();

        public static MyAIComponent Static;
        public static MyBotFactoryBase BotFactory;

        private int m_lastBotId = 0;
        private Dictionary<int, AgentSpawnData> m_agentsToSpawn;

        private MyHudNotification m_maxBotNotification;
        private bool m_debugDrawPathfinding = false;


        public MyAgentDefinition BotToSpawn = null;
        public MyAiCommandDefinition CommandDefinition = null;

        public event Action<int, MyBotDefinition> BotCreatedEvent;

        private struct BotRemovalRequest
        {
            public int SerialId;
            public bool RemoveCharacter;
        }

        private MyConcurrentQueue<BotRemovalRequest> m_removeQueue;
        private MyConcurrentQueue<AgentSpawnData> m_processQueue;
        private FastResourceLock m_lock;

        public MyAIComponent()
        {
            Static = this;
            BotFactory = Activator.CreateInstance(MyPerGameSettings.BotFactoryType) as MyBotFactoryBase;
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

                if (MyPerGameSettings.PathfindingType != null)
                {
                    m_pathfinding = Activator.CreateInstance(MyPerGameSettings.PathfindingType) as IMyPathfinding;
                }
                m_behaviorTreeCollection = new MyBehaviorTreeCollection();
                m_botCollection = new MyBotCollection(m_behaviorTreeCollection);
                m_loadedLocalPlayers = new List<int>();
                m_loadedBotObjectBuildersByHandle = new Dictionary<int, MyObjectBuilder_Bot>();
                m_agentsToSpawn = new Dictionary<int, AgentSpawnData>();
                m_removeQueue = new MyConcurrentQueue<BotRemovalRequest>();
                m_maxBotNotification = new MyHudNotification(MyCommonTexts.NotificationMaximumNumberBots, 2000, MyFontEnum.Red);
                m_processQueue = new MyConcurrentQueue<AgentSpawnData>();
                m_lock = new FastResourceLock();

#if !XB1
                if (MyFakes.ENABLE_BEHAVIOR_TREE_TOOL_COMMUNICATION)
                {
                    MyMessageLoop.AddMessageHandler(MyWMCodes.BEHAVIOR_GAME_UPLOAD_TREE, OnUploadNewTree);
                    MyMessageLoop.AddMessageHandler(MyWMCodes.BEHAVIOR_GAME_STOP_SENDING, OnBreakDebugging);
                    MyMessageLoop.AddMessageHandler(MyWMCodes.BEHAVIOR_GAME_RESUME_SENDING, OnResumeDebugging);
                }
#endif

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

            if (ob.BotBrains != null)
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

                    Debug.Assert(botBuilder == null || botBuilder.TypeId == botBuilder.BotDefId.TypeId, "Bot types don't match! Are you loading an old save?");
                    if ((botBuilder == null || botBuilder.TypeId == botBuilder.BotDefId.TypeId))
                    {
                        CreateBot(playerNumber, botBuilder);
                    }
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
                if (MyFakes.DEBUG_ONE_VOXEL_PATHFINDING_STEP_SETTING)
                {
                    if (!MyFakes.DEBUG_ONE_VOXEL_PATHFINDING_STEP)
                        // voxel pathfinding step isn't allowed - it disables also other AI stuff
                        return;
                }
                else
                {
                    if (MyFakes.DEBUG_ONE_AI_STEP_SETTING)
                        if (!MyFakes.DEBUG_ONE_AI_STEP)
                            // AI step isn't allowed
                            return;
                        else
                            // disable next AI step - and do one
                            MyFakes.DEBUG_ONE_AI_STEP = false;
                }

                MySimpleProfiler.Begin("AI");
                if (m_pathfinding != null)
					m_pathfinding.Update();
				ProfilerShort.Begin("MyAIComponent.Simulate()");
                base.Simulate();
                m_behaviorTreeCollection.Update();
                m_botCollection.Update();

                ProfilerShort.End();
                MySimpleProfiler.End("AI");
            }
        }

        public void PathfindingSetDrawDebug(bool drawDebug)
        {
            m_debugDrawPathfinding = drawDebug;
        }

        public void PathfindingSetDrawNavmesh(bool drawNavmesh)
        {
            MyRDPathfinding pf = m_pathfinding as MyRDPathfinding;
            if (pf != null)
                pf.SetDrawNavmesh(drawNavmesh);
        }

        public Vector3D? DebugTarget
        { get; private set; }


        public void GenerateNavmeshTile(Vector3D? target)
        {
            if (target.HasValue)
            {
                Vector3D point = target.Value + 0.1f;
                MyDestinationSphere destSphere = new MyDestinationSphere(ref point, 1);

                var path = Static.Pathfinding.FindPathGlobal(target.Value - 0.1f, destSphere, null);
                Vector3D nextPosition;
                float targetRadius;
                VRage.ModAPI.IMyEntity entity;
                path.GetNextTarget(target.Value, out nextPosition, out targetRadius, out entity);
            }

            DebugTarget = target;
        }

        public void InvalidateNavmeshPosition(Vector3D? target)
        {
            if (target.HasValue)
            {
                var pathfinding = (MyRDPathfinding)Static.Pathfinding;
                if(pathfinding != null)
                {
                    BoundingBoxD box = new BoundingBoxD(target.Value - 0.1, target.Value + 0.1);
                    pathfinding.InvalidateArea(box);
                }
            }

            DebugTarget = target;
        }
        

        BoundingBoxD m_debugTargetAABB;
        public void SetPathfindingDebugTarget(Vector3D? target)
        {
            MyExternalPathfinding pf = m_pathfinding as MyExternalPathfinding;
            if (pf != null)
                pf.SetTarget(target);
            else
            {
                if (target.HasValue)
                {
                    //TODO: Just for debug purpose... Anything can be implemented

                    m_debugTargetAABB = new MyOrientedBoundingBoxD(target.Value, new Vector3D(5, 5, 5), Quaternion.Identity).GetAABB();
                    List<VRage.Game.Entity.MyEntity> entities = new List<VRage.Game.Entity.MyEntity>();
                    MyGamePruningStructure.GetAllEntitiesInBox(ref m_debugTargetAABB, entities);
                }
            }

            DebugTarget = target;
        }


        private void DrawDebugTarget()
        {
            if (DebugTarget != null)
            {
                VRageRender.MyRenderProxy.DebugDrawSphere(DebugTarget.Value, 0.2f, Color.Red, 0, false);
                VRageRender.MyRenderProxy.DebugDrawAABB(m_debugTargetAABB, Color.Green);
            }

        }

        public override void UpdateAfterSimulation()
        {
            base.UpdateAfterSimulation();

            if (MyPerGameSettings.EnableAi)
            {
                PerformBotRemovals();

                AgentSpawnData newBotData;
                while (m_processQueue.TryDequeue(out newBotData))
                {
                    m_agentsToSpawn[newBotData.BotId] = newBotData;
                    Sync.Players.RequestNewPlayer(newBotData.BotId, MyDefinitionManager.Static.GetRandomCharacterName(), newBotData.AgentDefinition.BotModel);
                }

                ProfilerShort.Begin("Debug draw");
                if (m_debugDrawPathfinding && m_pathfinding != null) 
                    m_pathfinding.DebugDraw();
      
                m_botCollection.DebugDraw();
                DebugDrawBots();
                DrawDebugTarget();
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

                if (m_pathfinding != null) 
					m_pathfinding.UnloadData();
                m_botCollection.UnloadData();

                m_botCollection = null;
                m_pathfinding = null;

#if !XB1
                if (MyFakes.ENABLE_BEHAVIOR_TREE_TOOL_COMMUNICATION)
                {
                    MyMessageLoop.RemoveMessageHandler(MyWMCodes.BEHAVIOR_GAME_UPLOAD_TREE, OnUploadNewTree);
                    MyMessageLoop.RemoveMessageHandler(MyWMCodes.BEHAVIOR_GAME_STOP_SENDING, OnBreakDebugging);
                    MyMessageLoop.RemoveMessageHandler(MyWMCodes.BEHAVIOR_GAME_RESUME_SENDING, OnResumeDebugging);
                }
#endif

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

        public int SpawnNewBot(MyAgentDefinition agentDefinition, Vector3D position, bool createdByPlayer = true)
        {
            return SpawnNewBotInternal(agentDefinition, position, createdByPlayer);
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

            if (MyFakes.DEVELOPMENT_PRESET) return true;

            if (Sync.MyId == pid.SteamId)
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

            // We have to get the bot object builder and bot definition somehow
            // Either, the bot is being spawned on this computer and the definition was saved in the spawn data
            // or the bot is just being created from the object builder (MP bot creation, etc.), so the definition is there
            if (isBotSpawned)
            {
                spawnData = m_agentsToSpawn[playerNumber];
                createdByPlayer = spawnData.CreatedByPlayer;
                botDefinition = spawnData.AgentDefinition;
                m_agentsToSpawn.Remove(playerNumber);
            }
            else
            {
                if (botBuilder == null || botBuilder.BotDefId.TypeId.IsNull)
                {
                    MyPlayer missingBotPlayer = null;
                    if (Sync.Players.TryGetPlayerById(new MyPlayer.PlayerId(Sync.MyId, playerNumber), out missingBotPlayer))
                    {
                        Sync.Players.RemovePlayer(missingBotPlayer);
                    }
                    return;
                }

                MyDefinitionManager.Static.TryGetBotDefinition(botBuilder.BotDefId, out botDefinition);
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
                var player = Sync.Players.GetPlayerById(new MyPlayer.PlayerId(Sync.MyId, playerNumber));
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
                    RemoveBot(entry.Key, removeCharacter: true);
                }
            }
            PerformBotRemovals();
        }

        private void PerformBotRemovals()
        {
            BotRemovalRequest request;
            while (m_removeQueue.TryDequeue(out request))
            {
                MyPlayer player = Sync.Players.GetPlayerById(new MyPlayer.PlayerId(Sync.MyId, request.SerialId));
                if (player != null)
                    Sync.Players.RemovePlayer(player, request.RemoveCharacter);
            }
        }

        public void RemoveBot(int playerNumber, bool removeCharacter = false)
        {
            var request = new BotRemovalRequest();
            request.SerialId = playerNumber;
            request.RemoveCharacter = removeCharacter;

            m_removeQueue.Enqueue(request);
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
                if (MySession.Static.ControlledEntity != null && BotToSpawn != null)
                    TrySpawnBot();
                if (MySession.Static.ControlledEntity != null && CommandDefinition != null)
                    UseCommand();
            }
        }

        public void TrySpawnBot(MyAgentDefinition agentDefinition)
        {
            BotToSpawn = agentDefinition;
            TrySpawnBot();
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

            if (MySession.Static.GetCameraControllerEnum() == MyCameraControllerEnum.ThirdPersonSpectator || MySession.Static.GetCameraControllerEnum() == MyCameraControllerEnum.Entity)
            {
                var headMatrix = MySession.Static.ControlledEntity.GetHeadMatrix(true, true);
                cameraPos = headMatrix.Translation;
                cameraDir = headMatrix.Forward;
            }
            else
            {
                cameraPos = MySector.MainCamera.Position;
                cameraDir = MySector.MainCamera.WorldMatrix.Forward;
            }


            List<MyPhysics.HitInfo> hitInfos = new List<MyPhysics.HitInfo>();

            var line = new LineD(MySector.MainCamera.Position, MySector.MainCamera.Position + MySector.MainCamera.ForwardVector * 1000);

            MyPhysics.CastRay(line.From, line.To, hitInfos, MyPhysics.CollisionLayers.DefaultCollisionLayer);

            //MyPhysics.CastRay(cameraPos, cameraPos + cameraDir * 1000, hitInfos, MyPhysics.CollisionLayers.ObjectDetectionCollisionLayer);
            if (hitInfos.Count == 0)
            //return;
            {
                MyAIComponent.Static.SpawnNewBot(BotToSpawn, cameraPos);
                return;
            }

            MyPhysics.HitInfo? closestValidHit = null;
            foreach (var hitInfo in hitInfos)
            {
                var ent = hitInfo.HkHitInfo.GetHitEntity();
                if (ent is MyCubeGrid)
                {
                    closestValidHit = hitInfo;
                    break;
                }
                else if (ent is MyVoxelBase)
                {
                    closestValidHit = hitInfo;
                    break;
                }
                else if (ent is MyVoxelPhysics)
                {
                    closestValidHit = hitInfo;
                    break;
                }
            }

            /*
            if (closestValidHit.HasValue)
            {
                Vector3D position = closestValidHit.Value.Position;
                MyAIComponent.Static.SpawnNewBot(BotToSpawn, position);
            }
             */
            Vector3D position;
            if (closestValidHit.HasValue)
                position = closestValidHit.Value.Position;
            else
                position = MySector.MainCamera.Position;

            MyAIComponent.Static.SpawnNewBot(BotToSpawn, position);
            
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

        public static int GenerateBotId()
        {
            int current = Static.m_lastBotId;
            Static.m_lastBotId = GenerateBotId(current);
            return Static.m_lastBotId;
        }

        public void DebugDrawBots()
        {
            if (MyDebugDrawSettings.ENABLE_DEBUG_DRAW)
            {
                m_botCollection.DebugDrawBots();
            }
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
                var player = Sync.Players.GetPlayerById(new MyPlayer.PlayerId(Sync.MyId, m_botCollection.GetHandleToFirstBot()));
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
#if !XB1
                if (m_behaviorTreeCollection.TryGetValidToolWindow(out toolWindowHandle))
                    WinApi.PostMessage(toolWindowHandle, MyWMCodes.BEHAVIOR_TOOL_TREE_UPLOAD_SUCCESS, IntPtr.Zero, IntPtr.Zero);
#endif // !XB1
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
