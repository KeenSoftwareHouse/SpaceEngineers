using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using Medieval.Entities;
using Medieval.ObjectBuilders;
using Sandbox.Common;
using Sandbox.Common.ObjectBuilders.Voxels;
using Sandbox.Engine.Networking;
using Sandbox.Engine.Utils;
using Sandbox.Engine.Voxels;
using Sandbox.Game.Components;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.World;
using Sandbox.Graphics;
using VRage.Input;
using VRage.Library.Utils;
using VRage.Voxels;
using VRageMath;
using VRageRender;
using VRage.FileSystem;
using Sandbox.Graphics.GUI;
using Sandbox.Game.Entities.Character;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Game.Multiplayer;
using Sandbox.ModAPI;
using Sandbox.Engine.Multiplayer;
using SteamSDK;
using ProtoBuf;
using Sandbox.Game.Gui;
using VRage.Utils;
using Sandbox.Definitions;
using Sandbox.Game.Screens.Helpers;
using Sandbox.Engine.Physics;
using Sandbox.Common.ModAPI;
using Sandbox.Game.GUI;
using Sandbox.Game.Screens;

namespace Sandbox.Game.GameSystems
{
    [MySessionComponentDescriptor(MyUpdateOrder.BeforeSimulation, 1000)]
    public class MyScenarioSystem : MySessionComponentBase
    {
        public static int LoadTimeout = 120;
        public static MyScenarioSystem Static;

        private readonly HashSet<ulong> m_playersReadyForBattle = new HashSet<ulong>();

        private TimeSpan m_startBattlePreparationOnClients = TimeSpan.FromSeconds(0);

        //#### for server and clients
        private enum MyState
        {
            Loaded,
            JoinScreen,
            WaitingForClients,
            Running,
        }

        private MyState m_gameState = MyState.Loaded;


        // Time when battle was started (server or client local time).
        private TimeSpan m_startBattleTime = TimeSpan.FromSeconds(0);

        private StringBuilder m_tmpStringBuilder = new StringBuilder();

        private MyGuiScreenScenarioWaitForPlayers m_waitingScreen;

        // Absolute server time when server starts sending preparation requests to clients.
        public DateTime ServerPreparationStartTime { get; private set; }
        // Absolute server time when server starts battle game.
        public DateTime ServerStartGameTime { get; private set; }

        // Cached time limit from lobby.
        private TimeSpan? m_battleTimeLimit;

        private bool OnlinePrivateMode { get { return MySession.Static.OnlineMode == MyOnlineModeEnum.PRIVATE; } }


        public MyScenarioSystem()
        {
            Static = this;
        }

        void MySyncScenario_ClientWorldLoaded()
        {
            MySyncScenario.ClientWorldLoaded -= MySyncScenario_ClientWorldLoaded;

            m_waitingScreen = new MyGuiScreenScenarioWaitForPlayers();
            MyGuiSandbox.AddScreen(m_waitingScreen);
        }

        void MySyncScenario_StartScenario(long serverStartGameTime)
        {
            Debug.Assert(!Sync.IsServer);

            ServerStartGameTime = new DateTime(serverStartGameTime);

            StartScenario();
        }

        void MySyncScenario_PlayerReadyToStart(ulong steamId)
        {
            Debug.Assert(Sync.IsServer);

            if (m_gameState == MyState.WaitingForClients)
            {
                m_playersReadyForBattle.Add(steamId);

                if (AllPlayersReadyForBattle())
                {
                    StartScenario();

                    foreach (var playerId in m_playersReadyForBattle)
                    {
                        if (playerId != MySteam.UserId)
                            MySyncScenario.StartScenarioRequest(playerId, ServerStartGameTime.Ticks);
                    }
                }
            }
            else if (m_gameState == MyState.Running)
            {
                MySyncScenario.StartScenarioRequest(steamId, ServerStartGameTime.Ticks);
            }
        }

        private bool AllPlayersReadyForBattle()
        {
            foreach (var player in Sync.Players.GetAllPlayers())
            {
                if (!m_playersReadyForBattle.Contains(player.SteamId))
                    return false;
            }
            return true;
        }
        int m_bootUpCount = 0;
        public override void UpdateBeforeSimulation()
        {
            base.UpdateBeforeSimulation();

            if (!MySession.Static.IsScenario)
                return;

            if (!Sync.IsServer)
                return;

            if (MySession.Static.OnlineMode == MyOnlineModeEnum.OFFLINE)//!Sync.MultiplayerActive)
                if (m_gameState == MyState.Loaded)
                {
                    m_gameState = MyState.Running;
                    ServerStartGameTime = DateTime.UtcNow;
                }
                return;

            switch (m_gameState)
            {
                case MyState.Loaded:
                    if (MySession.Static.OnlineMode != MyOnlineModeEnum.OFFLINE && MyMultiplayer.Static == null)
                    {
                        m_bootUpCount++;
                        if (m_bootUpCount > 10)//because MyMultiplayer.Static is initialized later than this part of game
                        {
                            //network start failure - trying to save what we can :-)
                            MyPlayerCollection.RequestLocalRespawn();
                            m_gameState = MyState.Running;
                            return;
                        }
                    }
                    if (MySandboxGame.IsDedicated)
                    {
                        ServerPreparationStartTime = DateTime.UtcNow;
                        MyMultiplayer.Static.ScenarioStartTime = ServerPreparationStartTime;
                        m_gameState = MyState.Running;
                        return;
                    }
                    if (MySession.Static.OnlineMode == MyOnlineModeEnum.OFFLINE || MyMultiplayer.Static != null)
                    {
                        if (MyMultiplayer.Static != null)
                        {
                            MyMultiplayer.Static.Scenario = true;
                            MyMultiplayer.Static.ScenarioBriefing = MySession.Static.GetWorld().Checkpoint.Briefing;
                        }
                        MyGuiScreenScenarioMpServer guiscreen = new MyGuiScreenScenarioMpServer();
                        guiscreen.Briefing = MySession.Static.GetWorld().Checkpoint.Briefing;
                        MyGuiSandbox.AddScreen(guiscreen);
                        m_playersReadyForBattle.Add(MySteam.UserId);
                        m_gameState = MyState.JoinScreen;
                    }
                    break;
                case MyState.JoinScreen:
                    break;
                case MyState.WaitingForClients:
                    // Check timeout
                    TimeSpan currenTime = MySession.Static.ElapsedPlayTime;
                    if (AllPlayersReadyForBattle() || (LoadTimeout>0 && currenTime - m_startBattlePreparationOnClients > TimeSpan.FromSeconds(LoadTimeout)))
                    {
                        StartScenario();
                        foreach (var playerId in m_playersReadyForBattle)
                        {
                            if (playerId != MySteam.UserId)
                                MySyncScenario.StartScenarioRequest(playerId, ServerStartGameTime.Ticks);
                        }
                    }
                    break;
                case MyState.Running:
                    break;
            }
        }

        public override void LoadData()
        {
            base.LoadData();

            MySyncScenario.PlayerReadyToStartScenario += MySyncScenario_PlayerReadyToStart;
            MySyncScenario.StartScenario += MySyncScenario_StartScenario;
            MySyncScenario.ClientWorldLoaded += MySyncScenario_ClientWorldLoaded;
            MySyncScenario.PrepareScenario += MySyncBattleGame_PrepareScenario;
        }

        void MySyncBattleGame_PrepareScenario(long preparationStartTime)
        {
            Debug.Assert(!Sync.IsServer);

            ServerPreparationStartTime = new DateTime(preparationStartTime);
        }

        protected override void UnloadData()
        {
            base.UnloadData();

            MySyncScenario.PlayerReadyToStartScenario -= MySyncScenario_PlayerReadyToStart;
            MySyncScenario.StartScenario -= MySyncScenario_StartScenario;
            MySyncScenario.ClientWorldLoaded -= MySyncScenario_ClientWorldLoaded;
            MySyncScenario.PrepareScenario -= MySyncBattleGame_PrepareScenario;

            /*if (Sync.IsServer && MySession.Static.Battle)
            {
                Sync.Players.PlayerCharacterDied -= Players_PlayerCharacterDied;
                MySession.Static.Factions.FactionCreated -= Factions_FactionCreated;
            }*/
        }

        internal void PrepareForStart()
        {
            Debug.Assert(Sync.IsServer);

            m_gameState = MyState.WaitingForClients;
            m_startBattlePreparationOnClients = MySession.Static.ElapsedPlayTime;

            var onlineMode = GetOnlineModeFromCurrentLobbyType();
            if (onlineMode != MyOnlineModeEnum.OFFLINE)
            {
                m_waitingScreen = new MyGuiScreenScenarioWaitForPlayers();
                MyGuiSandbox.AddScreen(m_waitingScreen);

                ServerPreparationStartTime = DateTime.UtcNow;
                MyMultiplayer.Static.ScenarioStartTime = ServerPreparationStartTime;
                MySyncScenario.PrepareScenarioFromLobby(ServerPreparationStartTime.Ticks);
            }
            else
            {
                StartScenario();
            }
        }

        private void StartScenario()
        {
            if (Sync.IsServer)
            {
                ServerStartGameTime = DateTime.UtcNow;
            }
            if (m_waitingScreen != null)
            {
                MyGuiSandbox.RemoveScreen(m_waitingScreen);
                m_waitingScreen = null;
            }
            m_gameState = MyState.Running;
            m_startBattleTime = MySession.Static.ElapsedPlayTime;
            MyPlayerCollection.RequestLocalRespawn();
        }

        internal static MyOnlineModeEnum GetOnlineModeFromCurrentLobbyType()
        {
            MyMultiplayerLobby lobby = MyMultiplayer.Static as MyMultiplayerLobby;
            if (lobby == null)
            {
                Debug.Fail("Multiplayer lobby not found");
                return MyOnlineModeEnum.PRIVATE;
            }

            switch (lobby.GetLobbyType())
            {
                case LobbyTypeEnum.Private:
                    return MyOnlineModeEnum.PRIVATE;
                case LobbyTypeEnum.FriendsOnly:
                    return MyOnlineModeEnum.FRIENDS;
                case LobbyTypeEnum.Public:
                    return MyOnlineModeEnum.PUBLIC;
            }

            return MyOnlineModeEnum.PRIVATE;
        }

        internal static void SetLobbyTypeFromOnlineMode(MyOnlineModeEnum onlineMode)
        {
            MyMultiplayerLobby lobby = MyMultiplayer.Static as MyMultiplayerLobby;
            if (lobby == null)
            {
                Debug.Fail("Multiplayer lobby not found");
                return;
            }

            LobbyTypeEnum lobbyType = LobbyTypeEnum.Private;

            switch (onlineMode)
            {
                case MyOnlineModeEnum.FRIENDS:
                    lobbyType = LobbyTypeEnum.FriendsOnly;
                    break;
                case MyOnlineModeEnum.PUBLIC:
                    lobbyType = LobbyTypeEnum.Public;
                    break;
            }

            lobby.SetLobbyType(lobbyType);
        }

    }
}
