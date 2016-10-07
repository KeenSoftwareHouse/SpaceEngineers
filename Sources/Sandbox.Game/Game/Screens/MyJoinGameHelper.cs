﻿#region Using

using Sandbox.Common;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Engine.Multiplayer;
using Sandbox.Engine.Networking;
using Sandbox.Engine.Utils;
using Sandbox.Game.Localization;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.Screens.Helpers;
using Sandbox.Game.World;
using Sandbox.Graphics.GUI;
using SteamSDK;
using System;
using System.Diagnostics;
using System.Text;
using VRage;
using VRage.Game;
using VRage.Utils;

#endregion

namespace Sandbox.Game.Gui
{
    public static class MyJoinGameHelper
    {
        #region Join

        private static bool JoinGameTest(Lobby lobby)
        {
            if (!lobby.IsValid)
                return false;

            if (lobby.GetLobbyType() == LobbyTypeEnum.FriendsOnly && !MySteam.API.Friends.HasFriend(lobby.GetOwner()))
            {
                MyGuiSandbox.Show(MyCommonTexts.OnlyFriendsCanJoinThisGame);
                return false;
            }
            if (!MyMultiplayerLobby.IsLobbyCorrectVersion(lobby))
            {
                var formatString = MyTexts.GetString(MyCommonTexts.MultiplayerError_IncorrectVersion);
                var myVersion = MyBuildNumbers.ConvertBuildNumberFromIntToString(MyFinalBuildConstants.APP_VERSION);
                var serverVersion = MyBuildNumbers.ConvertBuildNumberFromIntToString(MyMultiplayerLobby.GetLobbyAppVersion(lobby));
                MyGuiSandbox.Show(new StringBuilder(String.Format(formatString, myVersion, serverVersion)));
                return false;
            }
            if (MyFakes.ENABLE_MP_DATA_HASHES && !MyMultiplayerLobby.HasSameData(lobby))
            {
                MyGuiSandbox.Show(MyCommonTexts.MultiplayerError_DifferentData);
                MySandboxGame.Log.WriteLine("Different game data when connecting to server. Local hash: " + MyDataIntegrityChecker.GetHashBase64() + ", server hash: " + MyMultiplayerLobby.GetDataHash(lobby));
                return false;
            }
            return true;
        }

        public static void JoinGame(Lobby lobby, bool requestData = true)
        {
            // Data not received
            if (requestData && String.IsNullOrEmpty(lobby.GetLobbyData(MyMultiplayer.AppVersionTag)))
            {
                var helper = new MyLobbyHelper(lobby);
                helper.OnSuccess += (l) => JoinGame(l, false);
                if (helper.RequestData())
                    return;
            }

            if (!JoinGameTest(lobby))
                return;

            if (MyMultiplayerLobby.GetLobbyScenario(lobby))
            {
                MyJoinGameHelper.JoinScenarioGame(lobby.LobbyId);
            }
            else if (MyFakes.ENABLE_BATTLE_SYSTEM && MyMultiplayerLobby.GetLobbyBattle(lobby))
            {
                bool canBeJoined = MyMultiplayerLobby.GetLobbyBattleCanBeJoined(lobby);
                // Check also valid faction ids in battle lobby.
                long faction1Id = MyMultiplayerLobby.GetLobbyBattleFaction1Id(lobby);
                long faction2Id = MyMultiplayerLobby.GetLobbyBattleFaction2Id(lobby);

                if (canBeJoined && faction1Id != 0 && faction2Id != 0)
                    MyJoinGameHelper.JoinBattleGame(lobby.LobbyId);
            }
            else
            {
                JoinGame(lobby.LobbyId);
            }
        }

        public static void JoinGame(GameServerItem server)
        {
            MyAnalyticsHelper.SetEntry(MyGameEntryEnum.Join);
            if (server.ServerVersion != MyFinalBuildConstants.APP_VERSION)
            {
                var sb = new StringBuilder();
                sb.AppendFormat(MyTexts.GetString(MyCommonTexts.MultiplayerError_IncorrectVersion), MyFinalBuildConstants.APP_VERSION, server.ServerVersion);
                MyGuiSandbox.Show(sb, MyCommonTexts.MessageBoxCaptionError);
                return;
            }
            if (MyFakes.ENABLE_MP_DATA_HASHES)
            {
                var serverHash = server.GetGameTagByPrefix("datahash");
                if (serverHash != "" && serverHash != MyDataIntegrityChecker.GetHashBase64())
                {
                    MyGuiSandbox.Show(MyCommonTexts.MultiplayerError_DifferentData);
                    MySandboxGame.Log.WriteLine("Different game data when connecting to server. Local hash: " + MyDataIntegrityChecker.GetHashBase64() + ", server hash: " + serverHash);
                    return;
                }
            }

            UInt32 unixTimestamp = (UInt32)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
            SteamAPI.Instance.AddFavoriteGame(server.AppID, System.Net.IPAddressExtensions.ToIPv4NetworkOrder(server.NetAdr.Address), (UInt16)server.NetAdr.Port, (UInt16)server.NetAdr.Port, FavoriteEnum.History, unixTimestamp);

            MyMultiplayerClient multiplayer = new MyMultiplayerClient(server, new MySyncLayer(new MyTransportLayer(MyMultiplayer.GameEventChannel)));
            MyMultiplayer.Static = multiplayer;
            MyMultiplayer.Static.SyncLayer.AutoRegisterGameEvents = false;
            MyMultiplayer.Static.SyncLayer.RegisterGameEvents();

            multiplayer.SendPlayerData(MySteam.UserName);

            string gamemode = server.GetGameTagByPrefix("gamemode");
            if (MyFakes.ENABLE_BATTLE_SYSTEM && gamemode == "B")
            {
                StringBuilder text = MyTexts.Get(MySpaceTexts.DialogTextJoiningBattle);

                MyGuiScreenProgress progress = new MyGuiScreenProgress(text, MyCommonTexts.Cancel);
                MyGuiSandbox.AddScreen(progress);
                progress.ProgressCancelled += () =>
                {
                    multiplayer.Dispose();
                    MyGuiScreenMainMenu.ReturnToMainMenu();
                };

                multiplayer.OnJoin += delegate
                {
                    MyJoinGameHelper.OnJoinBattle(progress, SteamSDK.Result.OK, new LobbyEnterInfo() { EnterState = LobbyEnterResponseEnum.Success }, multiplayer);
                };
            }
            else
            {
                StringBuilder text = MyTexts.Get(MyCommonTexts.DialogTextJoiningWorld);

                MyGuiScreenProgress progress = new MyGuiScreenProgress(text, MyCommonTexts.Cancel);
                MyGuiSandbox.AddScreen(progress);
                progress.ProgressCancelled += () =>
                {
                    multiplayer.Dispose();
                    if (MyMultiplayer.Static != null)
                    {
                        MyMultiplayer.Static.Dispose();
                    }
                };

                multiplayer.OnJoin += delegate
                {
                    MyJoinGameHelper.OnJoin(progress, SteamSDK.Result.OK, new LobbyEnterInfo() { EnterState = LobbyEnterResponseEnum.Success }, multiplayer);
                };
            }
        }

        public static void JoinGame(ulong lobbyId)
        {
            StringBuilder text = MyTexts.Get(MyCommonTexts.DialogTextJoiningWorld);

            MyGuiScreenProgress progress = new MyGuiScreenProgress(text, MyCommonTexts.Cancel);
            MyGuiSandbox.AddScreen(progress);

            progress.ProgressCancelled += () => MyGuiScreenMainMenu.ReturnToMainMenu();

            MyLog.Default.WriteLine("Joining lobby: " + lobbyId);

            var result = MyMultiplayer.JoinLobby(lobbyId);
            result.JoinDone += (joinResult, info, multiplayer) => OnJoin(progress, joinResult, info, multiplayer);

            progress.ProgressCancelled += () => result.Cancel();
        }

        public static void JoinBattleGame(ulong lobbyId)
        {
            StringBuilder text = MyTexts.Get(MySpaceTexts.DialogTextJoiningBattle);

            MyGuiScreenProgress progress = new MyGuiScreenProgress(text, MyCommonTexts.Cancel);
            MyGuiSandbox.AddScreen(progress);

            progress.ProgressCancelled += () => MyGuiScreenMainMenu.ReturnToMainMenu();

            MyLog.Default.WriteLine("Joining battle lobby: " + lobbyId);

            var result = MyMultiplayer.JoinLobby(lobbyId);
            result.JoinDone += (joinResult, info, multiplayer) => OnJoinBattle(progress, joinResult, info, multiplayer);

            progress.ProgressCancelled += () => result.Cancel();
        }

        public static void JoinScenarioGame(ulong lobbyId)
        {
            StringBuilder text = MyTexts.Get(MySpaceTexts.DialogTextJoiningScenario);

            MyGuiScreenProgress progress = new MyGuiScreenProgress(text, MyCommonTexts.Cancel);
            MyGuiSandbox.AddScreen(progress);

            progress.ProgressCancelled += () => MyGuiScreenMainMenu.ReturnToMainMenu();

            MyLog.Default.WriteLine("Joining scenario lobby: " + lobbyId);

            var result = MyMultiplayer.JoinLobby(lobbyId);
            result.JoinDone += (joinResult, info, multiplayer) => OnJoinScenario(progress, joinResult, info, multiplayer);

            progress.ProgressCancelled += () => result.Cancel();
        }

        public static void OnJoin(MyGuiScreenProgress progress, Result joinResult, LobbyEnterInfo enterInfo, MyMultiplayerBase multiplayer)
        {
            // HACK: To hide multiplayer from ME
            //if (!MySandboxGame.Services.SteamService.IsActive || MySandboxGame.Services.SteamService.AppId * 2 == 667900)
            //    return;

            MyLog.Default.WriteLine(String.Format("Lobby join response: {0}, enter state: {1}", joinResult.ToString(), enterInfo.EnterState));

            if (joinResult == Result.OK && enterInfo.EnterState == LobbyEnterResponseEnum.Success && multiplayer.GetOwner() != Sync.MyId)
            {
                DownloadWorld(progress, multiplayer);
            }
            else
            {
                string status = "ServerHasLeft";
                if (joinResult != Result.OK)
                {
                    status = joinResult.ToString();
                }
                else if (enterInfo.EnterState != LobbyEnterResponseEnum.Success)
                {
                    status = enterInfo.EnterState.ToString();
                }

                OnJoinFailed(progress, multiplayer, status);
            }
        }

        private static void DownloadWorld(MyGuiScreenProgress progress, MyMultiplayerBase multiplayer)
        {
            if (progress.Text != null)
            {
                progress.Text.Clear();
                progress.Text.Append(MyTexts.Get(MyCommonTexts.MultiplayerStateConnectingToServer));
            }

            MyLog.Default.WriteLine("World requested");

            const float worldRequestTimeout = 40; // in seconds
            Stopwatch worldRequestTime = Stopwatch.StartNew();

            ulong serverId = multiplayer.GetOwner();
            bool connected = false;
            progress.Tick += () =>
            {
                P2PSessionState state = default(P2PSessionState);
                Peer2Peer.GetSessionState(multiplayer.ServerId, ref state);

                if (!connected && state.ConnectionActive)
                {
                    MyLog.Default.WriteLine("World requested - connection alive");
                    connected = true;
                    if (progress.Text != null)
                    {
                        progress.Text.Clear();
                        progress.Text.Append(MyTexts.Get(MyCommonTexts.MultiplayerStateWaitingForServer));
                    }
                }

                //progress.Text.Clear();
                //progress.Text.AppendLine("Connecting: " + state.Connecting);
                //progress.Text.AppendLine("ConnectionActive: " + state.ConnectionActive);
                //progress.Text.AppendLine("Relayed: " + state.UsingRelay);
                //progress.Text.AppendLine("Bytes queued: " + state.BytesQueuedForSend);
                //progress.Text.AppendLine("Packets queued: " + state.PacketsQueuedForSend);
                //progress.Text.AppendLine("Last session error: " + state.LastSessionError);
                //progress.Text.AppendLine("Original server: " + serverId);
                //progress.Text.AppendLine("Current server: " + multiplayer.Lobby.GetOwner());
                //progress.Text.AppendLine("Game version: " + multiplayer.AppVersion);

                if (serverId != multiplayer.GetOwner())
                {
                    MyLog.Default.WriteLine("World requested - failed, server changed");
                    progress.Cancel();
                    MyGuiSandbox.Show(MyCommonTexts.MultiplayerErrorServerHasLeft);
                    multiplayer.Dispose();
                }

                if (worldRequestTime.IsRunning && worldRequestTime.Elapsed.TotalSeconds > worldRequestTimeout)
                {
                    MyLog.Default.WriteLine("World requested - failed, server changed");
                    progress.Cancel();
                    MyGuiSandbox.Show(MyCommonTexts.MultiplaterJoin_ServerIsNotResponding);
                    multiplayer.Dispose();
                }
            };

            var downloadResult = multiplayer.DownloadWorld();
            downloadResult.ProgressChanged += (result) =>
            {
                worldRequestTime.Stop();
                OnDownloadProgressChanged(progress, result, multiplayer);
            };

            progress.ProgressCancelled += () =>
            {
                downloadResult.Cancel();
                multiplayer.Dispose();
                //var joinScreen = MyScreenManager.GetScreenWithFocus() as MyGuiScreenJoinGame;
                //if (joinScreen != null)
                //  joinScreen.ReloadList();
            };
        }

        public static void OnJoinBattle(MyGuiScreenProgress progress, Result joinResult, LobbyEnterInfo enterInfo, MyMultiplayerBase multiplayer)
        {
            MyLog.Default.WriteLine(String.Format("Battle lobby join response: {0}, enter state: {1}", joinResult.ToString(), enterInfo.EnterState));

            bool battleCanBeJoined = multiplayer != null && multiplayer.BattleCanBeJoined;
            if (joinResult == Result.OK && enterInfo.EnterState == LobbyEnterResponseEnum.Success && battleCanBeJoined && multiplayer.GetOwner() != Sync.MyId)
            {
                // Create session with empty world
                Debug.Assert(MySession.Static == null);

                MySession.CreateWithEmptyWorld(multiplayer);
                MySession.Static.Settings.Battle = true;

                progress.CloseScreen();

                MyLog.Default.WriteLine("Battle lobby joined");

                if (MyPerGameSettings.GUI.BattleLobbyClientScreen != null)
                    MyGuiSandbox.AddScreen(MyGuiSandbox.CreateScreen(MyPerGameSettings.GUI.BattleLobbyClientScreen));
                else
                    Debug.Fail("No battle lobby client screen");
            }
            else
            {
                bool statusFullMessage = true;
                string status = MyTexts.GetString(MyCommonTexts.MultiplayerErrorServerHasLeft);

                if (joinResult != Result.OK)
                {
                    status = joinResult.ToString();
                    statusFullMessage = false;
                }
                else if (enterInfo.EnterState != LobbyEnterResponseEnum.Success)
                {
                    status = enterInfo.EnterState.ToString();
                    statusFullMessage = false;
                }
                else if (!battleCanBeJoined)
                {
                    if (MyFakes.ENABLE_JOIN_STARTED_BATTLE)
                    {
                        status = status = MyTexts.GetString(MyCommonTexts.MultiplayerErrorSessionEnded);
                        statusFullMessage = true;
                    }
                    else
                    {
                        status = "GameStarted";
                        statusFullMessage = false;
                    }
                }

                MyLog.Default.WriteLine("Battle join failed: " + status);

                OnJoinBattleFailed(progress, multiplayer, status, statusFullMessage: statusFullMessage);
            }
        }

        public static void OnJoinScenario(MyGuiScreenProgress progress, Result joinResult, LobbyEnterInfo enterInfo, MyMultiplayerBase multiplayer)
        {
            MyLog.Default.WriteLine(String.Format("Lobby join response: {0}, enter state: {1}", joinResult.ToString(), enterInfo.EnterState));
          
            if (joinResult == Result.OK && enterInfo.EnterState == LobbyEnterResponseEnum.Success && multiplayer.GetOwner() != Sync.MyId)
            {
                // Create session with empty world
                if (MySession.Static != null)
                {
                    MySession.Static.Unload();
                    MySession.Static = null;
                }

                MySession.CreateWithEmptyWorld(multiplayer);

                progress.CloseScreen();

                MyScreenManager.CloseAllScreensNowExcept(null);
                MyLog.Default.WriteLine("Scenario lobby joined");

                if (MyPerGameSettings.GUI.ScenarioLobbyClientScreen != null)
                    MyGuiSandbox.AddScreen(MyGuiSandbox.CreateScreen(MyPerGameSettings.GUI.ScenarioLobbyClientScreen));
                else
                    Debug.Fail("No scenario lobby client screen");
            }
            else
            {
                string status = "ServerHasLeft";
                if (joinResult != Result.OK)
                {
                    status = joinResult.ToString();
                }
                else if (enterInfo.EnterState != LobbyEnterResponseEnum.Success)
                {
                    status = enterInfo.EnterState.ToString();
                }

                OnJoinBattleFailed(progress, multiplayer, status);
            }
        }

        public static void DownloadScenarioWorld(MyMultiplayerBase multiplayer)
        {
            StringBuilder text = MyTexts.Get(MyCommonTexts.MultiplayerStateConnectingToServer);

            MyGuiScreenProgress progress = new MyGuiScreenProgress(text, MyCommonTexts.Cancel);
            MyGuiSandbox.AddScreen(progress);
            // Set focus to different control than Cancel button (because focused Cancel button can be unexpectedly pressed when sending a chat message - in case server has just started game).
            progress.FocusedControl = progress.RotatingWheel;

            progress.ProgressCancelled += () =>
            {
                MyGuiScreenMainMenu.UnloadAndExitToMenu();
            };

            DownloadWorld(progress, multiplayer);
        }

        public static void DownloadBattleWorld(MyMultiplayerBase multiplayer)
        {
            StringBuilder text = MyTexts.Get(MyCommonTexts.MultiplayerStateConnectingToServer);

            MyGuiScreenProgress progress = new MyGuiScreenProgress(text, MyCommonTexts.Cancel);
            MyGuiSandbox.AddScreen(progress);
            // Set focus to different control than Cancel button (because focused Cancel button can be unexpectedly pressed when sending a chat message - in case server has just started game).
            progress.FocusedControl = progress.RotatingWheel;

            progress.ProgressCancelled += () =>
            {
                MyGuiScreenMainMenu.UnloadAndExitToMenu();
            };

            DownloadWorld(progress, multiplayer);
        }

        private static void OnJoinFailed(MyGuiScreenProgress progress, MyMultiplayerBase multiplayer, string status)
        {
            if (multiplayer != null)
            {
                multiplayer.Dispose();
            }
            progress.Cancel();
            StringBuilder error = new StringBuilder();
            error.AppendFormat(MyCommonTexts.DialogTextJoinWorldFailed, status);

            MyGuiScreenMessageBox mb = MyGuiSandbox.CreateMessageBox(messageText: error, messageCaption: MyTexts.Get(MyCommonTexts.MessageBoxCaptionError));
            MyGuiSandbox.AddScreen(mb);
        }

        private static void OnJoinBattleFailed(MyGuiScreenProgress progress, MyMultiplayerBase multiplayer, string status, bool statusFullMessage = false)
        {
            MyGuiScreenMainMenu.UnloadAndExitToMenu();

            progress.Cancel();
            StringBuilder error = new StringBuilder();
            if (statusFullMessage)
                error.Append(status);
            else
                error.AppendFormat(MySpaceTexts.DialogTextJoinBattleFailed, status);

            MyGuiScreenMessageBox mb = MyGuiSandbox.CreateMessageBox(messageText: error, messageCaption: MyTexts.Get(MyCommonTexts.MessageBoxCaptionError));
            MyGuiSandbox.AddScreen(mb);
        }

        #endregion

        #region Download progress

        private static void CheckDx11AndJoin(MyObjectBuilder_World world, MyMultiplayerBase multiplayer)
        {
            bool needsDx11 = world.Checkpoint.RequiresDX >= 11;
            if (!needsDx11 || MySandboxGame.IsDirectX11)
            {
                if (multiplayer.Battle)
                {
                    if (multiplayer.BattleCanBeJoined)
                    {
                        MyGuiScreenLoadSandbox.LoadMultiplayerBattleWorld(world, multiplayer);
                    }
                    else
                    {
                        MyLog.Default.WriteLine("World downloaded but battle game ended");
                        MyGuiScreenMainMenu.UnloadAndExitToMenu();
                        MyGuiSandbox.Show(MyCommonTexts.MultiplayerErrorSessionEnded);
                        multiplayer.Dispose();
                    }
                }
                else if (multiplayer.Scenario)
                {
                    MyGuiScreenLoadSandbox.LoadMultiplayerScenarioWorld(world, multiplayer);
                }
                else
                {
                    MyGuiScreenLoadSandbox.LoadMultiplayerSession(world, multiplayer);
                }
            }
            else
            {
                HandleDx11Needed();
            }
        }

        public static void HandleDx11Needed()
        {
            MyGuiScreenMainMenu.ReturnToMainMenu();
            if (MyDirectXHelper.IsDx11Supported())
            {
                // Has DX11, ask for switch or selecting different scenario
                MyGuiSandbox.AddScreen(MyGuiSandbox.CreateMessageBox(
                    callback: OnDX11SwitchRequestAnswer,
                    messageCaption: MyTexts.Get(MyCommonTexts.MessageBoxCaptionError),
                    messageText: MyTexts.Get(MySpaceTexts.QuickstartDX11SwitchQuestion),
                    buttonType: MyMessageBoxButtonsType.YES_NO));
            }
            else
            {
                // No DX11, ask for selecting another scenario
                var text = MyTexts.Get(MySpaceTexts.QuickstartNoDx9SelectDifferent);
                MyGuiScreenMessageBox mb = MyGuiSandbox.CreateMessageBox(messageText: text, messageCaption: MyTexts.Get(MyCommonTexts.MessageBoxCaptionError));
                MyGuiSandbox.AddScreen(mb);
            }
        }

        public static void OnDX11SwitchRequestAnswer(MyGuiScreenMessageBox.ResultEnum result)
        {
            if (result == MyGuiScreenMessageBox.ResultEnum.YES)
            {
                MySandboxGame.Config.GraphicsRenderer = MySandboxGame.DirectX11RendererKey;
                MySandboxGame.Config.Save();
                MyGuiSandbox.BackToMainMenu();
                var text = MyTexts.Get(MySpaceTexts.QuickstartDX11PleaseRestartGame);
                MyGuiScreenMessageBox mb = MyGuiSandbox.CreateMessageBox(messageText: text, messageCaption: MyTexts.Get(MyCommonTexts.MessageBoxCaptionError));
                MyGuiSandbox.AddScreen(mb);
            }
            else
            {
                var text = MyTexts.Get(MySpaceTexts.QuickstartSelectDifferent);
                MyGuiScreenMessageBox mb = MyGuiSandbox.CreateMessageBox(messageText: text, messageCaption: MyTexts.Get(MyCommonTexts.MessageBoxCaptionError));
                MyGuiSandbox.AddScreen(mb);
            }
        }

        private static void OnDownloadProgressChanged(MyGuiScreenProgress progress, MyDownloadWorldResult result, MyMultiplayerBase multiplayer)
        {
            switch (result.State)
            {
                case MyDownloadWorldStateEnum.Success:
                    progress.CloseScreen();
                    var world = multiplayer.ProcessWorldDownloadResult(result);

                    CheckDx11AndJoin(world, multiplayer);
                    break;

                case MyDownloadWorldStateEnum.InProgress:
                    if (result.ReceivedBlockCount == 1)
                        MyLog.Default.WriteLine("First world part received");
                    string percent = (result.Progress * 100).ToString("0.");
                    float size = result.ReceivedDatalength;
                    string prefix = MyUtils.FormatByteSizePrefix(ref size);
                    string worldSize = size.ToString("0.") + " " + prefix + "B";
                    if (progress.Text != null)
                        progress.Text.Clear();
                    if (float.IsNaN(result.Progress))
                    {
                        MyLog.Default.WriteLine("World requested - preemble received");
                        if (progress.Text != null)
                            progress.Text.Append(MyTexts.Get(MyCommonTexts.DialogWaitingForWorldData));
                    }
                    else
                    {
                        if (progress.Text != null)
                            progress.Text.AppendFormat(MyTexts.GetString(MyCommonTexts.DialogTextDownloadingWorld), percent, worldSize);
                    }
                    break;

                case MyDownloadWorldStateEnum.WorldNotAvailable:
                    MyLog.Default.WriteLine("World requested - world not available");
                    progress.Cancel();
                    MyGuiSandbox.Show(MyCommonTexts.DialogDownloadWorld_WorldDoesNotExists);
                    multiplayer.Dispose();
                    break;

                case MyDownloadWorldStateEnum.ConnectionFailed:
                    MyLog.Default.WriteLine("World requested - connection failed");
                    progress.Cancel();
                    MyGuiSandbox.Show(MyTexts.AppendFormat(new StringBuilder(), MyCommonTexts.MultiplayerErrorConnectionFailed, result.ConnectionError));
                    multiplayer.Dispose();
                    break;

                case MyDownloadWorldStateEnum.DeserializationFailed:
                case MyDownloadWorldStateEnum.InvalidMessage:
                    MyLog.Default.WriteLine("World requested - message invalid (wrong version?)");
                    progress.Cancel();
                    MyGuiSandbox.Show(MyCommonTexts.DialogTextDownloadWorldFailed);
                    multiplayer.Dispose();
                    break;

                default:
                    throw new InvalidBranchException();
            }
        }

        #endregion
    }
}
