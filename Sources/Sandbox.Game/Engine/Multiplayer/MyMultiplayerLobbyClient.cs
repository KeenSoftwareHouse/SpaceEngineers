#region Using

using Sandbox.Engine.Networking;
using Sandbox.Engine.Utils;
using Sandbox.Game.Gui;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.World;
using Sandbox.Graphics.GUI;
using SteamSDK;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using VRage;
using VRage.Game;
using VRage.Utils;
using VRage.Trace;
using VRage.Library.Utils;
using VRage.Network;

#endregion

namespace Sandbox.Engine.Multiplayer
{
    /// <summary>
    /// Container of multiplayer classes
    /// </summary>
    public sealed class MyMultiplayerLobbyClient : MyMultiplayerClientBase
    {
        public readonly Lobby Lobby;

        public override bool IsServer { get { return ServerId == Sync.MyId; } }

        public override string WorldName
        {
            get { return GetLobbyWorldName(Lobby); }
            set { Lobby.SetLobbyData(MyMultiplayer.WorldNameTag, value); }
        }

        public override MyGameModeEnum GameMode
        {
            get 
            {
                return GetLobbyGameMode(Lobby);
            }
            set { Lobby.SetLobbyData(MyMultiplayer.GameModeTag, ((int)value).ToString()); }
        }

        public override float InventoryMultiplier
        {
            get { return GetLobbyFloat(MyMultiplayer.InventoryMultiplierTag, Lobby, 1); }
            set { Lobby.SetLobbyData(MyMultiplayer.InventoryMultiplierTag, value.ToString(CultureInfo.InvariantCulture)); }
        }

        public override float AssemblerMultiplier
        {
            get { return GetLobbyFloat(MyMultiplayer.AssemblerMultiplierTag, Lobby, 1); }
            set { Lobby.SetLobbyData(MyMultiplayer.AssemblerMultiplierTag, value.ToString(CultureInfo.InvariantCulture)); }
        }

        public override float RefineryMultiplier
        {
            get { return GetLobbyFloat(MyMultiplayer.RefineryMultiplierTag, Lobby, 1); }
            set { Lobby.SetLobbyData(MyMultiplayer.RefineryMultiplierTag, value.ToString(CultureInfo.InvariantCulture)); }
        }

        public override float WelderMultiplier
        {
            get { return GetLobbyFloat(MyMultiplayer.WelderMultiplierTag, Lobby, 1); }
            set { Lobby.SetLobbyData(MyMultiplayer.WelderMultiplierTag, value.ToString(CultureInfo.InvariantCulture)); }
        }

        public override float GrinderMultiplier
        {
            get { return GetLobbyFloat(MyMultiplayer.GrinderMultiplierTag, Lobby, 1); }
            set { Lobby.SetLobbyData(MyMultiplayer.GrinderMultiplierTag, value.ToString(CultureInfo.InvariantCulture)); }
        }

        public override string HostName
        {
            get { return GetLobbyHostName(Lobby); }
            set { Lobby.SetLobbyData(MyMultiplayer.HostNameTag, value); }
        }

        public override ulong WorldSize
        {
            get
            {
                return GetLobbyWorldSize(Lobby);
            }
            set { Lobby.SetLobbyData(MyMultiplayer.WorldSizeTag, value.ToString()); }
        }

        public override int AppVersion
        {
            get
            {
                return GetLobbyAppVersion(Lobby);
            }
            set { Lobby.SetLobbyData(MyMultiplayer.AppVersionTag, value.ToString()); }
        }

        public override string DataHash
        {
            get { return Lobby.GetLobbyData(MyMultiplayer.DataHashTag); }
            set { Lobby.SetLobbyData(MyMultiplayer.DataHashTag, value); }
        }

        public override int MaxPlayers
        {
            get { return 16; }
        }

        public override int ModCount
        {
            get { return GetLobbyModCount(Lobby); }
            protected set { Lobby.SetLobbyData(MyMultiplayer.ModCountTag, value.ToString()); }
        }

        public override List<MyObjectBuilder_Checkpoint.ModItem> Mods
        {
            get
            {
                return GetLobbyMods(Lobby);
            }
            set
            {
                ModCount = value.Count;
                int i = ModCount - 1;
                foreach (var mod in value)
                {
                    var data = mod.PublishedFileId + "_" + mod.FriendlyName;
                    Lobby.SetLobbyData(MyMultiplayer.ModItemTag + i--, data);
                }
            }
        }

        public override int ViewDistance
        {
            get { return GetLobbyViewDistance(Lobby); }
            set { Lobby.SetLobbyData(MyMultiplayer.ViewDistanceTag, value.ToString()); }
        }


        public override bool Scenario
        {
            get { return GetLobbyBool(MyMultiplayer.ScenarioTag, Lobby, false); }
            set { Lobby.SetLobbyData(MyMultiplayer.ScenarioTag, value.ToString()); }
        }

        public override string ScenarioBriefing
        {
            get { return Lobby.GetLobbyData(MyMultiplayer.ScenarioBriefingTag); }
            set { Lobby.SetLobbyData(MyMultiplayer.ScenarioBriefingTag, (value==null || value.Length<1?" ":value)); }
        }

        public override DateTime ScenarioStartTime
        {
            get { return GetLobbyDateTime(MyMultiplayer.ScenarioStartTimeTag, Lobby, DateTime.MinValue); }
            set { Lobby.SetLobbyData(MyMultiplayer.ScenarioStartTimeTag, value.ToString(CultureInfo.InvariantCulture)); }
        }

        //public override bool Battle
        //{
        //    get { return GetLobbyBool(MyMultiplayer.BattleTag, Lobby, false); }
        //    set { Lobby.SetLobbyData(MyMultiplayer.BattleTag, value.ToString()); }
        //}

        //public override float BattleRemainingTime
        //{
        //    get { return GetLobbyFloat(MyMultiplayer.BattleRemainingTimeTag, Lobby, 0); }
        //    set { Lobby.SetLobbyData(MyMultiplayer.BattleRemainingTimeTag, value.ToString(CultureInfo.InvariantCulture)); }
        //}

        //public override bool BattleCanBeJoined
        //{
        //    get { return GetLobbyBool(MyMultiplayer.BattleCanBeJoinedTag, Lobby, false); }
        //    set { Lobby.SetLobbyData(MyMultiplayer.BattleCanBeJoinedTag, value.ToString()); }
        //}

        //public override ulong BattleWorldWorkshopId
        //{
        //    get { return GetLobbyULong(MyMultiplayer.BattleWorldWorkshopIdTag, Lobby, 0); }
        //    set { Lobby.SetLobbyData(MyMultiplayer.BattleWorldWorkshopIdTag, value.ToString()); }
        //}

        //public override int BattleFaction1MaxBlueprintPoints
        //{
        //    get { return GetLobbyInt(MyMultiplayer.BattleFaction1MaxBlueprintPointsTag, Lobby, 0); }
        //    set { Lobby.SetLobbyData(MyMultiplayer.BattleFaction1MaxBlueprintPointsTag, value.ToString()); }
        //}

        //public override int BattleFaction2MaxBlueprintPoints
        //{
        //    get { return GetLobbyInt(MyMultiplayer.BattleFaction2MaxBlueprintPointsTag, Lobby, 0); }
        //    set { Lobby.SetLobbyData(MyMultiplayer.BattleFaction2MaxBlueprintPointsTag, value.ToString()); }
        //}

        //public override int BattleFaction1BlueprintPoints
        //{
        //    get { return GetLobbyInt(MyMultiplayer.BattleFaction1BlueprintPointsTag, Lobby, 0); }
        //    set { Lobby.SetLobbyData(MyMultiplayer.BattleFaction1BlueprintPointsTag, value.ToString()); }
        //}

        //public override int BattleFaction2BlueprintPoints
        //{
        //    get { return GetLobbyInt(MyMultiplayer.BattleFaction2BlueprintPointsTag, Lobby, 0); }
        //    set { Lobby.SetLobbyData(MyMultiplayer.BattleFaction2BlueprintPointsTag, value.ToString()); }
        //}

        //public override int BattleMapAttackerSlotsCount
        //{
        //    get { return GetLobbyInt(MyMultiplayer.BattleMapAttackerSlotsCountTag, Lobby, 0); }
        //    set { Lobby.SetLobbyData(MyMultiplayer.BattleMapAttackerSlotsCountTag, value.ToString()); }
        //}

        //public override long BattleFaction1Id
        //{
        //    get { return GetLobbyLong(MyMultiplayer.BattleFaction1IdTag, Lobby, 0); }
        //    set { Lobby.SetLobbyData(MyMultiplayer.BattleFaction1IdTag, value.ToString()); }
        //}

        //public override long BattleFaction2Id
        //{
        //    get { return GetLobbyLong(MyMultiplayer.BattleFaction2IdTag, Lobby, 0); }
        //    set { Lobby.SetLobbyData(MyMultiplayer.BattleFaction2IdTag, value.ToString()); }
        //}

        //public override int BattleFaction1Slot
        //{
        //    get { return GetLobbyInt(MyMultiplayer.BattleFaction1SlotTag, Lobby, 0); }
        //    set { Lobby.SetLobbyData(MyMultiplayer.BattleFaction1SlotTag, value.ToString()); }
        //}

        //public override int BattleFaction2Slot
        //{
        //    get { return GetLobbyInt(MyMultiplayer.BattleFaction2SlotTag, Lobby, 0); }
        //    set { Lobby.SetLobbyData(MyMultiplayer.BattleFaction2SlotTag, value.ToString()); }
        //}

        //public override bool BattleFaction1Ready
        //{
        //    get { return GetLobbyBool(MyMultiplayer.BattleFaction1ReadyTag, Lobby, false); }
        //    set { Lobby.SetLobbyData(MyMultiplayer.BattleFaction1ReadyTag, value.ToString()); }
        //}

        //public override bool BattleFaction2Ready
        //{
        //    get { return GetLobbyBool(MyMultiplayer.BattleFaction2ReadyTag, Lobby, false); }
        //    set { Lobby.SetLobbyData(MyMultiplayer.BattleFaction2ReadyTag, value.ToString()); }
        //}

        //public override int BattleTimeLimit
        //{
        //    get { return GetLobbyInt(MyMultiplayer.BattleTimeLimitTag, Lobby, 0); }
        //    set { Lobby.SetLobbyData(MyMultiplayer.BattleTimeLimitTag, value.ToString()); }
        //}

        private bool m_serverDataValid;

        public new MyReplicationClient ReplicationLayer { get { return (MyReplicationClient)base.ReplicationLayer; } }

        internal MyMultiplayerLobbyClient(Lobby lobby, MySyncLayer syncLayer)
            : base(syncLayer)
        {
            Lobby = lobby;
            ServerId = Lobby.GetOwner();

            SyncLayer.RegisterClientEvents(this);


            SyncLayer.TransportLayer.IsBuffering = true;

            SetReplicationLayer(new MyReplicationClient(this, CreateClientState(), MyEngineConstants.UPDATE_STEP_SIZE_IN_MILLISECONDS));
            ReplicationLayer.UseSmoothPing = MyFakes.MULTIPLAYER_SMOOTH_PING;
            ReplicationLayer.UseSmoothCorrection = MyFakes.MULTIPLAYER_SMOOTH_TIMESTAMP_CORRECTION;
            syncLayer.TransportLayer.Register(MyMessageId.SERVER_DATA, ReplicationLayer.ProcessServerData);
            syncLayer.TransportLayer.Register(MyMessageId.REPLICATION_CREATE, ReplicationLayer.ProcessReplicationCreate);
            syncLayer.TransportLayer.Register(MyMessageId.REPLICATION_DESTROY, ReplicationLayer.ProcessReplicationDestroy);
            syncLayer.TransportLayer.Register(MyMessageId.SERVER_STATE_SYNC, ReplicationLayer.ProcessStateSync);
            syncLayer.TransportLayer.Register(MyMessageId.RPC, ReplicationLayer.ProcessEvent);
            syncLayer.TransportLayer.Register(MyMessageId.REPLICATION_STREAM_BEGIN, ReplicationLayer.ProcessReplicationCreateBegin);

            Debug.Assert(!IsServer, "Wrong object created");

            MySteam.API.Matchmaking.LobbyChatUpdate += Matchmaking_LobbyChatUpdate;
            MySteam.API.Matchmaking.LobbyChatMsg += Matchmaking_LobbyChatMsg;
            ClientLeft += MyMultiplayerLobby_ClientLeft;
            AcceptMemberSessions();
        }

        void MyMultiplayerLobby_ClientLeft(ulong userId, ChatMemberStateChangeEnum stateChange)
        {
            if (userId == ServerId)
            {
                Peer2Peer.CloseSession(userId);
            }

            MySandboxGame.Log.WriteLineAndConsole("Player left: " + GetMemberName(userId) + " (" + userId + ")");
            MyTrace.Send(TraceWindow.Multiplayer, "Player left: " + stateChange.ToString());
        }

        public override bool IsCorrectVersion()
        {
            return IsLobbyCorrectVersion(Lobby);
        }

        void Matchmaking_LobbyChatUpdate(Lobby lobby, ulong changedUser, ulong makingChangeUser, ChatMemberStateChangeEnum stateChange)
        {
            //System.Diagnostics.Debug.Assert(MySession.Static != null);

            if (lobby.LobbyId == Lobby.LobbyId)
            {
                if (stateChange == ChatMemberStateChangeEnum.Entered)
                {
                    MySandboxGame.Log.WriteLineAndConsole("Player entered: " + MySteam.API.Friends.GetPersonaName(changedUser) + " (" + changedUser + ")");
                    MyTrace.Send(TraceWindow.Multiplayer, "Player entered");
                    Peer2Peer.AcceptSession(changedUser);

                    // When some clients connect at the same time then some of them can have already added clients 
                    // (see function MySyncLayer.RegisterClientEvents which registers all Members in Lobby).
                    if (Sync.Clients == null || !Sync.Clients.HasClient(changedUser))
                    {
                        RaiseClientJoined(changedUser);                 
                    }

                    if (MySandboxGame.IsGameReady && changedUser != ServerId)
                    {
                        // Player is able to connect to the battle which already started - player is then kicked and we do not want to show connected message in HUD.
                        var playerJoined = new MyHudNotification(MyCommonTexts.NotificationClientConnected, 5000, level: MyNotificationLevel.Important);
                        playerJoined.SetTextFormatArguments(MySteam.API.Friends.GetPersonaName(changedUser));
                        MyHud.Notifications.Add(playerJoined);
                    }
                }
                else
                {
                    // Kicked client can be already removed from Clients
                    if (Sync.Clients == null || Sync.Clients.HasClient(changedUser))
                        RaiseClientLeft(changedUser, stateChange);

                    if (changedUser == ServerId)
                    {
                        MyTrace.Send(TraceWindow.Multiplayer, "Host left: " + stateChange.ToString());
                        RaiseHostLeft();

                        MySessionLoader.UnloadAndExitToMenu();
                        MyGuiSandbox.AddScreen(MyGuiSandbox.CreateMessageBox(
                            messageCaption: MyTexts.Get(MyCommonTexts.MessageBoxCaptionError),
                            messageText: MyTexts.Get(MyCommonTexts.MultiplayerErrorServerHasLeft)));

                        // Set new server
                        //ServerId = Lobby.GetOwner();

                        //if (ServerId == Sync.MyId)
                        //{
                        //    Lobby.SetLobbyData(HostNameTag, Sync.MyName);
                        //}
                    }
                    else if (MySandboxGame.IsGameReady)
                    {
                        var playerLeft = new MyHudNotification(MyCommonTexts.NotificationClientDisconnected, 5000, level: MyNotificationLevel.Important);
                        playerLeft.SetTextFormatArguments(MySteam.API.Friends.GetPersonaName(changedUser));
                        MyHud.Notifications.Add(playerLeft);
                    }
                }
            }
        }

        void Matchmaking_LobbyChatMsg(Lobby lobby, ulong steamUserID, byte chatEntryTypeIn, uint chatID)
        {
            string messageText;
            ChatEntryTypeEnum chatEntryType;
            GetChatMessage((int)chatID, out messageText, out chatEntryType);
            RaiseChatMessageReceived(steamUserID, messageText, chatEntryType);
        }

        private void AcceptMemberSessions()
        {
            for (int i = 0; i < Lobby.MemberCount; i++)
            {
                var member = Lobby.GetLobbyMemberByIndex(i);
                if (member != Sync.MyId && member == ServerId)
                {
                    Peer2Peer.AcceptSession(member);
                }
            }
        }

        public override MyDownloadWorldResult DownloadWorld()
        {
            MyTrace.Send(TraceWindow.Multiplayer, "World request sent");
            MyDownloadWorldResult ret = new MyDownloadWorldResult(MyMultiplayer.WorldDownloadChannel, Lobby.GetOwner(), this);

            MyControlWorldRequestMsg msg = new MyControlWorldRequestMsg();
            SendControlMessage(ServerId, ref msg);
            return ret;
        }

        public override void DisconnectClient(ulong userId)
        {
            RaiseClientLeft(userId, ChatMemberStateChangeEnum.Disconnected);
        }
        
        public override void KickClient(ulong userId)
        {
            // In standard MP games, only the game server can kick players
            var myId = SteamAPI.Instance.GetSteamUserId();
            if (userId == myId || Lobby.GetOwner() != myId)
                return;

            MyControlKickClientMsg msg = new MyControlKickClientMsg();
            msg.KickedClient = userId;
            MyLog.Default.WriteLineAndConsole("Player " + GetMemberName(userId) + " kicked");
            SendControlMessageToAll(ref msg);

            RaiseClientLeft(userId, ChatMemberStateChangeEnum.Kicked);
        }

        public override void BanClient(ulong userId, bool banned)
        {
            System.Diagnostics.Debug.Fail("Ban is not supported in lobbies");
        }

        public override void Tick()
        {
            base.Tick();

            // TODO: Hack for invisible battle games - sometimes values are not written to Lobby so we try it again here
            if (!m_serverDataValid)
            {
                if (AppVersion == 0) 
                    MySession.Static.StartServer(this);

                m_serverDataValid = true;
            }

            //var delta = TimeSpan.FromMilliseconds(SyncLayer.Interpolation.Timer.AverageDeltaMilliseconds);
            //Profiler.CustomValue("Average delta ", (float)delta.TotalMilliseconds + 10, delta + TimeSpan.FromMilliseconds(10));

            //List<TimeSpan> deltas = new List<long>();
            //SyncLayer.Interpolation.Timer.GetDeltas(deltas);
            //if (deltas.Count > 0)
            //{
            //    var lastDelta = TimeSpan.FromTicks(deltas[deltas.Count - 1]);
            //    Profiler.CustomValue("Last delta", (float)lastDelta.TotalMilliseconds + 10, lastDelta + TimeSpan.FromMilliseconds(10));

            //    long sum = 0;
            //    foreach (var x in deltas)
            //    {
            //        sum += Math.Abs(x);
            //    }
            //    sum /= deltas.Count;
            //    Profiler.CustomValue("Sum abs delta", (float)sum, TimeSpan.FromTicks(sum));
            //}

            //Engine.Utils.MyFakes.EMULATE_LAG = TimeSpan.FromMilliseconds(250);
            //Engine.Utils.MyFakes.EMULATE_LAG = TimeSpan.Zero;
        }

        public override void SendChatMessage(string text)
        {
            var chatMsg = new ChatMessageBuffer();
            chatMsg.Text.Append(text);
            Lobby.SendChatMessage(chatMsg);
        }

        void GetChatMessage(int chatMsgID, out string messageText, out ChatEntryTypeEnum chatEntryType)
        {
            ChatMessageBuffer result = new ChatMessageBuffer();
            ulong senderID;
            Lobby.GetLobbyChatEntry(chatMsgID, result, out senderID, out chatEntryType);
            messageText = result.Text.ToString();
        }

        public override void Dispose()
        {
            MyTrace.Send(TraceWindow.Multiplayer, "Multiplayer closed");
            MySteam.API.Matchmaking.LobbyChatUpdate -= Matchmaking_LobbyChatUpdate;
            //MySteam.API.Matchmaking.LobbyDataUpdate -= Matchmaking_LobbyDataUpdate;
            MySteam.API.Matchmaking.LobbyChatMsg -= Matchmaking_LobbyChatMsg;
            
            if (Lobby.IsValid)
            {
                MyTrace.Send(TraceWindow.Multiplayer, "Leaving lobby");
                CloseMemberSessions();
                Lobby.Leave();
            }

            base.Dispose();
        }

        public override MemberCollection Members
        {
            get { return Lobby.Members; }
        }

        public override int MemberCount
        {
            get { return Lobby.MemberCount; }
        }

        public override ulong GetMemberByIndex(int memberIndex)
        {
            return Lobby.GetLobbyMemberByIndex(memberIndex);
        }

        public override ulong LobbyId
        {
            get { return Lobby.LobbyId; }
        }

        public override int MemberLimit
        {
            get { return Lobby.MemberLimit; }
            set {  }
        }

        public override bool IsAdmin(ulong steamID)
        {
            return Lobby.GetOwner() == steamID;
        }

        public override ulong GetOwner()
        {
            return Lobby.GetOwner();
        }

        public override void SetOwner(ulong owner)
        {
            Lobby.SetOwner(owner);
        }

        public override LobbyTypeEnum GetLobbyType()
        {
            return Lobby.GetLobbyType();
        }

        public override void SetLobbyType(LobbyTypeEnum type)
        {
            Lobby.SetLobbyType(type);
        }

        public override void SetMemberLimit(int limit)
        {
            Lobby.SetMemberLimit(limit);
        }

        public static bool IsLobbyCorrectVersion(Lobby lobby)
        {
            return GetLobbyAppVersion(lobby) == MyFinalBuildConstants.APP_VERSION;
        }

        public static MyGameModeEnum GetLobbyGameMode(Lobby lobby)
        {
            int val;
            if (int.TryParse(lobby.GetLobbyData(MyMultiplayer.GameModeTag), out val))
                return (MyGameModeEnum)val;
            else
                return MyGameModeEnum.Creative;
        }

        public static float GetLobbyFloat(string key, Lobby lobby, float defValue)
        {
            float val;
            if (float.TryParse(lobby.GetLobbyData(key), NumberStyles.Float, CultureInfo.InvariantCulture, out val))
                return val;
            else
                return defValue;
        }

        public static int GetLobbyInt(string key, Lobby lobby, int defValue)
        {
            int val;
            if (int.TryParse(lobby.GetLobbyData(key), NumberStyles.Integer, CultureInfo.InvariantCulture, out val))
                return val;
            else
                return defValue;
        }

        public static DateTime GetLobbyDateTime(string key, Lobby lobby, DateTime defValue)
        {
            DateTime val;
            if (DateTime.TryParse(lobby.GetLobbyData(key), CultureInfo.InvariantCulture, DateTimeStyles.None, out val))
                return val;
            else
                return defValue;
        }

        public static long GetLobbyLong(string key, Lobby lobby, long defValue)
        {
            long val;
            if (long.TryParse(lobby.GetLobbyData(key), out val))
                return val;
            else
                return defValue;
        }

        public static ulong GetLobbyULong(string key, Lobby lobby, ulong defValue)
        {
            ulong val;
            if (ulong.TryParse(lobby.GetLobbyData(key), out val))
                return val;
            else
                return defValue;
        }

        public static bool GetLobbyBool(string key, Lobby lobby, bool defValue)
        {
            bool val;
            if (bool.TryParse(lobby.GetLobbyData(key), out val))
                return val;
            else
                return defValue;
        }

        public static string GetLobbyWorldName(Lobby lobby)
        {
            return lobby.GetLobbyData(MyMultiplayer.WorldNameTag);
        }

        public static ulong GetLobbyWorldSize(Lobby lobby)
        {
            var s = lobby.GetLobbyData(MyMultiplayer.WorldSizeTag);
            if (!string.IsNullOrEmpty(s))
                return Convert.ToUInt64(s);

            return 0;
        }

        public static string GetLobbyHostName(Lobby lobby)
        {
            return lobby.GetLobbyData(MyMultiplayer.HostNameTag);
        }

        public static int GetLobbyAppVersion(Lobby lobby)
        {
            int result;
            return int.TryParse(lobby.GetLobbyData(MyMultiplayer.AppVersionTag), out result) ? result : 0;
        }

        public static string GetDataHash(Lobby lobby)
        {
            return lobby.GetLobbyData(MyMultiplayer.DataHashTag);
        }

        public static bool HasSameData(Lobby lobby)
        {
            string remoteHash = GetDataHash(lobby);

            // If the data hash is not set, the server does not want to check the data consistency
            if (remoteHash == "") return true;

            if (remoteHash == MyDataIntegrityChecker.GetHashBase64()) return true;

            return false;
        }

        public static int GetLobbyModCount(Lobby lobby)
        {
            return GetLobbyInt(MyMultiplayer.ModCountTag, lobby, 0);
        }

        public static List<MyObjectBuilder_Checkpoint.ModItem> GetLobbyMods(Lobby lobby)
        {
            var modsCount = GetLobbyModCount(lobby);
            var mods = new List<MyObjectBuilder_Checkpoint.ModItem>(modsCount);
            for (int i = 0; i < modsCount; ++i)
            {
                string modInfo = lobby.GetLobbyData(MyMultiplayer.ModItemTag + i);

                var index = modInfo.IndexOf("_");
                if (index != -1)
                {
                    ulong publishedFileId = 0;
                    ulong.TryParse(modInfo.Substring(0, index), out publishedFileId);
                    var name = modInfo.Substring(index + 1);
                    mods.Add(new MyObjectBuilder_Checkpoint.ModItem(name, publishedFileId, name));
                }
                else
                {
                    MySandboxGame.Log.WriteLineAndConsole(string.Format("Failed to parse mod details from LobbyData. '{0}'", modInfo));
                }

            }
            return mods;
        }

        public static int GetLobbyViewDistance(Lobby lobby)
        {
            return GetLobbyInt(MyMultiplayer.ViewDistanceTag, lobby, 20000);
        }

        public static bool GetLobbyScenario(Lobby lobby)
        {
            return GetLobbyBool(MyMultiplayer.ScenarioTag, lobby, false);
        }

        public static string GetLobbyScenarioBriefing(Lobby lobby)
        {
            return lobby.GetLobbyData(MyMultiplayer.ScenarioBriefingTag);
        }       

        public override string GetMemberName(ulong steamUserID)
        {
            return MySteam.API.Friends.GetPersonaName(steamUserID);
        }

        protected override void OnClientKick(ref MyControlKickClientMsg data, ulong kicked)
        {
            RaiseClientKicked(data.KickedClient);

            if (data.KickedClient == Sync.MyId)
            {
                MySessionLoader.UnloadAndExitToMenu();
                MyGuiSandbox.AddScreen(MyGuiSandbox.CreateMessageBox(
                    messageCaption: MyTexts.Get(MyCommonTexts.MessageBoxCaptionKicked),
                    messageText: MyTexts.Get(MyCommonTexts.MessageBoxTextYouHaveBeenKicked)));
            }
            else
            {
                AddKickedClient(data.KickedClient);
                RaiseClientLeft(data.KickedClient, ChatMemberStateChangeEnum.Kicked);
            }
        }

        protected override void OnClientBan(ref MyControlBanClientMsg data, ulong kicked)
        {
            System.Diagnostics.Debug.Fail("Ban is not supported in lobbies");
        }

        public override void OnAllMembersData(ref AllMembersDataMsg msg)
        {
            if (Sync.IsServer)
            {
                Debug.Fail("Members data cannot be sent to server");
                return;
            }

            ProcessAllMembersData(ref msg);
        }
    }
}
