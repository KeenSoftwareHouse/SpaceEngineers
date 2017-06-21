#region Using

using Sandbox.Engine.Networking;
using Sandbox.Engine.Utils;
using Sandbox.Game.Gui;
using Sandbox.Game.Multiplayer;
using Sandbox.Graphics.GUI;
using SteamSDK;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;

using VRage;
using VRage.Utils;
using VRage.Trace;
using VRage.Library.Utils;
using VRage.Network;
using Sandbox.Game.Entities;
using VRage.Game;
using Sandbox.Game.World;

#endregion

namespace Sandbox.Engine.Multiplayer
{
    sealed class MyMultiplayerClient : MyMultiplayerClientBase
    {
        #region Fields

        string m_worldName;
        MyGameModeEnum m_gameMode;
        float m_inventoryMultiplier;
        float m_assemblerMultiplier;
        float m_refineryMultiplier;
        float m_welderMultiplier;
        float m_grinderMultiplier;
        string m_hostName;
        ulong m_worldSize;
        int m_appVersion;
        int m_membersLimit;
        string m_dataHash;

        readonly List<ulong> m_members = new List<ulong>();
        readonly MemberCollection m_membersCollection;

        readonly Dictionary<ulong, MyConnectedClientData> m_memberData = new Dictionary<ulong, MyConnectedClientData>();

        public Action OnJoin;
        private bool m_clientJoined = false;

        #endregion

        #region Properties

        public override bool IsServer { get { return false; } }

        public override string WorldName
        {
            get { return m_worldName; }
            set { m_worldName = value; }
        }

        public override MyGameModeEnum GameMode
        {
            get { return m_gameMode; }
            set { m_gameMode = value; }
        }

        public override float InventoryMultiplier
        {
            get { return m_inventoryMultiplier; }
            set { m_inventoryMultiplier = value; }
        }

        public override float AssemblerMultiplier
        {
            get { return m_assemblerMultiplier; }
            set { m_assemblerMultiplier = value; }
        }

        public override float RefineryMultiplier
        {
            get { return m_refineryMultiplier; }
            set { m_refineryMultiplier = value; }
        }

        public override float WelderMultiplier
        {
            get { return m_welderMultiplier; }
            set { m_welderMultiplier = value; }
        }

        public override float GrinderMultiplier
        {
            get { return m_grinderMultiplier; }
            set { m_grinderMultiplier = value; }
        }

        public override string HostName
        {
            get { return m_hostName; }
            set { m_hostName = value; }
        }

        public override ulong WorldSize
        {
            get { return m_worldSize; }
            set { m_worldSize = value; }
        }

        public override int AppVersion
        {
            get { return m_appVersion; }
            set { m_appVersion = value; }
        }

        public override string DataHash
        {
            get { return m_dataHash; }
            set { m_dataHash = value; }
        }

        public override int MaxPlayers
        {
            get { return 65536; }
        }

        public override int ModCount
        {
            get;
            protected set;
        }

        private List<MyObjectBuilder_Checkpoint.ModItem> m_mods = new List<MyObjectBuilder_Checkpoint.ModItem>();
        public override List<MyObjectBuilder_Checkpoint.ModItem> Mods
        {
            get { return m_mods; }
            set
            {
                m_mods = value;
                ModCount = m_mods.Count;
            }
        }

        public override int ViewDistance { get; set; }

        public override bool Scenario
        {
            get;
            set;
        }

        public override string ScenarioBriefing
        {
            get;
            set;
        }

        public override DateTime ScenarioStartTime
        {
            get;
            set;
        }

        public GameServerItem Server { get; private set; }

        #endregion

        public new MyReplicationClient ReplicationLayer { get { return (MyReplicationClient)base.ReplicationLayer; } }

        internal MyMultiplayerClient(GameServerItem server, MySyncLayer syncLayer)
            : base(syncLayer)
        {
            m_membersCollection = new MemberCollection(m_members);

            Server = server;

            ServerId = server.SteamID;

            SyncLayer.TransportLayer.IsBuffering = true;
            SyncLayer.RegisterClientEvents(this);

            SetReplicationLayer(new MyReplicationClient(this, CreateClientState(), MyEngineConstants.UPDATE_STEP_SIZE_IN_MILLISECONDS));
            ReplicationLayer.UseSmoothPing = MyFakes.MULTIPLAYER_SMOOTH_PING;
            ReplicationLayer.UseSmoothCorrection = MyFakes.MULTIPLAYER_SMOOTH_TIMESTAMP_CORRECTION;
            syncLayer.TransportLayer.Register(MyMessageId.SERVER_DATA, ReplicationLayer.ProcessServerData);
            syncLayer.TransportLayer.Register(MyMessageId.REPLICATION_CREATE, OnReplicationCreate);
            syncLayer.TransportLayer.Register(MyMessageId.REPLICATION_DESTROY, OnReplicationDestroy);
            syncLayer.TransportLayer.Register(MyMessageId.SERVER_STATE_SYNC, ReplicationLayer.ProcessStateSync);
            syncLayer.TransportLayer.Register(MyMessageId.RPC, ReplicationLayer.ProcessEvent);
            syncLayer.TransportLayer.Register(MyMessageId.REPLICATION_STREAM_BEGIN, OnReplicationBeginCreate);
            syncLayer.TransportLayer.Register(MyMessageId.JOIN_RESULT, OnJoinResult);
            syncLayer.TransportLayer.Register(MyMessageId.WORLD_DATA, OnWorldData);
            syncLayer.TransportLayer.Register(MyMessageId.CLIENT_CONNNECTED,OnClientConnected);
 
            ClientJoined += MyMultiplayerClient_ClientJoined;
            ClientLeft += MyMultiplayerClient_ClientLeft;
            HostLeft += MyMultiplayerClient_HostLeft;

            Peer2Peer.ConnectionFailed += Peer2Peer_ConnectionFailed;
            Peer2Peer.SessionRequest += Peer2Peer_SessionRequest;            
        }

        void OnReplicationBeginCreate(MyPacket packet)
        {
            while (MyEntities.HasEntitiesToDelete())
            {
                MyEntities.DeleteRememberedEntities();
            }
            ReplicationLayer.ProcessReplicationCreateBegin(packet);
        }

        void OnReplicationCreate(MyPacket packet)
        {
            while (MyEntities.HasEntitiesToDelete())
            {
                MyEntities.DeleteRememberedEntities();
            }
            ReplicationLayer.ProcessReplicationCreate(packet);
        }

        void OnReplicationDestroy(MyPacket packet)
        {
            ReplicationLayer.ProcessReplicationDestroy(packet);
        }

        void OnWorldData(MyPacket packet)
        {
            ServerDataMsg msg = ReplicationLayer.OnWorldData(packet);
            OnServerData(ref msg);
        }

        void OnJoinResult(MyPacket packet)
        {
           JoinResultMsg msg = ReplicationLayer.OnJoinResult(packet);

           OnUserJoined(ref msg);
        }

        void OnClientConnected(MyPacket packet)
        {
            ConnectedClientDataMsg msg = ReplicationLayer.OnClientConnected(packet);
            OnConnectedClient(ref msg);
        }

        

        public override void Dispose()
        {
            CloseClient();

            base.Dispose();
        }

        void MyMultiplayerClient_HostLeft()
        {
            m_clientJoined = false;
            CloseSession();

            MySessionLoader.UnloadAndExitToMenu();
            MyGuiSandbox.AddScreen(MyGuiSandbox.CreateMessageBox(
                messageCaption: MyTexts.Get(MyCommonTexts.MessageBoxCaptionError),
                messageText: MyTexts.Get(MyCommonTexts.MultiplayerErrorServerHasLeft)));
        }

        void Peer2Peer_SessionRequest(ulong remoteUserId)
        {
            if (IsClientKickedOrBanned(remoteUserId)) return;

            if (IsServer || remoteUserId == ServerId)
            {
                Peer2Peer.AcceptSession(remoteUserId);
            }
        }

        void MyMultiplayerClient_ClientLeft(ulong user, ChatMemberStateChangeEnum stateChange)
        {
            if (user == ServerId)
            {
                RaiseHostLeft();
                return;
            }

            if (m_members.Contains(user))
            {
                m_members.Remove(user);

                MySandboxGame.Log.WriteLineAndConsole("Player disconnected: " + MySteam.API.Friends.GetPersonaName(user) + " (" + user + ")");

                MyTrace.Send(TraceWindow.Multiplayer, "Player disconnected: " + stateChange.ToString());

                if (MySandboxGame.IsGameReady && Sync.MyId != user)
                {
                    var clientLeft = new MyHudNotification(MyCommonTexts.NotificationClientDisconnected, 5000, level: MyNotificationLevel.Important);
                    clientLeft.SetTextFormatArguments(MySteam.API.Friends.GetPersonaName(user));
                    MyHud.Notifications.Add(clientLeft);
                }

                //Peer2Peer.CloseSession(user);
            }

            m_memberData.Remove(user);
        }

        void MyMultiplayerClient_ClientJoined(ulong user)
        {
            if (m_members.Contains(user)) return;

            m_members.Add(user);
        }

        void Peer2Peer_ConnectionFailed(ulong remoteUserId, P2PSessionErrorEnum error)
        {
            MyTrace.Send(TraceWindow.Multiplayer, "Peer2Peer_ConnectionFailed (" + remoteUserId.ToString() + ")");

            if (remoteUserId == ServerId)
            {
                RaiseHostLeft();
            }
        }

        public override bool IsCorrectVersion()
        {
            return m_appVersion == MyFinalBuildConstants.APP_VERSION;
        }

        public override MyDownloadWorldResult DownloadWorld()
        {
            MyTrace.Send(TraceWindow.Multiplayer, "World request sent");
            MyDownloadWorldResult ret = new MyDownloadWorldResult(MyMultiplayer.WorldDownloadChannel, ServerId, this);

            MyControlWorldRequestMsg msg = new MyControlWorldRequestMsg();
            SendControlMessage(ServerId, ref msg);

            return ret;
        }

        public override void DownloadProfiler(int index)
        {
            MyDownloadProfilerResult ret = new MyDownloadProfilerResult(MyMultiplayer.ProfilerDownloadChannel, ServerId, this);

            MyControlProfilerMsg msg = new MyControlProfilerMsg() { index = index };
            SendControlMessage(ServerId, ref msg);
        }

        public override void DisconnectClient(ulong userId)
        {
            CloseClient();
        }

        public override void KickClient(ulong client)
        {
            MyControlKickClientMsg msg = new MyControlKickClientMsg();
            msg.KickedClient = client;
            SendControlMessage(ServerId, ref msg);
        }

        public override void BanClient(ulong client, bool ban)
        {
            MyControlBanClientMsg msg = new MyControlBanClientMsg();
            msg.BannedClient = client;
            msg.Banned = ban;
            SendControlMessage(ServerId, ref msg);
        }

        private void CloseClient()
        {
            MyTrace.Send(TraceWindow.Multiplayer, "Multiplayer client closed");

            MyControlDisconnectedMsg msg = new MyControlDisconnectedMsg();
            msg.Client = Sync.MyId;

            SendControlMessage(ServerId, ref msg);
            OnJoin = null;

            //TODO: Any better way? P2P needs to be closed from both sides. If closed right after Send, message 
            //can stay not sent.
            Thread.Sleep(200);

            CloseSession();
        }

        private void CloseSession()
        {
            OnJoin = null;

            //WARN: If closed here, previous control message probably not come
            Peer2Peer.CloseSession(ServerId);

            Peer2Peer.ConnectionFailed -= Peer2Peer_ConnectionFailed;
            Peer2Peer.SessionRequest -= Peer2Peer_SessionRequest;
        }

        public override MemberCollection Members
        {
            get { return m_membersCollection; }
        }

        public override int MemberCount
        {
            get { return m_members.Count; }
        }

        public override ulong GetMemberByIndex(int memberIndex)
        {
            return m_members[memberIndex];
        }

        public override ulong LobbyId
        {
            get { return 0; }
        }

        public override int MemberLimit
        {
            get { return m_membersLimit; }
            set { m_membersLimit = value; }
        }

        public override bool IsAdmin(ulong steamID)
        {
            if (m_memberData.ContainsKey(steamID))
                return m_memberData[steamID].IsAdmin;
            return false;
        }

        public override ulong GetOwner()
        {
            return ServerId;
        }

        public override void SetOwner(ulong owner)
        {
            System.Diagnostics.Debug.Fail("Cannot change owner");
        }

        public override LobbyTypeEnum GetLobbyType()
        {
            return LobbyTypeEnum.Public;
        }

        public override void SetLobbyType(LobbyTypeEnum type)
        {
        }

        public override void SetMemberLimit(int limit)
        {
            m_membersLimit = limit;
        }

        protected override void OnChatMessage(ref ChatMsg msg)
        {
            bool debugCommands = !MyFinalBuildConstants.IS_OFFICIAL && MyFinalBuildConstants.IS_DEBUG;

            if (m_memberData.ContainsKey(msg.Author))
            {
                if (m_memberData[msg.Author].IsAdmin || debugCommands)
                {
                    MyClientDebugCommands.Process(msg.Text, msg.Author);
                }
            }

            RaiseChatMessageReceived(msg.Author, msg.Text, ChatEntryTypeEnum.ChatMsg);
        }

        public override void SendChatMessage(string text)
        {
            ChatMsg msg = new ChatMsg();
            msg.Text = text;
            msg.Author = Sync.MyId;

            OnChatMessage(ref msg);

            SendChatMessage(ref msg);    
        }

        void OnServerData(ref ServerDataMsg msg)
        {
            m_worldName = msg.WorldName;
            m_gameMode = msg.GameMode;
            m_inventoryMultiplier = msg.InventoryMultiplier;
            m_assemblerMultiplier = msg.AssemblerMultiplier;
            m_refineryMultiplier = msg.RefineryMultiplier;
            m_welderMultiplier = msg.WelderMultiplier;
            m_grinderMultiplier = msg.GrinderMultiplier;
            m_hostName = msg.HostName;
            m_worldSize = msg.WorldSize;
            m_appVersion = msg.AppVersion;
            m_membersLimit = msg.MembersLimit;
            m_dataHash = msg.DataHash;
        }

        void OnUserJoined(ref JoinResultMsg msg)
        {
            if (msg.JoinResult == JoinResult.OK)
            {
                if (OnJoin != null)
                {
                    OnJoin();
                    OnJoin = null;
                    m_clientJoined = true;
                }
            }
            else if (msg.JoinResult == JoinResult.NotInGroup)
            {
                MySessionLoader.UnloadAndExitToMenu();
                Dispose();

                ulong groupId = Server.GetGameTagByPrefixUlong("groupId");
                string groupName = MySteam.API.Friends.GetClanName(groupId);

                var messageBox = MyGuiSandbox.CreateMessageBox(
                    messageCaption: MyTexts.Get(MyCommonTexts.MessageBoxCaptionError),
                    messageText: new StringBuilder(string.Format(
                        MyTexts.GetString(MyCommonTexts.MultiplayerErrorNotInGroup), groupName)),
                    buttonType: MyMessageBoxButtonsType.YES_NO);
                messageBox.ResultCallback = delegate(MyGuiScreenMessageBox.ResultEnum result)
                {
                    if (result == MyGuiScreenMessageBox.ResultEnum.YES)
                    {
                        MySteam.API.OpenOverlayUser(groupId);
                    };
                };
                MyGuiSandbox.AddScreen(messageBox);
            }
            else if (msg.JoinResult == JoinResult.BannedByAdmins)
            {
                MySessionLoader.UnloadAndExitToMenu();
                Dispose();

                ulong admin = msg.Admin;

                if (admin != 0)
                {
                    var messageBox = MyGuiSandbox.CreateMessageBox(
                        messageCaption: MyTexts.Get(MyCommonTexts.MessageBoxCaptionError),
                        messageText: MyTexts.Get(MyCommonTexts.MultiplayerErrorBannedByAdminsWithDialog),
                        buttonType: MyMessageBoxButtonsType.YES_NO);
                    messageBox.ResultCallback = delegate(MyGuiScreenMessageBox.ResultEnum result)
                   {
                       if (result == MyGuiScreenMessageBox.ResultEnum.YES)
                       {
                           MySteam.API.OpenOverlayUser(admin);
                       };
                   };
                    MyGuiSandbox.AddScreen(messageBox);
                }
                else
                {
                    MyGuiSandbox.AddScreen(MyGuiSandbox.CreateMessageBox(
                  messageCaption: MyTexts.Get(MyCommonTexts.MessageBoxCaptionError),
                  messageText: MyTexts.Get(MyCommonTexts.MultiplayerErrorBannedByAdmins)));
                }
            }
            else
            {
                MyStringId resultText = MyCommonTexts.MultiplayerErrorConnectionFailed;

                switch (msg.JoinResult)
                {
                    case JoinResult.AlreadyJoined:
                        resultText = MyCommonTexts.MultiplayerErrorAlreadyJoined;
                        break;
                    case JoinResult.ServerFull:
                        resultText = MyCommonTexts.MultiplayerErrorServerFull;
                        break;
                    case JoinResult.SteamServersOffline:
                        resultText = MyCommonTexts.MultiplayerErrorSteamServersOffline;
                        break;
                    case JoinResult.TicketInvalid:
                        resultText = MyCommonTexts.MultiplayerErrorTicketInvalid;
                        break;
                    case JoinResult.GroupIdInvalid:
                        resultText = MyCommonTexts.MultiplayerErrorGroupIdInvalid;
                        break;

                    case JoinResult.TicketCanceled:
                        resultText = MyCommonTexts.MultiplayerErrorTicketCanceled;
                        break;
                    case JoinResult.TicketAlreadyUsed:
                        resultText = MyCommonTexts.MultiplayerErrorTicketAlreadyUsed;
                        break;
                    case JoinResult.LoggedInElseWhere:
                        resultText = MyCommonTexts.MultiplayerErrorLoggedInElseWhere;
                        break;
                    case JoinResult.NoLicenseOrExpired:
                        resultText = MyCommonTexts.MultiplayerErrorNoLicenseOrExpired;
                        break;
                    case JoinResult.UserNotConnected:
                        resultText = MyCommonTexts.MultiplayerErrorUserNotConnected;
                        break;
                    case JoinResult.VACBanned:
                        resultText = MyCommonTexts.MultiplayerErrorVACBanned;
                        break;
                    case JoinResult.VACCheckTimedOut:
                        resultText = MyCommonTexts.MultiplayerErrorVACCheckTimedOut;
                        break;

                    default:
                        System.Diagnostics.Debug.Fail("Unknown JoinResult");
                        break;
                }

                Dispose();
                MySessionLoader.UnloadAndExitToMenu();
                MyGuiSandbox.AddScreen(MyGuiSandbox.CreateMessageBox(
                    messageCaption: MyTexts.Get(MyCommonTexts.MessageBoxCaptionError),
                    messageText: MyTexts.Get(resultText)));
                return;
            }
        }

        public void LoadMembersFromWorld(List<MyObjectBuilder_Client> clients)
        {
            if (clients == null) return;
            foreach (var client in clients)
            {
                Debug.Assert(!m_members.Contains(client.SteamId), "Member already present!");
                Debug.Assert(!m_memberData.ContainsKey(client.SteamId), "Member already present!");

                m_memberData.Add(client.SteamId, new MyConnectedClientData() { Name = client.Name, IsAdmin = client.IsAdmin });

                RaiseClientJoined(client.SteamId);
            }
        }

        public override string GetMemberName(ulong steamUserID)
        {
            MyConnectedClientData clientData;
            m_memberData.TryGetValue(steamUserID, out clientData);
            return clientData.Name;
        }

        private void OnConnectedClient(ref ConnectedClientDataMsg msg)
        {
            MySandboxGame.Log.WriteLineAndConsole("Client connected: " + msg.Name + " (" + msg.SteamID.ToString() + ")");
            MyTrace.Send(TraceWindow.Multiplayer, "Client connected");

            if (MySandboxGame.IsGameReady && msg.SteamID != ServerId &&  Sync.MyId != msg.SteamID && msg.Join)
            {
                var clientConnected = new MyHudNotification(MyCommonTexts.NotificationClientConnected, 5000, level: MyNotificationLevel.Important);
                clientConnected.SetTextFormatArguments(msg.Name);
                MyHud.Notifications.Add(clientConnected);
            }

            m_memberData[msg.SteamID] = new MyConnectedClientData()
                {
                    Name = msg.Name,
                    IsAdmin = msg.IsAdmin
                };

            RaiseClientJoined(msg.SteamID);
        }

        public void SendPlayerData(string clientName)
        {
            ConnectedClientDataMsg msg = new ConnectedClientDataMsg();
            msg.SteamID = Sync.MyId;
            msg.Name = clientName;
            msg.Join = true;

            var buffer = new byte[1024];
            uint length;
            uint ticketHandle; // TODO: Store handle and end auth session on end
            if (!MySteam.API.GetAuthSessionTicket(out ticketHandle, buffer, out length))
            {
                MySessionLoader.UnloadAndExitToMenu();
                MyGuiSandbox.AddScreen(MyGuiSandbox.CreateMessageBox(
                    messageCaption: MyTexts.Get(MyCommonTexts.MessageBoxCaptionError),
                    messageText: MyTexts.Get(MyCommonTexts.MultiplayerErrorConnectionFailed)));
                return;
            }

            msg.Token = new byte[length];
            Array.Copy(buffer, msg.Token, length);

            ReplicationLayer.SendClientConnected(ref msg);
        }

        protected override void OnClientKick(ref MyControlKickClientMsg data, ulong sender)
        {
            if (data.KickedClient == Sync.MyId)
            {
                // We don't want to send disconnect message because the clients will disconnect the client automatically upon receiving on the MyControlKickClientMsg
                m_clientJoined = false;

                Dispose();
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

        protected override void OnClientBan(ref MyControlBanClientMsg data, ulong sender)
        {
            if (data.BannedClient == Sync.MyId && data.Banned == true)
            {
                // We don't want to send disconnect message because the clients will disconnect the client automatically upon receiving on the MyControlBanClientMsg
                m_clientJoined = false;

                Dispose();
                MySessionLoader.UnloadAndExitToMenu();
                MyGuiSandbox.AddScreen(MyGuiSandbox.CreateMessageBox(
                    messageCaption: MyTexts.Get(MyCommonTexts.MessageBoxCaptionKicked),
                    messageText: MyTexts.Get(MyCommonTexts.MessageBoxTextYouHaveBeenBanned)));
            }
            else
            {
                if (data.Banned) AddBannedClient(data.BannedClient);
                else RemoveBannedClient(data.BannedClient);

                if (m_members.Contains(data.BannedClient) && data.Banned == true)
                {
                    RaiseClientLeft(data.BannedClient, ChatMemberStateChangeEnum.Banned);
                }
            }
        }

        public override void StartProcessingClientMessagesWithEmptyWorld()
        {
            // Add server client - needed for processing messages, before StartProcessingClientMessages
            if (!Sync.Clients.HasClient(ServerId))
                Sync.Clients.AddClient(ServerId);

            base.StartProcessingClientMessagesWithEmptyWorld();

            // Set local client before LoadDataComponents.
            if (Sync.Clients.LocalClient == null)
                Sync.Clients.SetLocalSteamId(Sync.MyId, createLocalClient: !Sync.Clients.HasClient(Sync.MyId));
        }

        public override void OnAllMembersData(ref AllMembersDataMsg msg)
        {
            // Setup members and clients
            if (msg.Clients != null)
            {
                foreach (var client in msg.Clients)
                {
                    if (!m_memberData.ContainsKey(client.SteamId))
                        m_memberData.Add(client.SteamId, new MyConnectedClientData() { Name = client.Name, IsAdmin = client.IsAdmin });

                    if (!m_members.Contains(client.SteamId))
                        m_members.Add(client.SteamId);

                    if (!Sync.Clients.HasClient(client.SteamId))
                        Sync.Clients.AddClient(client.SteamId);
                }
            }

            // Setup identities, players, factions
            ProcessAllMembersData(ref msg);
        }
    }
}
