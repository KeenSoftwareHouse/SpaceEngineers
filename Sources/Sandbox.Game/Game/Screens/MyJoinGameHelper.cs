#region Using

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using ParallelTasks;
using Sandbox.Common;

using Sandbox.Common.ObjectBuilders.Gui;
using Sandbox.Graphics.GUI;
using Sandbox.Engine.Networking;
using Sandbox.Engine.Utils;
using SteamSDK;

using VRageMath;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Game.World;
using Sandbox.Engine.Multiplayer;
using VRage;
using VRage.Utils;
using System.Diagnostics;
using Sandbox.Game.Screens.Helpers;
using VRage.Utils;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.Localization;
using VRage;
using System.IO;

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
                MyGuiSandbox.Show(MySpaceTexts.OnlyFriendsCanJoinThisGame);
                return false;
            }
            if (!MyMultiplayerLobby.IsLobbyCorrectVersion(lobby))
            {
                var formatString = MyTexts.GetString(MySpaceTexts.MultiplayerError_IncorrectVersion);
                var myVersion = MyBuildNumbers.ConvertBuildNumberFromIntToString(MyFinalBuildConstants.APP_VERSION);
                var serverVersion = MyBuildNumbers.ConvertBuildNumberFromIntToString(MyMultiplayerLobby.GetLobbyAppVersion(lobby));
                MyGuiSandbox.Show(new StringBuilder(String.Format(formatString, myVersion, serverVersion)));
                return false;
            }
            if (MyFakes.ENABLE_MP_DATA_HASHES && !MyMultiplayerLobby.HasSameData(lobby))
            {
                MyGuiSandbox.Show(MySpaceTexts.MultiplayerError_DifferentData);
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

            JoinGame(lobby.LobbyId);
        }

        public static void JoinScenarioGame(Lobby lobby, bool requestData = true)
        {
            // Data not received
            if (requestData && String.IsNullOrEmpty(lobby.GetLobbyData(MyMultiplayer.AppVersionTag)))
            {
                var helper = new MyLobbyHelper(lobby);
                helper.OnSuccess += (l) => JoinScenarioGame(l, false);
                if (helper.RequestData())
                    return;
            }

            if (!JoinGameTest(lobby))
                return;

            JoinScenarioGame(lobby.LobbyId);
        }

        public static void JoinBattleGame(Lobby lobby, bool requestData = true)
        {
            // Data not received
            if (requestData && String.IsNullOrEmpty(lobby.GetLobbyData(MyMultiplayer.AppVersionTag)))
            {
                var helper = new MyLobbyHelper(lobby);
                helper.OnSuccess += (l) => JoinBattleGame(l, false);
                if (helper.RequestData())
                    return;
            }

            if (!JoinGameTest(lobby))
                return;

            JoinBattleGame(lobby.LobbyId);
        }


        public static void JoinGame(GameServerItem server)
        {
            if (server.ServerVersion != MyFinalBuildConstants.APP_VERSION)
            {
                var sb = new StringBuilder();
                sb.AppendFormat(MyTexts.GetString(MySpaceTexts.MultiplayerError_IncorrectVersion), MyFinalBuildConstants.APP_VERSION, server.ServerVersion);
                MyGuiSandbox.Show(sb, MySpaceTexts.MessageBoxCaptionError);
                return;
            }
            if (MyFakes.ENABLE_MP_DATA_HASHES)
            {
                var serverHash = server.GetGameTagByPrefix("datahash");
                if (serverHash != "" && serverHash != MyDataIntegrityChecker.GetHashBase64())
                {
                    MyGuiSandbox.Show(MySpaceTexts.MultiplayerError_DifferentData);
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

            StringBuilder text = MyTexts.Get(MySpaceTexts.DialogTextJoiningWorld);

            MyGuiScreenProgress progress = new MyGuiScreenProgress(text, MySpaceTexts.Cancel);
            MyGuiSandbox.AddScreen(progress);
            progress.ProgressCancelled += () =>
                {
                    multiplayer.Dispose();
                    MyGuiScreenMainMenu.ReturnToMainMenu();
                };

            multiplayer.OnJoin += delegate
            {
                MyJoinGameHelper.OnJoin(progress, SteamSDK.Result.OK, new LobbyEnterInfo() { EnterState = LobbyEnterResponseEnum.Success }, multiplayer);
            };
        }

        public static void JoinGame(ulong lobbyId)
        {
            StringBuilder text = MyTexts.Get(MySpaceTexts.DialogTextJoiningWorld);

            MyGuiScreenProgress progress = new MyGuiScreenProgress(text, MySpaceTexts.Cancel);
            MyGuiSandbox.AddScreen(progress);

            progress.ProgressCancelled += () => MyGuiScreenMainMenu.ReturnToMainMenu();

            MyLog.Default.WriteLine("Joining lobby: " + lobbyId);

            var result = MyMultiplayer.JoinLobby(lobbyId);
            result.JoinDone += (joinResult, info, multiplayer) => OnJoin(progress, joinResult, info, multiplayer);

            progress.ProgressCancelled += () => result.Cancel();
        }

        public static void JoinBattleGame(ulong lobbyId)
        {
            StringBuilder text = MyTexts.Get(MySpaceTexts.DialogTextJoiningBattleLobby);

            MyGuiScreenProgress progress = new MyGuiScreenProgress(text, MySpaceTexts.Cancel);
            MyGuiSandbox.AddScreen(progress);

            progress.ProgressCancelled += () => MyGuiScreenMainMenu.ReturnToMainMenu();

            MyLog.Default.WriteLine("Joining battle lobby: " + lobbyId);

            var result = MyMultiplayer.JoinLobby(lobbyId);
            result.JoinDone += (joinResult, info, multiplayer) => OnJoinBattle(progress, joinResult, info, multiplayer);

            progress.ProgressCancelled += () => result.Cancel();
        }

        public static void JoinScenarioGame(ulong lobbyId)
        {
            StringBuilder text = MyTexts.Get(MySpaceTexts.DialogTextJoiningBattleLobby);

            MyGuiScreenProgress progress = new MyGuiScreenProgress(text, MySpaceTexts.Cancel);
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

            if (joinResult == Result.OK && enterInfo.EnterState == LobbyEnterResponseEnum.Success && multiplayer.GetOwner() != MySteam.UserId)
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
                progress.Text.Append(MyTexts.Get(MySpaceTexts.MultiplayerStateConnectingToServer));
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
                        progress.Text.Append(MyTexts.Get(MySpaceTexts.MultiplayerStateWaitingForServer));
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
                    MyGuiSandbox.Show(MySpaceTexts.MultiplayerErrorServerHasLeft);
                    multiplayer.Dispose();
                }

                if (worldRequestTime.IsRunning && worldRequestTime.Elapsed.TotalSeconds > worldRequestTimeout)
                {
                    MyLog.Default.WriteLine("World requested - failed, server changed");
                    progress.Cancel();
                    MyGuiSandbox.Show(MySpaceTexts.MultiplaterJoin_ServerIsNotResponding);
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
            if (joinResult == Result.OK && enterInfo.EnterState == LobbyEnterResponseEnum.Success && battleCanBeJoined && multiplayer.GetOwner() != MySteam.UserId)
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
                string status = "ServerHasLeft";
                if (joinResult != Result.OK)
                {
                    status = joinResult.ToString();
                }
                else if (enterInfo.EnterState != LobbyEnterResponseEnum.Success)
                {
                    status = enterInfo.EnterState.ToString();
                }
                else if (!battleCanBeJoined)
                {
                    status = "Started battle cannot be joined";
                }

                OnJoinBattleFailed(progress, multiplayer, status);
            }
        }

        public static void OnJoinScenario(MyGuiScreenProgress progress, Result joinResult, LobbyEnterInfo enterInfo, MyMultiplayerBase multiplayer)
        {
            MyLog.Default.WriteLine(String.Format("Lobby join response: {0}, enter state: {1}", joinResult.ToString(), enterInfo.EnterState));

            if (joinResult == Result.OK && enterInfo.EnterState == LobbyEnterResponseEnum.Success && multiplayer.GetOwner() != MySteam.UserId)
            {
                // Create session with empty world
                if (MySession.Static != null)
                {
                    MySession.Static.Unload();
                    MySession.Static = null;
                }

                MySession.CreateWithEmptyWorld(multiplayer);

                progress.CloseScreen();

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
            StringBuilder text = MyTexts.Get(MySpaceTexts.MultiplayerStateConnectingToServer);

            MyGuiScreenProgress progress = new MyGuiScreenProgress(text, MySpaceTexts.Cancel);
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
            StringBuilder text = MyTexts.Get(MySpaceTexts.MultiplayerStateConnectingToServer);

            MyGuiScreenProgress progress = new MyGuiScreenProgress(text, MySpaceTexts.Cancel);
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
            error.AppendFormat(MySpaceTexts.DialogTextJoinWorldFailed, status);

            MyGuiScreenMessageBox mb = MyGuiSandbox.CreateMessageBox(messageText: error, messageCaption: MyTexts.Get(MySpaceTexts.MessageBoxCaptionError));
            MyGuiSandbox.AddScreen(mb);
        }

        private static void OnJoinBattleFailed(MyGuiScreenProgress progress, MyMultiplayerBase multiplayer, string status)
        {
            MyGuiScreenMainMenu.UnloadAndExitToMenu();

            progress.Cancel();
            StringBuilder error = new StringBuilder();
            error.AppendFormat(MySpaceTexts.DialogTextJoinBattleLobbyFailed, status);

            MyGuiScreenMessageBox mb = MyGuiSandbox.CreateMessageBox(messageText: error, messageCaption: MyTexts.Get(MySpaceTexts.MessageBoxCaptionError));
            MyGuiSandbox.AddScreen(mb);
        }

        #endregion

        #region Download progress

        private static void OnDownloadProgressChanged(MyGuiScreenProgress progress, MyDownloadWorldResult result, MyMultiplayerBase multiplayer)
        {
            switch (result.State)
            {
                case MyDownloadWorldStateEnum.Success:
                    progress.CloseScreen();
                    var world = multiplayer.ProcessWorldDownloadResult(result);
                    if (MyFakes.ENABLE_BATTLE_SYSTEM && multiplayer.Battle)
                        MyGuiScreenLoadSandbox.LoadMultiplayerBattleWorld(world, multiplayer);
                    else if (multiplayer.Scenario)
                        MyGuiScreenLoadSandbox.LoadMultiplayerScenarioWorld(world, multiplayer);
                    else
                        MyGuiScreenLoadSandbox.LoadMultiplayerSession(world, multiplayer);
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
                            progress.Text.Append(MyTexts.Get(MySpaceTexts.DialogWaitingForWorldData));
                    }
                    else
                    {
                        if (progress.Text != null)
                            progress.Text.AppendFormat(MyTexts.GetString(MySpaceTexts.DialogTextDownloadingWorld), percent, worldSize);
                    }
                    break;

                case MyDownloadWorldStateEnum.WorldNotAvailable:
                    MyLog.Default.WriteLine("World requested - world not available");
                    progress.Cancel();
                    MyGuiSandbox.Show(MySpaceTexts.DialogDownloadWorld_WorldDoesNotExists);
                    multiplayer.Dispose();
                    break;

                case MyDownloadWorldStateEnum.ConnectionFailed:
                    MyLog.Default.WriteLine("World requested - connection failed");
                    progress.Cancel();
                    MyGuiSandbox.Show(MyTexts.AppendFormat(new StringBuilder(), MySpaceTexts.MultiplayerErrorConnectionFailed, result.ConnectionError));
                    multiplayer.Dispose();
                    break;

                case MyDownloadWorldStateEnum.DeserializationFailed:
                case MyDownloadWorldStateEnum.InvalidMessage:
                    MyLog.Default.WriteLine("World requested - message invalid (wrong version?)");
                    progress.Cancel();
                    MyGuiSandbox.Show(MySpaceTexts.DialogTextDownloadWorldFailed);
                    multiplayer.Dispose();
                    break;

                default:
                    throw new InvalidBranchException();
            }
        }


        #endregion
    }
}
