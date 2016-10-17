using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using Sandbox.Engine.Networking;
using Sandbox.Engine.Utils;
using Sandbox.Game.World;
using VRage.Library.Utils;
using VRageMath;
using VRageRender;
using VRage.FileSystem;
using Sandbox.Graphics.GUI;
using Sandbox.Game.Multiplayer;
using Sandbox.ModAPI;
using Sandbox.Engine.Multiplayer;
using SteamSDK;
using Sandbox.Game.Gui;
using VRage.Utils;
using Sandbox.Game.GUI;
using Sandbox.Game.Screens;
using Sandbox.Game.Localization;
using VRage;
using VRage.Game;
using VRage.Game.Components;

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
        public enum MyState
        {
            Loaded,
            JoinScreen,
            WaitingForClients,
            Running,
            Ending
        }

        private MyState m_gameState = MyState.Loaded;
        public MyState GameState
        {
            get { return m_gameState; }
            set { if (m_gameState!=value)
            {
                m_gameState = value;
                m_stateChangePlayTime = MySession.Static.ElapsedPlayTime;
            }
            }
        }
        private TimeSpan m_stateChangePlayTime;

        // Time when battle was started (server or client local time).
        private TimeSpan m_startBattleTime = TimeSpan.FromSeconds(0);

        private StringBuilder m_tmpStringBuilder = new StringBuilder();

        private MyGuiScreenScenarioWaitForPlayers m_waitingScreen;

        // Absolute server time when server starts sending preparation requests to clients.
        public DateTime ServerPreparationStartTime { get; private set; }
        // Absolute server time when server starts battle game.
        public DateTime ServerStartGameTime { get; private set; }//max value when not started yet

        // Cached time limit from lobby.
        private TimeSpan? m_battleTimeLimit;

        private bool OnlinePrivateMode { get { return MySession.Static.OnlineMode == MyOnlineModeEnum.PRIVATE; } }


        public MyScenarioSystem()
        {
            Static = this;
            ServerStartGameTime = DateTime.MaxValue;
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

            if (GameState == MyState.WaitingForClients)
            {
                m_playersReadyForBattle.Add(steamId);

                if (AllPlayersReadyForBattle())
                {
                    StartScenario();

                    foreach (var playerId in m_playersReadyForBattle)
                    {
                        if (playerId != Sync.MyId)
                            MySyncScenario.StartScenarioRequest(playerId, ServerStartGameTime.Ticks);
                    }
                }
            }
            else if (GameState == MyState.Running)
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

            if (!(MySession.Static.IsScenario || MySession.Static.Settings.ScenarioEditMode))
                return;

            if (!Sync.IsServer)
                return;

            if (MySession.Static.OnlineMode == MyOnlineModeEnum.OFFLINE && GameState < MyState.Running)
            {
                if (GameState == MyState.Loaded)
                {
                    GameState = MyState.Running;
                    ServerStartGameTime = DateTime.UtcNow;
                }
                return;
            }

            switch (GameState)
            {
                case MyState.Loaded:
                    if (MySession.Static.OnlineMode != MyOnlineModeEnum.OFFLINE && MyMultiplayer.Static == null)
                    {
                        if (MyFakes.XBOX_PREVIEW)
                        {
                            GameState = MyState.Running;
                        }
                        else
                        {
                            m_bootUpCount++;
                            if (m_bootUpCount > 100)//because MyMultiplayer.Static is initialized later than this part of game
                            {
                                //network start failure - trying to save what we can :-)
                                MyPlayerCollection.RequestLocalRespawn();
                                GameState = MyState.Running;
                            }
                        }
                        return;
                    }
                    if (MySandboxGame.IsDedicated || MySession.Static.Settings.ScenarioEditMode)
                    {
                        ServerPreparationStartTime = DateTime.UtcNow;
                        MyMultiplayer.Static.ScenarioStartTime = ServerPreparationStartTime;
                        GameState = MyState.Running;
                        if (!MySandboxGame.IsDedicated)
                            StartScenario();
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
                        m_playersReadyForBattle.Add(Sync.MyId);
                        GameState = MyState.JoinScreen;
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
                            if (playerId != Sync.MyId)
                                MySyncScenario.StartScenarioRequest(playerId, ServerStartGameTime.Ticks);
                        }
                    }
                    break;
                case MyState.Running:
                    break;
                case MyState.Ending:
                    if (EndAction != null && MySession.Static.ElapsedPlayTime - m_stateChangePlayTime > TimeSpan.FromSeconds(10))
                        EndAction();
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

            GameState = MyState.WaitingForClients;
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
            GameState = MyState.Running;
            m_startBattleTime = MySession.Static.ElapsedPlayTime;
            if (MySession.Static.LocalHumanPlayer == null || MySession.Static.LocalHumanPlayer.Character == null)
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

        //loads next mission, SP only
        //id can be workshop ID or save name (in that case official scenarios are searched first, if not found, then user's saves)
        public static void LoadNextScenario(string id)
        {
            if (MySession.Static.OnlineMode != MyOnlineModeEnum.OFFLINE)
                return;
            MyAPIGateway.Utilities.ShowNotification(MyTexts.GetString(MySpaceTexts.NotificationNextScenarioWillLoad), 10000);
            ulong workshopID;
            if (ulong.TryParse(id, out workshopID))
            {
                //scenario from steam, without the user needing to subscribe it first:
                if (!MySteam.IsOnline)
                {
                    MyGuiSandbox.AddScreen(MyGuiSandbox.CreateMessageBox(
                                messageText: MyTexts.Get(MySpaceTexts.MessageBoxTextWorkshopDownloadFailed),
                                messageCaption: MyTexts.Get(MyCommonTexts.ScreenCaptionWorkshop)));
                }
                else
                {
                    MySandboxGame.Log.WriteLine(string.Format("Querying details of file " + workshopID));

                    Action<bool, RemoteStorageGetPublishedFileDetailsResult> onGetDetailsCallResult = delegate(bool ioFailure, RemoteStorageGetPublishedFileDetailsResult data)
                    {
                        MySandboxGame.Log.WriteLine(string.Format("Obtained details: Id={4}; Result={0}; ugcHandle={1}; title='{2}'; tags='{3}'", data.Result, data.FileHandle, data.Title, data.Tags, data.PublishedFileId));
                        if (!ioFailure && data.Result == Result.OK && data.Tags.Length != 0)
                        {
#if !XB1 // XB1_NOWORKSHOP
                            m_newWorkshopMap.Title = data.Title;
                            m_newWorkshopMap.PublishedFileId = data.PublishedFileId;
                            m_newWorkshopMap.Description = data.Description;
                            m_newWorkshopMap.UGCHandle = data.FileHandle;
                            m_newWorkshopMap.SteamIDOwner = data.SteamIDOwner;
                            m_newWorkshopMap.TimeUpdated = data.TimeUpdated;
                            m_newWorkshopMap.Tags = data.Tags.Split(',');
                            Static.EndAction += EndActionLoadWorkshop;
#else // XB1
                            System.Diagnostics.Debug.Assert(false); // TODO?
#endif // XB1
                        }
                        else
                        {
                            MyGuiSandbox.AddScreen(MyGuiSandbox.CreateMessageBox(
                                    messageText: MyTexts.Get(MySpaceTexts.MessageBoxTextWorkshopDownloadFailed),
                                    messageCaption: MyTexts.Get(MyCommonTexts.ScreenCaptionWorkshop)));
                        }
                    };
                    MySteam.API.RemoteStorage.GetPublishedFileDetails(workshopID, 0, onGetDetailsCallResult);
                }

            }
            else
            {
                var contentDir = Path.Combine(MyFileSystem.ContentPath, "Missions", id);
                if (Directory.Exists(contentDir))
                {
                    m_newPath = contentDir;
                    Static.EndAction += EndActionLoadLocal;
                    return;
                }
                var saveDir = Path.Combine(MyFileSystem.SavesPath, id);
                if (Directory.Exists(saveDir))
                {
                    m_newPath = saveDir;
                    Static.EndAction += EndActionLoadLocal;
                    return;
                }
                //fail msg:
                StringBuilder error = new StringBuilder();
                error.AppendFormat(MyTexts.GetString(MySpaceTexts.MessageBoxTextScenarioNotFound), contentDir, saveDir);
                MyGuiScreenMessageBox mb = MyGuiSandbox.CreateMessageBox(messageText: error, messageCaption: MyTexts.Get(MyCommonTexts.MessageBoxCaptionError));
                MyGuiSandbox.AddScreen(mb);
            }
        }

        private event Action EndAction;//scenario ended and we are loading next mission
        private static string m_newPath;
        private static void EndActionLoadLocal()
        {
            Static.EndAction -= EndActionLoadLocal;
            Debug.Assert(m_newPath != null);
            LoadMission(m_newPath, false, MyOnlineModeEnum.OFFLINE, 1);
        }

#if !XB1 // XB1_NOWORKSHOP
        private static MySteamWorkshop.SubscribedItem m_newWorkshopMap = new MySteamWorkshop.SubscribedItem();
        private static void EndActionLoadWorkshop()
        {
            Static.EndAction -= EndActionLoadWorkshop;
            MySteamWorkshop.CreateWorldInstanceAsync(m_newWorkshopMap, MySteamWorkshop.MyWorkshopPathInfo.CreateScenarioInfo(), true, delegate(bool success, string sessionPath)
            {
                if (success)
                {
                    m_newPath = sessionPath;
                    LoadMission(sessionPath, false, MyOnlineModeEnum.OFFLINE, 1);
                }
                else
                    MyGuiSandbox.AddScreen(MyGuiSandbox.CreateMessageBox(
                                messageText: MyTexts.Get(MySpaceTexts.MessageBoxTextWorkshopDownloadFailed),
                                messageCaption: MyTexts.Get(MyCommonTexts.ScreenCaptionWorkshop)));
            });
        }
#endif // !XB1

        private struct CheckpointData
        {
            public MyObjectBuilder_Checkpoint Checkpoint;
            public string SessionPath;
            public ulong CheckpointSize;
            public bool PersistentEditMode;
        }

        private static CheckpointData? m_checkpointData;
        private static void CheckDx11AndLoad(string sessionPath, bool multiplayer, MyOnlineModeEnum onlineMode, short maxPlayers,
            MyGameModeEnum gameMode, MyObjectBuilder_Checkpoint checkpoint, ulong checkpointSizeInBytes)
        {
            bool needsDx11 = checkpoint.RequiresDX>=11;
            if (!needsDx11 || MySandboxGame.IsDirectX11)
            {
                LoadMission(sessionPath, multiplayer, onlineMode, maxPlayers, gameMode, checkpoint, checkpointSizeInBytes);
            }
            else
            {
                MyJoinGameHelper.HandleDx11Needed();
            }
        }


        public static void LoadMission(string sessionPath, bool multiplayer, MyOnlineModeEnum onlineMode, short maxPlayers,
            MyGameModeEnum gameMode = MyGameModeEnum.Survival)
        {
            MyLog.Default.WriteLine("LoadSession() - Start");
            MyLog.Default.WriteLine(sessionPath);

            ulong checkpointSizeInBytes;
            var checkpoint = MyLocalCache.LoadCheckpoint(sessionPath, out checkpointSizeInBytes);

            CheckDx11AndLoad(sessionPath, multiplayer, onlineMode, maxPlayers, gameMode, checkpoint, checkpointSizeInBytes);
            }
        public static void LoadMission(string sessionPath, bool multiplayer, MyOnlineModeEnum onlineMode, short maxPlayers,
            MyGameModeEnum gameMode, MyObjectBuilder_Checkpoint checkpoint, ulong checkpointSizeInBytes)
        {
            var persistentEditMode = checkpoint.Settings.ScenarioEditMode;

            checkpoint.Settings.OnlineMode = onlineMode;
            checkpoint.Settings.MaxPlayers = maxPlayers;
            checkpoint.Settings.Scenario = true;
            checkpoint.Settings.GameMode = gameMode;
            checkpoint.Settings.ScenarioEditMode = false;

            if (!MySession.IsCompatibleVersion(checkpoint))
            {
                MyLog.Default.WriteLine(MyTexts.Get(MyCommonTexts.DialogTextIncompatibleWorldVersion).ToString());
                MyGuiSandbox.AddScreen(MyGuiSandbox.CreateMessageBox(
                    messageCaption: MyTexts.Get(MyCommonTexts.MessageBoxCaptionError),
                    messageText: MyTexts.Get(MyCommonTexts.DialogTextIncompatibleWorldVersion),
                    buttonType: MyMessageBoxButtonsType.OK));
                MyLog.Default.WriteLine("LoadSession() - End");
                return;
            }

            if (!MySteamWorkshop.CheckLocalModsAllowed(checkpoint.Mods, checkpoint.Settings.OnlineMode == MyOnlineModeEnum.OFFLINE))
            {
                MyLog.Default.WriteLine(MyTexts.Get(MyCommonTexts.DialogTextLocalModsDisabledInMultiplayer).ToString());
                MyGuiSandbox.AddScreen(MyGuiSandbox.CreateMessageBox(
                    messageCaption: MyTexts.Get(MyCommonTexts.MessageBoxCaptionError),
                    messageText: MyTexts.Get(MyCommonTexts.DialogTextLocalModsDisabledInMultiplayer),
                    buttonType: MyMessageBoxButtonsType.OK));
                MyLog.Default.WriteLine("LoadSession() - End");
                return;
            }

            m_checkpointData = new CheckpointData()
            {
                Checkpoint = checkpoint,
                CheckpointSize = checkpointSizeInBytes,
                PersistentEditMode = persistentEditMode,
                SessionPath = sessionPath,
            };

            if (checkpoint.BriefingVideo != null && checkpoint.BriefingVideo.Length > 0 && !MyFakes.XBOX_PREVIEW)
            {
                MyGuiSandbox.AddScreen(MyGuiSandbox.CreateMessageBox(
                    messageCaption: MyTexts.Get(MySpaceTexts.MessageBoxCaptionVideo),
                    messageText: MyTexts.Get(MySpaceTexts.MessageBoxTextWatchVideo),
                    buttonType: MyMessageBoxButtonsType.YES_NO,
                    callback: OnVideoMessageBox));
            }
            else
            {
                var checkpointData = m_checkpointData.Value;
                m_checkpointData = null;
                LoadMission(checkpointData);
            }
        }

        private static void OnVideoMessageBox(MyGuiScreenMessageBox.ResultEnum result)
        {
            if (result == MyGuiScreenMessageBox.ResultEnum.YES)
                MyGuiSandbox.OpenUrlWithFallback(m_checkpointData.Value.Checkpoint.BriefingVideo, "Scenario briefing video", true);

            var checkpointData = m_checkpointData.Value;
            m_checkpointData = null;
            LoadMission(checkpointData);
        }


        private static void LoadMission(CheckpointData data)
        {
            var checkpoint = data.Checkpoint;
            MySteamWorkshop.DownloadModsAsync(checkpoint.Mods, delegate(bool success,string mismatchMods)
            {
                if (success || (checkpoint.Settings.OnlineMode == MyOnlineModeEnum.OFFLINE) && MySteamWorkshop.CanRunOffline(checkpoint.Mods))
                {
                    //Sandbox.Audio.MyAudio.Static.Mute = true;

                    MyScreenManager.CloseAllScreensNowExcept(null);
                    MyGuiSandbox.Update(VRage.Game.MyEngineConstants.UPDATE_STEP_SIZE_IN_MILLISECONDS);

                    MySessionLoader.CheckMismatchmods(mismatchMods, callback: delegate(VRage.Game.ModAPI.ResultEnum val)
                    {
                        // May be called from gameplay, so we must make sure we unload the current game
                        if (MySession.Static != null)
                        {
                            MySession.Static.Unload();
                            MySession.Static = null;
                        }

                        //seed 0 has special meaning - please randomize at mission start. New seed will be saved and game will run with it ever since.
                        //  if you use this, YOU CANNOT HAVE ANY PROCEDURAL ASTEROIDS ALREADY SAVED
                        if (checkpoint.Settings.ProceduralSeed == 0)
                            checkpoint.Settings.ProceduralSeed = MyRandom.Instance.Next();

                        MySessionLoader.StartLoading(delegate
                        {
                            checkpoint.Settings.Scenario = true;
                            MySession.LoadMission(data.SessionPath, checkpoint, data.CheckpointSize, data.PersistentEditMode);
                        });
                    });
                }
                else
                {
                    MyLog.Default.WriteLine(MyTexts.Get(MyCommonTexts.DialogTextDownloadModsFailed).ToString());
                    MyGuiSandbox.AddScreen(MyGuiSandbox.CreateMessageBox(
                        messageCaption: MyTexts.Get(MyCommonTexts.MessageBoxCaptionError),
                        messageText: MyTexts.Get(MyCommonTexts.DialogTextDownloadModsFailed),
                        buttonType: MyMessageBoxButtonsType.OK, callback: delegate(MyGuiScreenMessageBox.ResultEnum result)
                        {
                            if (MyFakes.QUICK_LAUNCH != null)
                                MySessionLoader.UnloadAndExitToMenu();
                        }));
                }
                MyLog.Default.WriteLine("LoadSession() - End");
            });
        }

    }
}
