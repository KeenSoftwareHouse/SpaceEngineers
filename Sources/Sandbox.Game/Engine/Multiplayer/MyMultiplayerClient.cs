#region Using

using Sandbox.Common;

using Sandbox.Common.ObjectBuilders;
using Sandbox.Engine.Networking;
using Sandbox.Engine.Utils;
using Sandbox.Game;
using Sandbox.Game.Gui;
using Sandbox.Game.Localization;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.World;
using Sandbox.Graphics.GUI;
using SteamSDK;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

using VRage;
using VRage.Utils;
using VRage.Trace;
using VRage.Utils;
using VRage.Library.Utils;


#endregion

namespace Sandbox.Engine.Multiplayer
{
    class MyMultiplayerClient : MyMultiplayerBase
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

        List<ulong> m_members = new List<ulong>();
        MemberCollection m_membersCollection;

        Dictionary<ulong, MyConnectedClientData> m_memberData = new Dictionary<ulong, MyConnectedClientData>();

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

        private int m_viewDistance;
        public override int ViewDistance
        {
            get { return m_viewDistance; }
            set { m_viewDistance = value; }
        }

        public override bool Battle
        {
            get;
            set;
        }

        public override bool BattleStarted
        {
            get;
            set;
        }

        public override int BattleFaction1MaxBlueprintPoints
        {
            get;
            set;
        }

        public override int BattleFaction2MaxBlueprintPoints
        {
            get;
            set;
        }

        public override int BattleFaction1BlueprintPoints
        {
            get;
            set;
        }

        public override int BattleFaction2BlueprintPoints
        {
            get;
            set;
        }

        public override int BattleMapAttackerSlotsCount
        {
            get;
            set;
        }

        public override long BattleFaction1Id
        {
            get;
            set;
        }

        public override long BattleFaction2Id
        {
            get;
            set;
        }

        public override int BattleFaction1Slot
        {
            get;
            set;
        }

        public override int BattleFaction2Slot
        {
            get;
            set;
        }

        public override bool BattleFaction1Ready
        {
            get;
            set;
        }

        public override bool BattleFaction2Ready
        {
            get;
            set;
        }

        public override int BattleTimeLimit
        {
            get;
            set;
        }


        public GameServerItem Server { get; private set; }
        #endregion

        internal MyMultiplayerClient(GameServerItem server, MySyncLayer syncLayer)
            : base(syncLayer)
        {
            m_membersCollection = new MemberCollection(m_members);

            Server = server;

            ServerId = server.SteamID;

            SyncLayer.TransportLayer.IsBuffering = true;

            SyncLayer.RegisterClientEvents(this);

            //MySyncLayer.RegisterMessage<ChatMsg>(OnChatMessage, MyMessagePermissions.Any, MyTransportMessageEnum.Request);
            //MySyncLayer.RegisterMessage<SendServerDataMsg>(OnServerData, MyMessagePermissions.Any, MyTransportMessageEnum.Request);
            //MySyncLayer.RegisterMessage<ConnectedPlayerDataMsg>(OnPlayerConnected, MyMessagePermissions.Any, MyTransportMessageEnum.Request);

            RegisterControlMessage<ChatMsg>(MyControlMessageEnum.Chat, OnChatMessage);
            RegisterControlMessage<ServerDataMsg>(MyControlMessageEnum.ServerData, OnServerData);
            RegisterControlMessage<JoinResultMsg>(MyControlMessageEnum.JoinResult, OnUserJoined);

            SyncLayer.RegisterMessageImmediate<ConnectedClientDataMsg>(this.OnConnectedClient, MyMessagePermissions.Any);

            ClientJoined += MyMultiplayerClient_ClientJoined;
            ClientLeft += MyMultiplayerClient_ClientLeft;
            HostLeft += MyMultiplayerClient_HostLeft;

            Peer2Peer.ConnectionFailed += Peer2Peer_ConnectionFailed;
            Peer2Peer.SessionRequest += Peer2Peer_SessionRequest;
        }

        void MyMultiplayerClient_HostLeft()
        {
            m_clientJoined = false;
            CloseClient();

            MyGuiScreenMainMenu.UnloadAndExitToMenu();
            MyGuiSandbox.AddScreen(MyGuiSandbox.CreateMessageBox(
                messageCaption: MyTexts.Get(MySpaceTexts.MessageBoxCaptionError),
                messageText: MyTexts.Get(MySpaceTexts.MultiplayerErrorServerHasLeft)));
        }

        void Peer2Peer_SessionRequest(ulong remoteUserId)
        {
            if (IsClientKickedOrBanned(remoteUserId)) return;
            
            Peer2Peer.AcceptSession(remoteUserId);
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

                if (MySandboxGame.IsGameReady && MySteam.UserId != user)
                {
                    var clientLeft = new MyHudNotification(MySpaceTexts.NotificationClientDisconnected, 5000, level: MyNotificationLevel.Important);
                    clientLeft.SetTextFormatArguments(MySteam.API.Friends.GetPersonaName(user));
                    MyHud.Notifications.Add(clientLeft);
                }

                Peer2Peer.CloseSession(user);
            }

            m_memberData.Remove(user);
        }                             

        void MyMultiplayerClient_ClientJoined(ulong user)
        {
            Debug.Assert(!m_members.Contains(user));
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
            return m_appVersion == Sandbox.Common.MyFinalBuildConstants.APP_VERSION;
        }

        public override MyDownloadWorldResult DownloadWorld()
        {
            MyTrace.Send(TraceWindow.Multiplayer, "World request sent");
            MyDownloadWorldResult ret = new MyDownloadWorldResult(MyMultiplayer.WorldDownloadChannel, ServerId, this);

            MyControlWorldRequestMsg msg = new MyControlWorldRequestMsg();
            SendControlMessage(ServerId, ref msg);

            return ret;
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

        public override void Tick()
        {
            base.Tick();
        }

        public override void Dispose()
        {
            CloseClient();

            base.Dispose();
        }

        private void CloseClient()
        {
            MyTrace.Send(TraceWindow.Multiplayer, "Multiplayer client closed");

            if (m_clientJoined)
            {
                MyControlDisconnectedMsg msg = new MyControlDisconnectedMsg();
                msg.Client = MySteam.UserId;

                SendControlMessage(ServerId, ref msg);
            }
            OnJoin = null;

            //TODO: Any better way? P2P needs to be closed from both sides. If closed right after Send, message 
            //can stay not sent.
            Thread.Sleep(200);

            //WARN: If closed here, previous control message probably not come
            Peer2Peer.CloseSession(ServerId);

            CloseMemberSessions();

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

        void OnChatMessage(ref ChatMsg msg, ulong sender)
        {
            RaiseChatMessageReceived(sender, msg.Text, ChatEntryTypeEnum.ChatMsg);
        }

        public override void SendChatMessage(string text)
        {
            ChatMsg msg = new ChatMsg();
            msg.Text = text;

            SendControlMessageToAllAndSelf(ref msg);
        }

        void OnServerData(ref ServerDataMsg msg, ulong sender)
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

        void OnUserJoined(ref JoinResultMsg msg, ulong sender)
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
                MyGuiScreenMainMenu.UnloadAndExitToMenu();
                Dispose();

                ulong groupId = Server.GetGameTagByPrefixUlong("groupId");
                string groupName = MySteam.API.Friends.GetClanName(groupId);

                var messageBox = MyGuiSandbox.CreateMessageBox(
                    messageCaption: MyTexts.Get(MySpaceTexts.MessageBoxCaptionError),
                    messageText: new StringBuilder(string.Format(
                        MyTexts.GetString(MySpaceTexts.MultiplayerErrorNotInGroup), groupName)),
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
                MyGuiScreenMainMenu.UnloadAndExitToMenu();
                Dispose();

                ulong admin = msg.Admin;

                if (admin != 0)
                {
                    var messageBox = MyGuiSandbox.CreateMessageBox(
                        messageCaption: MyTexts.Get(MySpaceTexts.MessageBoxCaptionError),
                        messageText: MyTexts.Get(MySpaceTexts.MultiplayerErrorBannedByAdminsWithDialog),
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
                  messageCaption: MyTexts.Get(MySpaceTexts.MessageBoxCaptionError),
                  messageText: MyTexts.Get(MySpaceTexts.MultiplayerErrorBannedByAdmins)));
                }
            }
            else
            {
                MyStringId resultText = MySpaceTexts.MultiplayerErrorConnectionFailed;

                switch (msg.JoinResult)
                {
                    case JoinResult.AlreadyJoined:
                        resultText = MySpaceTexts.MultiplayerErrorAlreadyJoined;
                        break;
                    case JoinResult.ServerFull:
                        resultText = MySpaceTexts.MultiplayerErrorServerFull;
                        break;
                    case JoinResult.SteamServersOffline:
                        resultText = MySpaceTexts.MultiplayerErrorSteamServersOffline;
                        break;
                    case JoinResult.TicketInvalid:
                        resultText = MySpaceTexts.MultiplayerErrorTicketInvalid;
                        break;
                    case JoinResult.GroupIdInvalid:
                        resultText = MySpaceTexts.MultiplayerErrorGroupIdInvalid;
                        break;

                    case JoinResult.TicketCanceled:
                        resultText = MySpaceTexts.MultiplayerErrorTicketCanceled;
                        break;                        
                    case JoinResult.TicketAlreadyUsed:
                        resultText = MySpaceTexts.MultiplayerErrorTicketAlreadyUsed;
                        break;
                    case JoinResult.LoggedInElseWhere:
                        resultText = MySpaceTexts.MultiplayerErrorLoggedInElseWhere;
                        break;
                    case JoinResult.NoLicenseOrExpired:
                        resultText = MySpaceTexts.MultiplayerErrorNoLicenseOrExpired;
                        break;
                    case JoinResult.UserNotConnected:
                        resultText = MySpaceTexts.MultiplayerErrorUserNotConnected;
                        break;
                    case JoinResult.VACBanned:
                        resultText = MySpaceTexts.MultiplayerErrorVACBanned;
                        break;
                    case JoinResult.VACCheckTimedOut:
                        resultText = MySpaceTexts.MultiplayerErrorVACCheckTimedOut;
                        break;
        
                    default:
                        System.Diagnostics.Debug.Fail("Unknown JoinResult");
                        break;
                }

                Dispose();
                MyGuiScreenMainMenu.UnloadAndExitToMenu();
                MyGuiSandbox.AddScreen(MyGuiSandbox.CreateMessageBox(
                    messageCaption: MyTexts.Get(MySpaceTexts.MessageBoxCaptionError),
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

        public void OnConnectedClient(ref ConnectedClientDataMsg msg, MyNetworkClient sender)
        {
            MySandboxGame.Log.WriteLineAndConsole("Client connected: " + msg.Name + " (" + msg.SteamID.ToString() + ")");
            MyTrace.Send(TraceWindow.Multiplayer, "Client connected");

            if (MySandboxGame.IsGameReady && msg.SteamID != ServerId && MySteam.UserId != msg.SteamID && msg.Join)
            {
                var clientConnected = new MyHudNotification(MySpaceTexts.NotificationClientConnected, 5000, level: MyNotificationLevel.Important);
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
            msg.SteamID = MySteam.UserId;
            msg.Name = clientName;
            msg.Join = true;

            var buffer = new byte[1024];
            uint length = 0;
            if (!MySteam.API.GetAuthSessionTicket(buffer, ref length))
            {
                MyGuiScreenMainMenu.UnloadAndExitToMenu();
                MyGuiSandbox.AddScreen(MyGuiSandbox.CreateMessageBox(
                    messageCaption: MyTexts.Get(MySpaceTexts.MessageBoxCaptionError),
                    messageText: MyTexts.Get(MySpaceTexts.MultiplayerErrorConnectionFailed)));
                return;
            }

            msg.Token = new byte[length];
            Array.Copy(buffer, msg.Token, length);

            SyncLayer.SendMessageToServer(ref msg);
        }

        protected override void OnClientKick(ref MyControlKickClientMsg data, ulong sender)
        {
            if (data.KickedClient == MySteam.UserId)
            {
                // We don't want to send disconnect message because the clients will disconnect the client automatically upon receiving on the MyControlKickClientMsg
                m_clientJoined = false;

                Dispose();
                MyGuiScreenMainMenu.ReturnToMainMenu();
                MyGuiSandbox.AddScreen(MyGuiSandbox.CreateMessageBox(
                    messageCaption: MyTexts.Get(MySpaceTexts.MessageBoxCaptionKicked),
                    messageText: MyTexts.Get(MySpaceTexts.MessageBoxTextYouHaveBeenKicked)));
            }
            else
            {
                AddKickedClient(data.KickedClient);
                RaiseClientLeft(data.KickedClient, ChatMemberStateChangeEnum.Kicked);
            }
        }

        protected override void OnClientBan(ref MyControlBanClientMsg data, ulong sender)
        {
            if (data.BannedClient == MySteam.UserId && data.Banned == true)
            {
                // We don't want to send disconnect message because the clients will disconnect the client automatically upon receiving on the MyControlBanClientMsg
                m_clientJoined = false;

                Dispose();
                MyGuiScreenMainMenu.ReturnToMainMenu();
                MyGuiSandbox.AddScreen(MyGuiSandbox.CreateMessageBox(
                    messageCaption: MyTexts.Get(MySpaceTexts.MessageBoxCaptionKicked),
                    messageText: MyTexts.Get(MySpaceTexts.MessageBoxTextYouHaveBeenBanned)));
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

        protected override void OnPing(ref MyControlPingMsg data, ulong sender)
        {
            MyNetworkStats.Static.OnPingSuccess();
        }
    }
}
