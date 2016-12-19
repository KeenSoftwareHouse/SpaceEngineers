using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading;
using Sandbox.Engine.Networking;
using Sandbox.Engine.Utils;
using Sandbox.Game;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.World;
using SteamSDK;
using VRage.Game;
using VRage.Trace;
using VRage.Utils;
using VRage.Network;
using VRage.Library.Utils;

namespace Sandbox.Engine.Multiplayer
{
    #region Structs

    public static class MyDedicatedServerOverrides
    {
        public static int? MaxPlayers;
        public static IPAddress IpAddress;
        public static int? Port;
    }

    #endregion

    public abstract class MyDedicatedServerBase : MyMultiplayerServerBase
    {
        #region Fields

        protected string m_worldName;
        protected MyGameModeEnum m_gameMode;
        protected string m_hostName;
        protected ulong m_worldSize;
        protected int m_appVersion = MyFinalBuildConstants.APP_VERSION;
        protected int m_membersLimit;
        protected string m_dataHash;
        protected ulong m_groupId;

        protected List<ulong> m_members = new List<ulong>();
        private MemberCollection m_membersCollection;

        protected Dictionary<ulong, MyConnectedClientData> m_memberData = new Dictionary<ulong, MyConnectedClientData>();
        private Dictionary<ulong, MyConnectedClientData> m_pendingMembers = new Dictionary<ulong, MyConnectedClientData>();

        private HashSet<ulong> m_waitingForGroup = new HashSet<ulong>();

        #endregion

        #region Properties

        public override bool IsServer { get { return true; } }

        public bool ServerStarted { get; set; }
        public string ServerInitError { get; set; }

        public bool IsService { get; private set; }

        public override string WorldName
        {
            get { return m_worldName; }
            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    m_worldName = "noname";
                }
                else
                {
                    m_worldName = value;
                }
                SteamSDK.SteamServerAPI.Instance.GameServer.SetMapName(m_worldName);
            }
        }

        public override MyGameModeEnum GameMode
        {
            get { return m_gameMode; }
            set { m_gameMode = value; }
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
            get { return 1024; }
        }

        private int m_modCount;
        public override int ModCount
        {
            get { return m_modCount; }
            protected set
            {
                m_modCount = value;
                SteamSDK.SteamServerAPI.Instance.GameServer.SetKeyValue(MyMultiplayer.ModCountTag, m_modCount.ToString());
            }
        }

        private List<MyObjectBuilder_Checkpoint.ModItem> m_mods = new List<MyObjectBuilder_Checkpoint.ModItem>();
        public override List<MyObjectBuilder_Checkpoint.ModItem> Mods
        {
            get { return m_mods; }
            set
            {
                m_mods = value;
                ModCount = m_mods.Count;
                int i = ModCount - 1;
                foreach (var mod in m_mods)
                {
                    SteamSDK.SteamServerAPI.Instance.GameServer.SetKeyValue(MyMultiplayer.ModItemTag + i--, mod.FriendlyName);
                }
            }
        }

        private int m_viewDistance;
        public override int ViewDistance
        {
            get { return m_viewDistance; }
            set { m_viewDistance = value; }
        }

        #endregion

        public static readonly string SteamIDPrefix = "STEAM_";
        public static readonly ulong SteamIDMagicConstant = 76561197960265728ul;

        public static string ConvertSteamIDFrom64(ulong from)
        {
            from -= SteamIDMagicConstant;
            return new StringBuilder(SteamIDPrefix).Append("0:").Append(from % 2).Append(':').Append((ulong)(from / 2)).ToString();
        }

        public static ulong ConvertSteamIDTo64(string from)
        {
            var split = from.Replace(SteamIDPrefix, "").Split(':');
            if (split.Length != 3)
                return 0;
            return SteamIDMagicConstant + Convert.ToUInt64(split[1]) + Convert.ToUInt64(split[2]) * 2;
        }


        protected MyDedicatedServerBase(MySyncLayer syncLayer)
            : base(syncLayer, null)
        {
            syncLayer.TransportLayer.Register(MyMessageId.CLIENT_CONNNECTED, (p) => ClientConnected(p));
        }

        protected void Initialize(IPEndPoint serverEndpoint)
        {
            m_groupId = MySandboxGame.ConfigDedicated.GroupID;

            ServerStarted = false;

            HostName = "Dedicated server";

            m_membersCollection = new MemberCollection(m_members);
            SetMemberLimit(MaxPlayers);

            SteamSDK.Peer2Peer.SessionRequest += Peer2Peer_SessionRequest;
            SteamSDK.Peer2Peer.ConnectionFailed += Peer2Peer_ConnectionFailed;
            ClientLeft += MyDedicatedServer_ClientLeft;

            SteamSDK.SteamServerAPI.Instance.GameServer.ServersConnected += GameServer_ServersConnected;
            SteamSDK.SteamServerAPI.Instance.GameServer.ServersConnectFailure += GameServer_ServersConnectFailure;
            SteamSDK.SteamServerAPI.Instance.GameServer.ServersDisconnected += GameServer_ServersDisconnected;
            SteamSDK.SteamServerAPI.Instance.GameServer.PolicyResponse += GameServer_PolicyResponse;
            SteamSDK.SteamServerAPI.Instance.GameServer.ValidateAuthTicketResponse += GameServer_ValidateAuthTicketResponse;
            SteamSDK.SteamServerAPI.Instance.GameServer.UserGroupStatus += GameServer_UserGroupStatus;

            ServerStartResult startResult = SteamSDK.SteamServerAPI.Instance.GameServer.Start(
                serverEndpoint,
                (ushort)MySandboxGame.ConfigDedicated.SteamPort,
                SteamSDK.ServerMode.eServerModeAuthenticationAndSecure,
                MyFinalBuildConstants.APP_VERSION.ToString(),
                MyFakes.DEDICATED_SERVER_USE_SOCKET_SHARE);

            switch (startResult)
            {
                case ServerStartResult.PortAlreadyUsed:
                    ServerInitError = "Error starting Steam dedicated server: Server port " + (ushort)MySandboxGame.ConfigDedicated.ServerPort + " already in use";
                    MyLog.Default.WriteLineAndConsole(ServerInitError);
                    break;

                case ServerStartResult.UnknownError:
                    ServerInitError = "Error starting Steam dedicated server";
                    MyLog.Default.WriteLineAndConsole(ServerInitError);
                    break;
            }

            if (startResult != ServerStartResult.OK)
                return;

            // This has to be exact name of app like this to show in Server Browser
            SteamSDK.SteamServerAPI.Instance.GameServer.SetModDir(MyPerGameSettings.SteamGameServerGameDir);

            SteamSDK.SteamServerAPI.Instance.GameServer.ProductName = MyPerGameSettings.SteamGameServerProductName;
            SteamSDK.SteamServerAPI.Instance.GameServer.GameDescription = MyPerGameSettings.SteamGameServerDescription;
            SteamSDK.SteamServerAPI.Instance.GameServer.SetDedicated(true);

            string serverName = MySandboxGame.ConfigDedicated.ServerName;
            if (String.IsNullOrWhiteSpace(serverName))
                serverName = "Unnamed server";

            SteamSDK.SteamServerAPI.Instance.GameServer.SetServerName(serverName);
            SteamSDK.SteamServerAPI.Instance.GameServer.LogOnAnonymous();


            SteamSDK.SteamServerAPI.Instance.GameServer.EnableHeartbeats(true);

            if (m_groupId != 0 && SteamServerAPI.Instance.GetAccountType(m_groupId) != AccountType.Clan)
            {
                MyLog.Default.WriteLineAndConsole("Specified group ID is invalid: " + m_groupId);
            }

            UInt32 ip = 0;
            UInt64 id = 0;

            int timeout = 100;
            while (ip == 0 && timeout > 0)
            {
                SteamSDK.SteamServerAPI.Instance.GameServer.RunCallbacks();

                Thread.Sleep(100);
                timeout--;

                ip = SteamSDK.SteamServerAPI.Instance.GameServer.GetPublicIP();
                id = SteamSDK.SteamServerAPI.Instance.GameServer.GetSteamID();
            }

            MySandboxGame.Services.SteamService.UserId = id;

            if (ip == 0)
            {
                MyLog.Default.WriteLineAndConsole("Error: No IP assigned.");
                return;
            }

            var ipAddress = IPAddressExtensions.FromIPv4NetworkOrder(ip);

            ServerId = MySteam.Server.GetSteamID();
            m_members.Add(ServerId);
            m_memberData.Add(ServerId, new MyConnectedClientData() { Name = "Server", IsAdmin = true });

            SyncLayer.RegisterClientEvents(this);

            MyLog.Default.WriteLineAndConsole("Server successfully started");
            MyLog.Default.WriteLineAndConsole("Product name: " + SteamSDK.SteamServerAPI.Instance.GameServer.ProductName);
            MyLog.Default.WriteLineAndConsole("Desc: " + SteamSDK.SteamServerAPI.Instance.GameServer.GameDescription);
            MyLog.Default.WriteLineAndConsole("Public IP: " + ipAddress.ToString());
            MyLog.Default.WriteLineAndConsole("Steam ID: " + id.ToString());

            ServerStarted = true;
        }

        internal abstract void SendGameTagsToSteam();
        protected abstract void SendServerData();

        void Peer2Peer_SessionRequest(ulong remoteUserId)
        {
            if (IsClientKickedOrBanned(remoteUserId)) return;

            MyLog.Default.WriteLineAndConsole("Peer2Peer_SessionRequest " + remoteUserId);
            SteamSDK.Peer2Peer.AcceptSession(remoteUserId);
        }

        void Peer2Peer_ConnectionFailed(ulong remoteUserId, P2PSessionErrorEnum error)
        {
            MyLog.Default.WriteLineAndConsole("Peer2Peer_ConnectionFailed " + remoteUserId + ", " + error);
        }

        void MyDedicatedServer_ClientLeft(ulong user, ChatMemberStateChangeEnum arg2)
        {
            Peer2Peer.CloseSession(user);
            MyLog.Default.WriteLineAndConsole("User left " + GetMemberName(user));
            if (m_members.Contains(user))
                m_members.Remove(user);

            if (m_pendingMembers.ContainsKey(user))
                m_pendingMembers.Remove(user);

            if (m_waitingForGroup.Contains(user))
                m_waitingForGroup.Remove(user);

            if (arg2 != ChatMemberStateChangeEnum.Kicked && arg2 != ChatMemberStateChangeEnum.Banned)
            {
                foreach (var member in m_members)
                {
                    if (member != ServerId)
                    {
                        MyControlDisconnectedMsg msg = new MyControlDisconnectedMsg();
                        msg.Client = user;
                        SendControlMessage(member, ref msg);
                    }
                }
            }

            SteamSDK.SteamServerAPI.Instance.GameServer.SendUserDisconnect(user);

            m_memberData.Remove(user);
        }

        void GameServer_ValidateAuthTicketResponse(ulong steamID, SteamSDK.AuthSessionResponseEnum response, ulong steamOwner)
        {
            MyLog.Default.WriteLineAndConsole("Server ValidateAuthTicketResponse (" + response.ToString() + "), owner: " + steamOwner.ToString());

            if (IsClientKickedOrBanned(steamOwner) || MySandboxGame.ConfigDedicated.Banned.Contains(steamOwner))
            {
                UserRejected(steamID, JoinResult.BannedByAdmins);
                RaiseClientKicked(steamID);
            }

            if (response == AuthSessionResponseEnum.OK)
            {
                if (MemberLimit > 0 && m_members.Count - 1 >= MemberLimit) // Unfortunately, DS counds into the members, so subtract it
                {
                    UserRejected(steamID, JoinResult.ServerFull);
                }
                else if (m_groupId == 0 || MySandboxGame.ConfigDedicated.Administrators.Contains(steamID.ToString()) || MySandboxGame.ConfigDedicated.Administrators.Contains(ConvertSteamIDFrom64(steamID)))
                {
                    UserAccepted(steamID);
                }
                else if (SteamServerAPI.Instance.GetAccountType(m_groupId) != AccountType.Clan)
                {
                    UserRejected(steamID, JoinResult.GroupIdInvalid);
                }
                else if (SteamServerAPI.Instance.GameServer.RequestGroupStatus(steamID, m_groupId))
                {
                    // Returns false when there's no connection to Steam
                    m_waitingForGroup.Add(steamID);
                }
                else
                {
                    UserRejected(steamID, JoinResult.SteamServersOffline);
                }
            }
            else
            {
                JoinResult joinResult = JoinResult.TicketInvalid;
                switch (response)
                {
                    case AuthSessionResponseEnum.AuthTicketCanceled:
                        joinResult = JoinResult.TicketCanceled;
                        break;
                    case AuthSessionResponseEnum.AuthTicketInvalidAlreadyUsed:
                        joinResult = JoinResult.TicketAlreadyUsed;
                        break;
                    case AuthSessionResponseEnum.LoggedInElseWhere:
                        joinResult = JoinResult.LoggedInElseWhere;
                        break;
                    case AuthSessionResponseEnum.NoLicenseOrExpired:
                        joinResult = JoinResult.NoLicenseOrExpired;
                        break;
                    case AuthSessionResponseEnum.UserNotConnectedToSteam:
                        joinResult = JoinResult.UserNotConnected;
                        break;
                    case AuthSessionResponseEnum.VACBanned:
                        joinResult = JoinResult.VACBanned;
                        break;
                    case AuthSessionResponseEnum.VACCheckTimedOut:
                        joinResult = JoinResult.VACCheckTimedOut;
                        break;
                }

                UserRejected(steamID, joinResult);
            }
        }

        void GameServer_UserGroupStatus(ulong userId, ulong groupId, bool member, bool officier)
        {
            if (groupId == m_groupId && m_waitingForGroup.Remove(userId))
            {
                if ((member || officier))
                {
                    UserAccepted(userId);
                }
                else
                {
                    UserRejected(userId, JoinResult.NotInGroup);
                }
            }
        }

        void GameServer_PolicyResponse(sbyte result)
        {
            MyLog.Default.WriteLineAndConsole("Server PolicyResponse (" + result.ToString() + ")");
        }

        void GameServer_ServersDisconnected(SteamSDK.Result result)
        {
            MyLog.Default.WriteLineAndConsole("Server disconnected (" + result.ToString() + ")");
        }

        void GameServer_ServersConnectFailure(SteamSDK.Result result)
        {
            MyLog.Default.WriteLineAndConsole("Server connect failure (" + result.ToString() + ")");
        }

        void GameServer_ServersConnected()
        {
            MyLog.Default.WriteLineAndConsole("Server connected to Steam");
        }

        private void UserRejected(ulong steamID, JoinResult reason)
        {
            m_pendingMembers.Remove(steamID);
            m_waitingForGroup.Remove(steamID);

            if (m_members.Contains(steamID))
            {
                RaiseClientLeft(steamID, ChatMemberStateChangeEnum.Disconnected);
            }
            else
            {
                SendJoinResult(steamID, reason);
            }
        }

        protected virtual void UserAccepted(ulong steamID)
        {
            System.Diagnostics.Debug.Assert(!m_members.Contains(steamID));
            m_members.Add(steamID);

            MyConnectedClientData clientData;
            if (m_pendingMembers.TryGetValue(steamID, out clientData))
            {
                m_pendingMembers.Remove(steamID);
                m_memberData[steamID] = clientData;


                foreach (var user in m_members)
                {
                    if (user != ServerId)
                    {
                        SendClientData(user, steamID, clientData.Name, true);

                        // CH:Note: The connecting player will get the information about other connected players from the world object builder
                        //if (steamID != user)
                        //    SendClientData(steamID, user, m_memberData[user].Name, false);
                    }
                }
            }

            SendServerData();
            SendJoinResult(steamID, JoinResult.OK);
        }

        public override bool IsCorrectVersion()
        {
            return m_appVersion == MyFinalBuildConstants.APP_VERSION;
        }

        public override MyDownloadWorldResult DownloadWorld()
        {
            System.Diagnostics.Debug.Fail("Dedicated server cannot download world, only create or load");
            return null;
        }

        public override void DisconnectClient(ulong userId)
        {
            MyControlDisconnectedMsg msg = new MyControlDisconnectedMsg();
            msg.Client = ServerId;
            SendControlMessage(userId, ref msg);

            RaiseClientLeft(userId, ChatMemberStateChangeEnum.Disconnected);
        }

        public override void KickClient(ulong userId)
        {
            MyControlKickClientMsg msg = new MyControlKickClientMsg();
            msg.KickedClient = userId;

            MyLog.Default.WriteLineAndConsole("Player " + GetMemberName(userId) + " kicked");
            SendControlMessageToAll(ref msg);

            AddKickedClient(userId);
            RaiseClientLeft(userId, ChatMemberStateChangeEnum.Kicked);
        }

        public override void BanClient(ulong userId, bool banned)
        {
            if (banned)
            {
                MyLog.Default.WriteLineAndConsole("Player " + GetMemberName(userId) + " banned");
                MyControlBanClientMsg msg = new MyControlBanClientMsg();
                msg.BannedClient = userId;
                msg.Banned = true;
                SendControlMessageToAll(ref msg);

                AddBannedClient(userId);
                if (m_members.Contains(userId))
                {
                    RaiseClientLeft(userId, ChatMemberStateChangeEnum.Banned);
                }

                MySandboxGame.ConfigDedicated.Banned.Add(userId);
            }
            else
            {
                MyLog.Default.WriteLineAndConsole("Player " + userId.ToString() + " unbanned");
                RemoveBannedClient(userId);
                MySandboxGame.ConfigDedicated.Banned.Remove(userId);
            }

            MySandboxGame.ConfigDedicated.Save();
        }

        public override void Tick()
        {
            base.Tick();

            UpdateSteamServerData();
        }

        void UpdateSteamServerData()
        {
            SteamSDK.SteamServerAPI.Instance.GameServer.SetMapName(m_worldName);
            SteamSDK.SteamServerAPI.Instance.GameServer.SetMaxPlayerCount(m_membersLimit);

            foreach (var memberData in m_memberData)
            {
                SteamSDK.SteamServerAPI.Instance.GameServer.BUpdateUserData(memberData.Key, memberData.Value.Name, 0);
            }
        }

        public override void SendChatMessage(string text)
        {
            ChatMsg msg = new ChatMsg();
            msg.Text = text;
            msg.Author = Sync.MyId;

            SendChatMessage(ref msg);
            // This will send the message to every client except message author
            OnChatMessage(ref msg);
        }

        public void SendJoinResult(ulong sendTo, JoinResult joinResult, ulong adminID = 0)
        {
            JoinResultMsg msg = new JoinResultMsg();
            msg.JoinResult = joinResult;
            msg.Admin = adminID;

            ReplicationLayer.SendJoinResult(ref msg,sendTo);
        }

        public override void Dispose()
        {
            MyTrace.Send(TraceWindow.Multiplayer, "Multiplayer closed");

            foreach (var member in m_members)
            {
                MyControlDisconnectedMsg msg = new MyControlDisconnectedMsg();
                msg.Client = ServerId;

                if (member != ServerId)
                    SendControlMessage(member, ref msg);
            }


            //TODO: Any better way? P2P needs to be closed from both sides. If closed right after Send, message 
            //can stay not sent.
            Thread.Sleep(200);

            try
            {
                CloseMemberSessions();

                SteamSDK.SteamServerAPI.Instance.GameServer.EnableHeartbeats(false);

                base.Dispose();

                MyLog.Default.WriteLineAndConsole("Logging off Steam...");
                SteamSDK.SteamServerAPI.Instance.GameServer.LogOff();

                MyLog.Default.WriteLineAndConsole("Shutting down server...");
                SteamSDK.SteamServerAPI.Instance.GameServer.Shutdown();
                MyLog.Default.WriteLineAndConsole("Done");

                SteamSDK.Peer2Peer.SessionRequest -= Peer2Peer_SessionRequest;
                SteamSDK.Peer2Peer.ConnectionFailed -= Peer2Peer_ConnectionFailed;
                ClientLeft -= MyDedicatedServer_ClientLeft;
            }
            catch(Exception ex)
            {
                MyLog.Default.WriteLineAndConsole("catch exception : " + ex.ToString());
            }
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
            set
            {
                SetMemberLimit(Math.Max(value, 2));
                SteamSDK.SteamServerAPI.Instance.GameServer.SetMaxPlayerCount(m_membersLimit);
            }
        }

        public override ulong GetOwner()
        {
            return ServerId;
        }

        [Obsolete("Use MySession.IsUserAdmin")]
        public override bool IsAdmin(ulong steamID)
        {
            if (m_memberData.ContainsKey(steamID))
                return m_memberData[steamID].IsAdmin;

            return false;
        }

        public override void SetOwner(ulong owner)
        {
            System.Diagnostics.Debug.Fail("Cannot change owner of dedicated server");
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
            m_membersLimit = MyDedicatedServerOverrides.MaxPlayers.HasValue ? MyDedicatedServerOverrides.MaxPlayers.Value : limit;
        }

        protected void OnConnectedClient(ref ConnectedClientDataMsg msg, ulong steamId)
        {
            RaiseClientJoined(steamId);

            MyLog.Default.WriteLineAndConsole("OnConnectedClient " + msg.Name + " attempt");
            System.Diagnostics.Debug.Assert(msg.Join);

            if (m_members.Contains(steamId))
            {
                MyLog.Default.WriteLineAndConsole("Already joined");
                SendJoinResult(steamId, JoinResult.AlreadyJoined);
                return;
            }

            if (MySandboxGame.ConfigDedicated.Banned.Contains(steamId))
            {
                MyLog.Default.WriteLineAndConsole("User is banned by admins");

                ulong adminID = 0;
                foreach (var user in m_memberData)
                {
                    if (user.Value.IsAdmin)
                    {
                        adminID = user.Key;
                        break;
                    }
                }

                if (adminID == 0 && MySandboxGame.ConfigDedicated.Administrators.Count > 0)
                {
                    adminID = ConvertSteamIDTo64(MySandboxGame.ConfigDedicated.Administrators[0]);
                }


                SendJoinResult(steamId, JoinResult.BannedByAdmins, adminID);
                return;
            }

            AuthSessionResponseEnum res = SteamSDK.SteamServerAPI.Instance.GameServer.BeginAuthSession(steamId, msg.Token);
            if (res != AuthSessionResponseEnum.OK)
            {
                MyLog.Default.WriteLineAndConsole("Authentication failed (" + res.ToString() + ")");
                SendJoinResult(steamId, JoinResult.TicketInvalid);
                return;
            }

            m_pendingMembers.Add(steamId, new MyConnectedClientData()
            {
                Name = msg.Name,
                IsAdmin = MySandboxGame.ConfigDedicated.Administrators.Contains(steamId.ToString()) || MySandboxGame.ConfigDedicated.Administrators.Contains(ConvertSteamIDFrom64(steamId)),
            });
        }

        public override string GetMemberName(ulong steamUserID)
        {
            MyConnectedClientData clientData;
            m_memberData.TryGetValue(steamUserID, out clientData);
            return clientData.Name == null ? ("ID:" + steamUserID) : clientData.Name;
        }

        void SendClientData(ulong steamTo, ulong connectedSteamID, string connectedClientName, bool join)
        {
            ConnectedClientDataMsg msg = new ConnectedClientDataMsg();
            msg.SteamID = connectedSteamID;
            msg.Name = connectedClientName;
            msg.IsAdmin = MySandboxGame.ConfigDedicated.Administrators.Contains(connectedSteamID.ToString()) || MySandboxGame.ConfigDedicated.Administrators.Contains(ConvertSteamIDFrom64(connectedSteamID));
            msg.Join = join;

            ReplicationLayer.SendClientConnected(ref msg, steamTo);
        }

        protected override void OnClientKick(ref MyControlKickClientMsg data, ulong sender)
        {
            if (MySession.Static.IsUserAdmin(sender))
                KickClient(data.KickedClient);
        }

        protected override void OnClientBan(ref MyControlBanClientMsg data, ulong sender)
        {
            if (MySession.Static.IsUserAdmin(sender))
                BanClient(data.BannedClient, data.Banned);
        }

        void ClientConnected(VRage.MyPacket packet)
        {
           ConnectedClientDataMsg msg =  ReplicationLayer.OnClientConnected(packet);
           OnConnectedClient(ref msg,msg.SteamID);
        }
    }
}
