#region Using


using Sandbox.Engine.Networking;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.World;
using SteamSDK;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using VRage;
using VRage.Collections;
using VRage.Compiler;
using VRage.ObjectBuilders;
using VRage.Serialization;
using VRage.Trace;
using VRage.Library.Collections;
using VRage.Network;
using VRage.Library.Utils;
using Sandbox.Game;
using VRage.Game;
using VRage.Profiler;
using VRage.Utils;

#endregion

namespace Sandbox.Engine.Multiplayer
{
    public enum MyControlMessageEnum : byte
    {
        WorldRequest,
        Kick,
        Disconnected,
        Ban,

        Ack,
        Ping,

        BattleKeyValue,
        ProfilerRequest,
        HeaderAck,
    }


    #region Control messages data

    public struct MyControlWorldRequestMsg
    {
    }

    public struct MyControlAckMessageMsg
    {
        public int channel;
        public int index;
        public int head;
    }

    public struct MyControlAckHeaderMessageMsg
    {
        public int channel;
    }

    public struct MyControlProfilerMsg
    {
        public int index;
    }

    public struct MyControlKickClientMsg
    {
        public ulong KickedClient;
    }

    public struct MyControlDisconnectedMsg
    {
        public ulong Client;
    }

    public struct MyControlBanClientMsg
    {
        public ulong BannedClient;
        public BoolBlit Banned;
    }

    public struct AllMembersDataMsg
    {
        public List<MyObjectBuilder_Identity> Identities;
        public List<MyPlayerCollection.AllPlayerData> Players;
        public List<MyObjectBuilder_Faction> Factions;
        public List<MyObjectBuilder_Client> Clients;
    }

    #endregion

    #region Control messages callbacks

    interface IControlMessageCallback<TMsg> : ITransportCallback
    {
        void Write(ByteStream destination, ref TMsg msg);
    }

    public delegate void ControlMessageHandler<T>(ref T message, ulong sender) where T : struct;

    public class MyControlMessageCallback<TMsg> : IControlMessageCallback<TMsg>
            where TMsg : struct
    {
        public readonly ISerializer<TMsg> Serializer;
        public readonly ControlMessageHandler<TMsg> Callback;
        public readonly MyMessagePermissions Permission;

        public MyControlMessageCallback(ControlMessageHandler<TMsg> callback, ISerializer<TMsg> serializer, MyMessagePermissions permission)
        {
            this.Callback = callback;
            this.Serializer = serializer;
            this.Permission = permission;
        }

        public void Write(ByteStream destination, ref TMsg msg)
        {
            Serializer.Serialize(destination, ref msg);
        }

        void ITransportCallback.Receive(ByteStream source, ulong sender, MyTimeSpan timestamp)
        {
            if (!MySyncLayer.CheckReceivePermissions(sender, Permission))
            {
                return;
            }

            TMsg msg;
            try
            {
                Serializer.Deserialize(source, out msg);
                MyTrace.Send(TraceWindow.Multiplayer, "Received control message: " + msg.ToString(), sender + ", " + source.Position + " B");
            }
            catch (Exception e)
            {
                // Catch, add more info (what message) and write to log
                MySandboxGame.Log.WriteLine(new Exception(String.Format("Error deserializing '{0}', message size '{1}'", typeof(TMsg).Name, source.Length), e));
                return;
            }

            Callback(ref msg, sender);
        }

        string ITransportCallback.MessageType
        {
            get { return TypeNameHelper<TMsg>.Name; }
        }
    }

    #endregion

    [StaticEventOwner]
    [PreloadRequired]
    public abstract class MyMultiplayerBase : IDisposable
    {
        public struct MyConnectedClientData
        {
            public string Name;
            public bool IsAdmin;
        }

        public readonly MySyncLayer SyncLayer;
        public MyReplicationLayer ReplicationLayer { get; private set; }

        private MemoryStream m_worldSendStream;
        ByteStream m_controlReceiveStream;
        ByteStream m_controlSendStream;

        private ulong m_serverId;
        private Dictionary<string, byte[]> m_voxelMapData;

        // Stores which clients were kicked from the game and when they were kicked.
        // This serves to prevent the multiplayer client/server from re-openning P2P sessions to them
        // Is erased upon server restart. For permanent kick, use banning
        private Dictionary<ulong, int> m_kickedClients;

        // Stores banned clients to prevent clients from re-openning P2P sessions to them
        // It is erased upon game restart, so the server should save this elsewhere to make it persistent
        private HashSet<ulong> m_bannedClients;

        private static List<ulong> m_tmpClientList = new List<ulong>();

        private int m_lastKickUpdate;

        // Memory leak - blocks are kept in memory
        // All entities are put into this hashset, but the hashset is not being cleared since revision 62788.
        //private HashSet<MySyncEntity> m_dirtyPhysicsEntities = new HashSet<MySyncEntity>();

        Dictionary<int, ITransportCallback> m_controlMessageHandlers = new Dictionary<int, ITransportCallback>();
        Dictionary<Type, MyControlMessageEnum> m_controlMessageTypes = new Dictionary<Type, MyControlMessageEnum>();

        Dictionary<ulong, MyMultipartSender> m_worldSenders = new Dictionary<ulong, MyMultipartSender>();
        Dictionary<ulong, MyMultipartSender> m_profilerSenders = new Dictionary<ulong, MyMultipartSender>();
        List<ulong> m_worldSendersToRemove = new List<ulong>();
        List<ulong> m_profilerSendersToRemove = new List<ulong>();

        private TimeSpan m_lastSentTimeTimestamp = new TimeSpan();
        private BitStream m_sendPhysicsStream = new BitStream();

        private const int KICK_TIMEOUT_MS = 300 * 1000; // A kick timeouts after five minutes

        #region Properties

        public ulong ServerId
        {
            get
            {
                //Debug.Assert(Lobby.GetOwner() == m_serverId, "Server id is invalid");
                return m_serverId;
            }
            protected set { m_serverId = value; }
        }
        public abstract bool IsServer { get; }

        float m_serverSimulationRatio = 1.0f;
        public float ServerSimulationRatio
        {
            get 
            {
                return (float)Math.Round(m_serverSimulationRatio, 2);
            }
            set 
            {
                m_serverSimulationRatio = value;
            }
        }

        [Event, Server, Reliable]
        public static void OnSetPriorityMultiplier(float priority)
        {
            MyMultiplayer.Static.ReplicationLayer.SetPriorityMultiplier(MyEventContext.Current.Sender, priority);
        }

        public DictionaryReader<string, byte[]> VoxelMapData { get { return m_voxelMapData; } }
        public uint FrameCounter { get; private set; }

        public abstract string WorldName { get; set; }
        public abstract MyGameModeEnum GameMode { get; set; }
        public abstract float InventoryMultiplier { get; set; }
        public abstract float AssemblerMultiplier { get; set; }
        public abstract float RefineryMultiplier { get; set; }
        public abstract float WelderMultiplier { get; set; }
        public abstract float GrinderMultiplier { get; set; }
        public abstract string HostName { get; set; }
        public abstract ulong WorldSize { get; set; }
        public abstract int AppVersion { get; set; }
        public abstract string DataHash { get; set; }
        public abstract int MaxPlayers { get; }
        public abstract int ModCount { get; protected set; }
        public abstract List<MyObjectBuilder_Checkpoint.ModItem> Mods { get; set; }
        public abstract int ViewDistance { get; set; }
        public abstract bool Scenario { get; set; }
        public abstract string ScenarioBriefing { get; set; }
        public abstract DateTime ScenarioStartTime { get; set; }

        #endregion

        public abstract bool IsCorrectVersion();

        public event Action<ulong> ClientJoined;
        public event Action<ulong, ChatMemberStateChangeEnum> ClientLeft;
        public event Action HostLeft;
        public event Action<ulong, string, ChatEntryTypeEnum> ChatMessageReceived;
        public event Action<string, string, string> ScriptedChatMessageReceived;
        public event Action<ulong> ClientKicked;

        internal MyMultiplayerBase(MySyncLayer syncLayer)
        {
            SyncLayer = syncLayer;
            m_controlSendStream = new ByteStream(64 * 1024, true);
            m_controlReceiveStream = new ByteStream();

            m_kickedClients = new Dictionary<ulong, int>();
            m_bannedClients = new HashSet<ulong>();

            m_lastKickUpdate = MySandboxGame.TotalTimeInMilliseconds;

            MyNetworkReader.SetHandler(MyMultiplayer.ControlChannel, ControlMessageReceived, DisconnectClient);

            RegisterControlMessage<MyControlWorldRequestMsg>(MyControlMessageEnum.WorldRequest, OnWorldRequest, MyMessagePermissions.ToServer);
            RegisterControlMessage<MyControlAckMessageMsg>(MyControlMessageEnum.Ack, OnAck, MyMessagePermissions.ToServer);
            RegisterControlMessage<MyControlAckHeaderMessageMsg>(MyControlMessageEnum.HeaderAck, OnHeaderAck, MyMessagePermissions.ToServer);
            RegisterControlMessage<MyControlKickClientMsg>(MyControlMessageEnum.Kick, OnClientKick, MyMessagePermissions.FromServer | MyMessagePermissions.ToServer);
            RegisterControlMessage<MyControlDisconnectedMsg>(MyControlMessageEnum.Disconnected, OnDisconnectedClient, MyMessagePermissions.FromServer | MyMessagePermissions.ToServer);
            RegisterControlMessage<MyControlBanClientMsg>(MyControlMessageEnum.Ban, OnClientBan, MyMessagePermissions.FromServer | MyMessagePermissions.ToServer);
            RegisterControlMessage<MyControlProfilerMsg>(MyControlMessageEnum.ProfilerRequest, OnProfilerRequest, MyMessagePermissions.ToServer);
            //m_serializers[typeof(MyControlMessageData)] = new XmlSerializer(typeof(MyControlMessageData));

            syncLayer.TransportLayer.DisconnectPeerOnError = DisconnectClient;

            // TODO: Remove
            //SyncLayer.TransportLayer.Register(MyMessageId.SERVER_UPDATE, OnServerPhysicsUpdate);

        }

        protected virtual void SetReplicationLayer(MyReplicationLayer layer)
        {
            if (ReplicationLayer != null)
                throw new InvalidOperationException("Replication layer already set");
            ReplicationLayer = layer;
            ReplicationLayer.RegisterFromGameAssemblies();
        }

        public bool IsConnectionDirect
        {
            get
            {
                if (IsServer)
                    return true;

                P2PSessionState state = default(P2PSessionState);
                Peer2Peer.GetSessionState(ServerId, ref state);
                return !state.UsingRelay;
            }
        }

        public bool IsConnectionAlive
        {
            get
            {
                if (IsServer)
                    return true;

                P2PSessionState state = default(P2PSessionState);
                Peer2Peer.GetSessionState(ServerId, ref state);
                return state.ConnectionActive;
            }
        }

        public DateTime LastMessageReceived
        {
            get
            {
                return MyMultiplayer.ReplicationLayer.LastMessageFromServer;
            }
        }


        internal void RegisterControlMessage<T>(MyControlMessageEnum msg, ControlMessageHandler<T> handler, MyMessagePermissions permission) where T : struct
        {
            MyControlMessageCallback<T> callback = new MyControlMessageCallback<T>(handler, MySyncLayer.GetSerializer<T>(), permission);
            m_controlMessageHandlers.Add((int)msg, callback);
            m_controlMessageTypes.Add(typeof(T), msg);
        }

        unsafe void ControlMessageReceived(byte[] data, int dataSize, ulong sender, MyTimeSpan timestamp, MyTimeSpan receivedTime)
        {
            ProfilerShort.Begin("Process control message");

            m_controlReceiveStream.Reset(data, dataSize);

            MyControlMessageEnum msgId = (MyControlMessageEnum)m_controlReceiveStream.ReadUShort();

            ITransportCallback handler;
            if (m_controlMessageHandlers.TryGetValue((int)msgId, out handler))
            {
                handler.Receive(m_controlReceiveStream, sender, MyTimeSpan.Zero);
            }
            ProfilerShort.End();
        }

        public void SendAck(ulong sendTo, int channel, int index, int head)
        {
            MyControlAckMessageMsg msg = new MyControlAckMessageMsg() { channel = channel, index = index, head = head };
            SendControlMessage(sendTo, ref msg, false);
        }

        public void SendHeaderAck(ulong sendTo, int channel)
        {
            MyControlAckHeaderMessageMsg msg = new MyControlAckHeaderMessageMsg() { channel = channel };
            SendControlMessage(sendTo, ref msg);
        }

        protected void SendControlMessage<T>(ulong user, ref T message, bool reliable = true) where T : struct
        {
            ITransportCallback handler;
            MyControlMessageEnum messageEnum;
            m_controlMessageTypes.TryGetValue(typeof(T), out messageEnum);
            m_controlMessageHandlers.TryGetValue((int)messageEnum, out handler);

            var callback = ((MyControlMessageCallback<T>)handler);
            if (!MySyncLayer.CheckSendPermissions(user, callback.Permission))
            {
                return;
            }


            m_controlSendStream.Position = 0;
            m_controlSendStream.WriteUShort((ushort)messageEnum);
            callback.Write(m_controlSendStream, ref message);

            if (!Peer2Peer.SendPacket(user, m_controlSendStream.Data, (int)m_controlSendStream.Position, reliable ? P2PMessageEnum.Reliable : P2PMessageEnum.Unreliable, MyMultiplayer.ControlChannel))
            {
                System.Diagnostics.Debug.Fail("P2P packet not sent");
            }

            // Peer2Peer.SendPacket(user, (byte*)&msg, sizeof(ControlMessageStruct), P2PMessageEnum.Reliable, MyMultiplayer.ControlChannel);
        }

        internal void SendControlMessageToAll<T>(ref T message, ulong exceptUserId = 0) where T : struct
        {
            for (int i = 0; i < MemberCount; i++)
            {
                ulong member = GetMemberByIndex(i);
                if (member != Sync.MyId && member != exceptUserId)
                    SendControlMessage(member, ref message);
            }
        }

        protected void OnAck(ref MyControlAckMessageMsg msg, ulong send)
        {
            MyMultipartSender msgSender;
            switch (msg.channel)
            {
                case (int)MyMultiplayer.WorldDownloadChannel:
                    if (m_worldSenders.TryGetValue(send, out msgSender))
                        msgSender.ReceiveAck(msg.index, msg.head);
                    break;
                case (int)MyMultiplayer.ProfilerDownloadChannel:
                    if (m_profilerSenders.TryGetValue(send, out msgSender))
                        msgSender.ReceiveAck(msg.index, msg.head);
                    break;
            }
        }

        protected void OnHeaderAck(ref MyControlAckHeaderMessageMsg msg, ulong send)
        {
            MyMultipartSender msgSender;
            switch (msg.channel)
            {
                case (int)MyMultiplayer.WorldDownloadChannel:
                    if (m_worldSenders.TryGetValue(send, out msgSender))
                    {
                        msgSender.HeaderAck = true;
                        msgSender.SendWhole();
                    }
                    break;
                case (int)MyMultiplayer.ProfilerDownloadChannel:
                    if (m_profilerSenders.TryGetValue(send, out msgSender))
                    {
                        msgSender.HeaderAck = true;
                        msgSender.SendWhole();
                    }
                    break;
            }
        }

        protected void OnWorldRequest(ref MyControlWorldRequestMsg data, ulong sender)
        {
            ProfilerShort.Begin("OnWorldRequest");

            MyTrace.Send(TraceWindow.Multiplayer, "World request received");

            MySandboxGame.Log.WriteLineAndConsole("World request received: " + GetMemberName(sender));

            if (IsClientKickedOrBanned(sender) || MySandboxGame.ConfigDedicated.Banned.Contains(sender))
            {
                MySandboxGame.Log.WriteLineAndConsole("Sending no world, because client has been kicked or banned: " + GetMemberName(sender) + " (Client is probably modified.)");
                RaiseClientLeft(sender, ChatMemberStateChangeEnum.Banned);
                return;
            }

            m_worldSendStream = new MemoryStream();

            if (IsServer && MySession.Static != null)
            {
                MySandboxGame.Log.WriteLine("...responding");

                MyObjectBuilder_World worldData = MySession.Static.GetWorld(false);
                var checkpoint = worldData.Checkpoint;
                checkpoint.WorkshopId = null;
                checkpoint.CharacterToolbar = null;
                checkpoint.Settings.ScenarioEditMode = checkpoint.Settings.ScenarioEditMode && !MySession.Static.LoadedAsMission;

                worldData.Clusters = new List<VRageMath.BoundingBoxD>();
                Sandbox.Engine.Physics.MyPhysics.SerializeClusters(worldData.Clusters);

                ProfilerShort.Begin("SerializeXML");
                MyObjectBuilderSerializer.SerializeXML(m_worldSendStream, worldData, MyObjectBuilderSerializer.XmlCompression.Gzip);
                ProfilerShort.BeginNextBlock("SendFlush");
                SyncLayer.TransportLayer.SendFlush(sender);
                ProfilerShort.End();
            }
            var buffer = m_worldSendStream.ToArray();
            MyMultipartSender msgSender = new MyMultipartSender(buffer, buffer.Length, sender, MyMultiplayer.WorldDownloadChannel, 1150);
            m_worldSenders[sender] = msgSender;

            ProfilerShort.End();
        }

        protected abstract void OnClientKick(ref MyControlKickClientMsg data, ulong sender);
        protected abstract void OnClientBan(ref MyControlBanClientMsg data, ulong sender);

        protected void OnProfilerRequest(ref MyControlProfilerMsg data, ulong sender)
        {
            if (IsServer && !m_profilerSenders.ContainsKey(sender))
            {
                MemoryStream profilerStream = new MemoryStream();

                MyObjectBuilder_Profiler profilerData = MyObjectBuilder_Profiler.GetObjectBuilder(VRage.Profiler.MyRenderProfiler.GetProfilerAtIndex(data.index));
                MyObjectBuilderSerializer.SerializeXML(profilerStream, profilerData, MyObjectBuilderSerializer.XmlCompression.Gzip);
                SyncLayer.TransportLayer.SendFlush(sender);
                var buffer = profilerStream.ToArray();
                MyMultipartSender msgSender = new MyMultipartSender(buffer, buffer.Length, sender, MyMultiplayer.ProfilerDownloadChannel);
                m_profilerSenders[sender] = msgSender;
            }
        }

        protected virtual void OnChatMessage(ref ChatMsg msg)
        {

        }

        protected virtual void OnScriptedChatMessage(ref ScriptedChatMsg msg)
        {
            RaiseScriptedChatMessageReceived(msg.Author, msg.Text, msg.Font);
        }

        void OnDisconnectedClient(ref MyControlDisconnectedMsg data, ulong sender)
        {
            RaiseClientLeft(data.Client, ChatMemberStateChangeEnum.Disconnected);
            Console.WriteLine("Disconnected: " + sender);
        }


        public virtual MyDownloadWorldResult DownloadWorld()
        {
            //MyTrace.Send(TraceWindow.Multiplayer, "World request sent");
            //MyDownloadWorldResult ret = new MyDownloadWorldResult(WorldDownloadChannel, Lobby.GetOwner());
            //SendControlMessage(ServerId, ControlMessageEnum.WorldRequest);
            //return ret;
            return null;
        }

        public virtual void DownloadProfiler(int index)
        {
            return;
        }

        public abstract void DisconnectClient(ulong userId);
        public abstract void KickClient(ulong userId);
        public abstract void BanClient(ulong userId, bool banned);

        protected void AddKickedClient(ulong userId)
        {
            if (m_kickedClients.ContainsKey(userId))
            {
                MySandboxGame.Log.WriteLine("Trying to kick player who was already kicked!");
                Debug.Fail("Trying to kick player who was already kicked!");
            }
            else
            {
                m_kickedClients.Add(userId, MySandboxGame.TotalTimeInMilliseconds);
            }
        }

        protected void AddBannedClient(ulong userId)
        {
            if (m_bannedClients.Contains(userId))
            {
                MySandboxGame.Log.WriteLine("Trying to ban player who was already banned!");
                Debug.Fail("Trying to ban player who was already banned!");
            }
            else
            {
                m_bannedClients.Add(userId);
            }
        }

        protected void RemoveBannedClient(ulong userId)
        {
            m_bannedClients.Remove(userId);
        }

        protected bool IsClientKickedOrBanned(ulong userId)
        {
            return m_kickedClients.ContainsKey(userId) || m_bannedClients.Contains(userId);
        }

        public MyObjectBuilder_World ProcessWorldDownloadResult(MyDownloadWorldResult result)
        {
            MyTrace.Send(TraceWindow.Multiplayer, "World data processed");
            m_voxelMapData = result.WorldData.VoxelMaps.Dictionary;

            MyLog.Default.WriteLine("ProcessWorldDownloadResult voxel maps:");
            foreach (var voxelmap in m_voxelMapData)
            {
                MyLog.Default.WriteLine(voxelmap.Key);
            }

            return result.WorldData;
        }

        /// <summary>
        /// Call when downloaded world is loaded
        /// </summary>
        public void StartProcessingClientMessages()
        {
            MyTrace.Send(TraceWindow.Multiplayer, "Processing client messages");
            SyncLayer.TransportLayer.IsBuffering = false;
            MyTrace.Send(TraceWindow.Multiplayer, "Processing client messages - done");
        }

        /// <summary>
        /// Call when empty world is created (battle lobby)
        /// </summary>
        public virtual void StartProcessingClientMessagesWithEmptyWorld()
        {
            StartProcessingClientMessages();
        }

        bool TransportLayer_TypemapAccept(ulong userId)
        {
            return userId == ServerId;
        }

        public void MarkPhysicsDirty(MySyncEntity entity)
        {
            // Memory leak - blocks are kept in memory
            // All entities are put into this hashset, but the hashset is not being cleared since revision 62788.
            /*if (IsServer)
            {
                m_dirtyPhysicsEntities.Add(entity);
            }*/
        }

        public void ReportReplicatedObjects()
        {
            if (VRage.Profiler.MyRenderProfiler.ProfilerVisible)
            {
                ProfilerShort.Begin("ReportReplicatedObjects (only when profiler visible)");
                ReplicationLayer.ReportReplicatedObjects();
                ProfilerShort.End();
            }
        }

        public virtual void Tick()
        {
            FrameCounter++;

            ProfilerShort.Begin("SendElapsedGameTime");
            if (IsServer && (MySession.Static.ElapsedGameTime - m_lastSentTimeTimestamp).Seconds > 30)
            {
                m_lastSentTimeTimestamp = MySession.Static.ElapsedGameTime;
                SendElapsedGameTime();
            }
            ProfilerShort.End();

            ProfilerShort.Begin("Client kick update");
            int currentTotalTime = MySandboxGame.TotalTimeInMilliseconds;
            if (currentTotalTime - m_lastKickUpdate > 20000)
            {
                m_tmpClientList.Clear();
                foreach (var client in m_kickedClients.Keys)
                {
                    m_tmpClientList.Add(client);
                }

                foreach (var client in m_tmpClientList)
                {
                    if (currentTotalTime - m_kickedClients[client] > KICK_TIMEOUT_MS)
                        m_kickedClients.Remove(client);
                }
                m_tmpClientList.Clear();

                m_lastKickUpdate = currentTotalTime;
            }
            ProfilerShort.End();
            
            ProfilerShort.Begin("ReplicationLayer.SendUpdate");
            ReplicationLayer.SendUpdate();
            ProfilerShort.End();
            
            // TODO: Remove
            //if (IsServer)
            //{
            //    SendServerPhysicsUpdate();
            //}

            SendWorlds();
            SendProfilers();

            ProfilerShort.Begin("TransportLayer.Tick");
            Sync.Layer.TransportLayer.Tick();
            ProfilerShort.End();

            ProfilerShort.Begin("Trace, NetProfiler.Commit");
            //VRage.Trace.MyTrace.Send(VRage.Trace.TraceWindow.Multiplayer, "============ Frame end ============");
            NetProfiler.Commit();
            ProfilerShort.End();
        }

        private void SendWorlds()
        {
            foreach (var sender in m_worldSenders)
            {
                if (sender.Value.SendWhole())
                    m_worldSendersToRemove.Add(sender.Key);
            }
            foreach (var sender in m_worldSendersToRemove)
            {
                m_worldSenders.Remove(sender);
            }
            m_worldSendersToRemove.Clear();
        }

        private void SendProfilers()
        {
            foreach (var sender in m_profilerSenders)
            {
                if (sender.Value.SendWhole())
                    m_profilerSendersToRemove.Add(sender.Key);
            }
            foreach (var sender in m_profilerSendersToRemove)
            {
                m_profilerSenders.Remove(sender);
            }
            m_profilerSendersToRemove.Clear();
        }

        public abstract void SendChatMessage(string text);

        public virtual void Dispose()
        {
            MyTrace.Send(TraceWindow.Multiplayer, "Multiplayer closed");
            m_voxelMapData = null;
            MyNetworkReader.ClearHandler(MyMultiplayer.ControlChannel);
            SyncLayer.TransportLayer.Clear();
            MyNetworkReader.Clear();

            m_sendPhysicsStream.Dispose();
            ReplicationLayer.Dispose();

            MyMultiplayer.Static = null;
        }

        public abstract MemberCollection Members
        {
            get;
        }

        public abstract int MemberCount
        {
            get;
        }

        public abstract ulong GetMemberByIndex(int memberIndex);

        public abstract string GetMemberName(ulong steamUserID);

        protected void RaiseChatMessageReceived(ulong steamUserID, string messageText, ChatEntryTypeEnum chatEntryType)
        {
            var handler = ChatMessageReceived;
            if (handler != null)
                handler(steamUserID, messageText, chatEntryType);
        }

        protected void RaiseScriptedChatMessageReceived(string author, string messageText, string font)
        {
            var handler = ScriptedChatMessageReceived;
            if (handler != null)
                handler(messageText, author, font);
        }

        protected void RaiseHostLeft()
        {
            var handler = HostLeft;
            if (handler != null)
                handler();
        }

        protected void RaiseClientLeft(ulong changedUser, ChatMemberStateChangeEnum stateChange)
        {
            var handler = ClientLeft;
            if (handler != null)
                handler(changedUser, stateChange);
        }

        protected void RaiseClientJoined(ulong changedUser)
        {
            var handler = ClientJoined;
            if (handler != null)
                handler(changedUser);
        }

        protected void RaiseClientKicked(ulong user)
        {
            var handler = ClientKicked;
            if (handler != null)
                handler(user);
        }

        public abstract ulong LobbyId
        {
            get;
        }

        public abstract ulong GetOwner();
        [Obsolete("Use MySession.IsUserAdmin")]
        public abstract bool IsAdmin(ulong steamID);
        public abstract void SetOwner(ulong owner);

        public abstract int MemberLimit { get; set; }

        public abstract LobbyTypeEnum GetLobbyType();

        public abstract void SetLobbyType(LobbyTypeEnum type);

        public abstract void SetMemberLimit(int limit);

        protected void CloseMemberSessions()
        {
            for (int i = 0; i < MemberCount; i++)
            {
                var member = GetMemberByIndex(i);
                if (member != Sync.MyId && member == ServerId)
                {
                    Peer2Peer.CloseSession(member);
                }
            }
        }

        public void SendAllMembersDataToClient(ulong clientId)
        {
            Debug.Assert(Sync.IsServer);

            AllMembersDataMsg response = new AllMembersDataMsg();
            if (Sync.Players != null)
            {
                response.Identities = Sync.Players.SaveIdentities();
                response.Players = Sync.Players.SavePlayers();
            }

            if (MySession.Static.Factions != null)
                response.Factions = MySession.Static.Factions.SaveFactions();

            response.Clients = MySession.Static.SaveMembers(true);

            MyMultiplayer.RaiseStaticEvent(s => MyMultiplayerBase.OnAllMembersRecieved, response, new EndpointId(clientId));
        }

        public virtual void OnAllMembersData(ref AllMembersDataMsg msg)
        {

        }

        protected void ProcessAllMembersData(ref AllMembersDataMsg msg)
        {    
            Debug.Assert(!Sync.IsServer);

            Sync.Players.ClearIdentities();
            if (msg.Identities != null)
                Sync.Players.LoadIdentities(msg.Identities);

            Sync.Players.ClearPlayers();
            if (msg.Players != null)
                Sync.Players.LoadPlayers(msg.Players);

            MySession.Static.Factions.LoadFactions(msg.Factions, true);
        }

        protected MyClientState CreateClientState()
        {
            return Activator.CreateInstance(MyPerGameSettings.ClientStateType) as MyClientState;
        }
      
        public static void SendElapsedGameTime()
        {
            Debug.Assert(Sync.IsServer, "Only server can send time info");

            long elapsedGameTicks = MySession.Static.ElapsedGameTime.Ticks;
            MyMultiplayer.RaiseStaticEvent(s => MyMultiplayerBase.OnElapsedGameTime, elapsedGameTicks);
        }

        [Event, Broadcast]
        static void OnElapsedGameTime(long elapsedGameTicks)
        {
            MySession.Static.ElapsedGameTime = new TimeSpan(elapsedGameTicks);
        }

        protected static void SendChatMessage(ref ChatMsg msg)
        {
            MyMultiplayer.RaiseStaticEvent(s => MyMultiplayerBase.OnChatMessageRecieved, msg);
        }

        public static void SendScriptedChatMessage(ref ScriptedChatMsg msg)
        {
            MyMultiplayer.RaiseStaticEvent(s => MyMultiplayerBase.OnScriptedChatMessageRecieved, msg);
        }

        [Event,Reliable, Client]
        static void OnAllMembersRecieved(AllMembersDataMsg msg)
        {
            MyMultiplayer.Static.OnAllMembersData(ref msg);
        }

        [Event,Reliable, Server, BroadcastExcept]
        static void OnChatMessageRecieved(ChatMsg msg)
        {
            MyMultiplayer.Static.OnChatMessage(ref msg);
        }
        
        [Event,Reliable, Server, Broadcast]
        static void OnScriptedChatMessageRecieved(ScriptedChatMsg msg)
        {
            if(MySession.Static == null)
                return;
            if (msg.Target != 0 && MySession.Static.LocalPlayerId != msg.Target)
                return;
            MyMultiplayer.Static.OnScriptedChatMessage(ref msg);
        }
    }

    //necessary to have it here because of font enum
    public struct ScriptedChatMsg
    {
        public string Text;
        public string Author;
        public long Target;
        public string Font;
    }
}
