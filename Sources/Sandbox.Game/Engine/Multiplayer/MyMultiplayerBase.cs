#region Using


using Sandbox.Common.ObjectBuilders;
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
using System.IO;
using System.Xml.Serialization;

using VRage;
using VRage.Collections;
using VRage.Compiler;
using VRage.Serialization;
using VRage.Trace;


#endregion

namespace Sandbox.Engine.Multiplayer
{
    public enum MyControlMessageEnum : byte
    {
        WorldRequest,
        Kick,
        Disconnected,
        Ban,

        Chat,
        ServerData,
        ClientData,
        JoinResult,
        Ack,
        Ping,
    }


    #region Control messages data

    public struct MyControlWorldRequestMsg
    {
    }

    public struct MyControlAckMessageMsg
    {
    }

    public struct MyControlPingMsg
    {
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

        public MyControlMessageCallback(ControlMessageHandler<TMsg> callback, ISerializer<TMsg> serializer)
        {
            this.Callback = callback;
            this.Serializer = serializer;
        }

        public void Write(ByteStream destination, ref TMsg msg)
        {
            Serializer.Serialize(destination, ref msg);
        }

        void ITransportCallback.Receive(ByteStream source, ulong sender, TimeSpan timestamp)
        {
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


    public abstract class MyMultiplayerBase : IDisposable
    {
        public struct MyConnectedClientData
        {
            public string Name;
            public bool IsAdmin;
        }

        public readonly MySyncLayer SyncLayer;

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

        private HashSet<MySyncEntity> m_registeredEntities = new HashSet<MySyncEntity>();
        private HashSet<MySyncEntity> m_newRegisteredEntities = new HashSet<MySyncEntity>();
        Dictionary<int, ITransportCallback> m_controlMessageHandlers = new Dictionary<int, ITransportCallback>();
        Dictionary<Type, MyControlMessageEnum> m_controlMessageTypes = new Dictionary<Type, MyControlMessageEnum>();

        Dictionary<ulong, MyMultipartSender> m_worldSenders = new Dictionary<ulong, MyMultipartSender>();

        private TimeSpan m_lastSentTimeTimestamp = new TimeSpan();

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

        public float ServerSimulationRatio = 1.0f;

        public abstract string WorldName
        {
            get;
            set;
        }

        public abstract MyGameModeEnum GameMode
        {
            get;
            set;
        }

        public abstract float InventoryMultiplier
        {
            get;
            set;
        }

        public abstract float AssemblerMultiplier
        {
            get;
            set;
        }

        public abstract float RefineryMultiplier
        {
            get;
            set;
        }

        public abstract float WelderMultiplier
        {
            get;
            set;
        }

        public abstract float GrinderMultiplier
        {
            get;
            set;
        }

        public abstract string HostName
        {
            get;
            set;
        }

        public abstract ulong WorldSize
        {
            get;
            set;
        }

        public abstract int AppVersion
        {
            get;
            set;
        }

        public abstract string DataHash
        {
            get;
            set;
        }

        public abstract int MaxPlayers
        {
            get;
        }

        public abstract int ModCount
        {
            get;
            protected set;
        }

        public abstract List<MyObjectBuilder_Checkpoint.ModItem> Mods
        {
            get;
            set;
        }

        public abstract int ViewDistance
        {
            get;
            set;
        }

        public DictionaryReader<string, byte[]> VoxelMapData
        {
            get { return m_voxelMapData; }
        }

        public uint FrameCounter
        {
            get;
            private set;
        }

        public abstract bool Battle
        {
            get;
            set;
        }

        public abstract bool BattleStarted
        {
            get;
            set;
        }

        public abstract int BattleFaction1MaxBlueprintPoints
        {
            get;
            set;
        }

        public abstract int BattleFaction2MaxBlueprintPoints
        {
            get;
            set;
        }

        public abstract int BattleFaction1BlueprintPoints
        {
            get;
            set;
        }

        public abstract int BattleFaction2BlueprintPoints
        {
            get;
            set;
        }

        public abstract int BattleMapAttackerSlotsCount
        {
            get;
            set;
        }

        public abstract long BattleFaction1Id
        {
            get;
            set;
        }

        public abstract long BattleFaction2Id
        {
            get;
            set;
        }

        public abstract int BattleFaction1Slot
        {
            get;
            set;
        }

        public abstract int BattleFaction2Slot
        {
            get;
            set;
        }

        public abstract bool BattleFaction1Ready
        {
            get;
            set;
        }

        public abstract bool BattleFaction2Ready
        {
            get;
            set;
        }

        public abstract int BattleTimeLimit
        {
            get;
            set;
        }


        #endregion

        public abstract bool IsCorrectVersion();
        
        public event Action<ulong> ClientJoined;
        public event Action<ulong, ChatMemberStateChangeEnum> ClientLeft;
        public event Action HostLeft;
        public event Action<ulong, string, ChatEntryTypeEnum> ChatMessageReceived;
        public event Action<ulong> ClientKicked;


        internal MyMultiplayerBase(MySyncLayer syncLayer)
        {
            SyncLayer = syncLayer;
            m_controlSendStream = new ByteStream(64 * 1024, true);
            m_controlReceiveStream = new ByteStream();

            m_kickedClients = new Dictionary<ulong, int>();
            m_bannedClients = new HashSet<ulong>();

            m_lastKickUpdate = MySandboxGame.TotalTimeInMilliseconds;

            MyNetworkReader.SetHandler(MyMultiplayer.ControlChannel, ControlMessageReceived);

            RegisterControlMessage<MyControlWorldRequestMsg>(MyControlMessageEnum.WorldRequest, OnWorldRequest);
            RegisterControlMessage<MyControlAckMessageMsg>(MyControlMessageEnum.Ack, OnAck);
            RegisterControlMessage<MyControlKickClientMsg>(MyControlMessageEnum.Kick, OnClientKick);
            RegisterControlMessage<MyControlDisconnectedMsg>(MyControlMessageEnum.Disconnected, OnDisconnectedClient);
            RegisterControlMessage<MyControlBanClientMsg>(MyControlMessageEnum.Ban, OnClientBan);
            RegisterControlMessage<MyControlPingMsg>(MyControlMessageEnum.Ping, OnPing);            
            //m_serializers[typeof(MyControlMessageData)] = new XmlSerializer(typeof(MyControlMessageData));
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
                return SyncLayer.LastMessageFromServer;
            }
        }
        

        protected void RegisterControlMessage<T>(MyControlMessageEnum msg, ControlMessageHandler<T> handler) where T: struct
        {
            MyControlMessageCallback<T> callback = new MyControlMessageCallback<T>(handler, MySyncLayer.GetSerializer<T>());
            m_controlMessageHandlers.Add((int)msg, callback);
            m_controlMessageTypes.Add(typeof(T), msg);
        }

        unsafe void ControlMessageReceived(byte[] data, int dataSize, ulong sender, TimeSpan timestamp)
        {
            ProfilerShort.Begin("Process control message");

            m_controlReceiveStream.Reset(data, dataSize);

            MyControlMessageEnum msgId = (MyControlMessageEnum)m_controlReceiveStream.ReadUShort();

            ITransportCallback handler;
            if (m_controlMessageHandlers.TryGetValue((int)msgId, out handler))
            {
                handler.Receive(m_controlReceiveStream, sender, TimeSpan.Zero);
            }
            ProfilerShort.End();
        }

        public void SendAck(ulong sendTo)
        {
            MyControlAckMessageMsg msg = new MyControlAckMessageMsg();
            SendControlMessage(sendTo, ref msg);
        }

        public void SendPingToServer()
        {
            if (IsServer)
                return;

            MyControlPingMsg msg = new MyControlPingMsg();
            SendControlMessage(ServerId, ref msg);
        }

        protected void SendControlMessage<T>(ulong user, ref T message) where T : struct
        {
            ITransportCallback handler;
            MyControlMessageEnum messageEnum;
            m_controlMessageTypes.TryGetValue(typeof(T), out messageEnum);
            m_controlMessageHandlers.TryGetValue((int)messageEnum, out handler);
            
            m_controlSendStream.Position = 0;
            m_controlSendStream.WriteUShort((ushort)messageEnum);

            ((MyControlMessageCallback<T>)handler).Write(m_controlSendStream, ref message);

            if (!Peer2Peer.SendPacket(user, m_controlSendStream.Data, (int)m_controlSendStream.Position, P2PMessageEnum.Reliable, MyMultiplayer.ControlChannel))
            {
                System.Diagnostics.Debug.Fail("P2P packet not sent");
            }

           // Peer2Peer.SendPacket(user, (byte*)&msg, sizeof(ControlMessageStruct), P2PMessageEnum.Reliable, MyMultiplayer.ControlChannel);
        }

        protected void SendControlMessageToAllAndSelf<T>(ref T message) where T : struct
        {
            for (int i = 0; i < MemberCount; i++)
            {
                ulong member = GetMemberByIndex(i);
                SendControlMessage(member, ref message);
            }
        }

        protected void SendControlMessageToAll<T>(ref T message) where T : struct
        {
            for (int i = 0; i < MemberCount; i++)
            {
                ulong member = GetMemberByIndex(i);
                if (member != MySteam.UserId)
                    SendControlMessage(member, ref message);
            }
        }

        protected void OnAck(ref MyControlAckMessageMsg msg, ulong send)
        {
            MyMultipartSender msgSender;
            if (m_worldSenders.TryGetValue(send, out msgSender))
            {
                if (!msgSender.SendPart())
                {
                    // We're done, world is sent
                    m_worldSenders.Remove(send);
                }
            }
        }

        protected void OnWorldRequest(ref MyControlWorldRequestMsg data, ulong sender)
        {
            ProfilerShort.Begin("OnWorldRequest");

            MyTrace.Send(TraceWindow.Multiplayer, "World request received");

            MySandboxGame.Log.WriteLineAndConsole("World request received: " + GetMemberName(sender));

            if (IsClientKickedOrBanned(sender))
            {
                MySandboxGame.Log.WriteLineAndConsole("Sending no world, because client has been kicked or banned: " + GetMemberName(sender));
                return;
            }

            m_worldSendStream = new MemoryStream();

            if (IsServer && MySession.Static != null)
            {
                MySandboxGame.Log.WriteLine("...responding");

                MyMultipartMessage.SendPreemble(sender, MyMultiplayer.WorldDownloadChannel);

                MyObjectBuilder_World worldData = MySession.Static.GetWorld();
                var checkpoint = worldData.Checkpoint;
                checkpoint.WorkshopId = null;
                checkpoint.CharacterToolbar = null;
                ProfilerShort.Begin("SerializeXML");
                Sandbox.Common.ObjectBuilders.Serializer.MyObjectBuilderSerializer.SerializeXML(m_worldSendStream, worldData, Sandbox.Common.ObjectBuilders.Serializer.MyObjectBuilderSerializer.XmlCompression.Gzip);
                ProfilerShort.BeginNextBlock("SendFlush");
                SyncLayer.TransportLayer.SendFlush(sender);
                ProfilerShort.End();
            }

            var buffer = m_worldSendStream.ToArray();
            MyMultipartSender msgSender = new MyMultipartSender(buffer, buffer.Length, sender, MyMultiplayer.WorldDownloadChannel, 1150 * 12);
            m_worldSenders[sender] = msgSender;

            ProfilerShort.End();
        }

        protected abstract void OnClientKick(ref MyControlKickClientMsg data, ulong sender);
        protected abstract void OnClientBan(ref MyControlBanClientMsg data, ulong sender);
        protected abstract void OnPing(ref MyControlPingMsg data, ulong sender);

        void OnDisconnectedClient(ref MyControlDisconnectedMsg data, ulong sender)
        {
            RaiseClientLeft(data.Client, ChatMemberStateChangeEnum.Disconnected);
        }

        
        public virtual MyDownloadWorldResult DownloadWorld()
        {
            //MyTrace.Send(TraceWindow.Multiplayer, "World request sent");
            //MyDownloadWorldResult ret = new MyDownloadWorldResult(WorldDownloadChannel, Lobby.GetOwner());
            //SendControlMessage(ServerId, ControlMessageEnum.WorldRequest);
            //return ret;
            return null;
        }

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

        bool TransportLayer_TypemapAccept(ulong userId)
        {
            return userId == ServerId;
        }

        public void RegisterForTick(MySyncEntity entity)
        {
            m_newRegisteredEntities.Add(entity);
        }

        public virtual void Tick()
        {
            FrameCounter++;

            if (MyFakes.ENABLE_MULTIPLAYER_CONSTRAINT_COMPENSATION && IsServer && FrameCounter % 2 == 0)
            {
                MySyncGlobal.SendSimulationInfo();
            }

            if (IsServer && (MySession.Static.ElapsedGameTime - m_lastSentTimeTimestamp).Seconds > 30)
            {
                m_lastSentTimeTimestamp = MySession.Static.ElapsedGameTime;
                MySyncGlobal.SendElapsedGameTime();
            }

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

            foreach (var e in m_registeredEntities)
            {
                e.Tick();
            }
            var old = m_registeredEntities;
            m_registeredEntities = m_newRegisteredEntities;
            m_newRegisteredEntities = old;
            m_newRegisteredEntities.Clear();

            VRage.Trace.MyTrace.Send(VRage.Trace.TraceWindow.Multiplayer, "============ Frame end ============");
        }

        public abstract void SendChatMessage(string text);

        public virtual void Dispose()
        {
            MyTrace.Send(TraceWindow.Multiplayer, "Multiplayer closed");
            m_voxelMapData = null;
            MyNetworkReader.ClearHandler(MyMultiplayer.ControlChannel);
            SyncLayer.TransportLayer.Clear();
            MyNetworkReader.Clear();

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
        public abstract bool IsAdmin(ulong steamID);
        public abstract void SetOwner(ulong owner);

        public abstract int MemberLimit
        {
            get;
            set;
        }

        public abstract LobbyTypeEnum GetLobbyType();

        public abstract void SetLobbyType(LobbyTypeEnum type);

        public abstract void SetMemberLimit(int limit);

        protected void CloseMemberSessions()
        {
            for (int i = 0; i < MemberCount; i++)
            {
                var member = GetMemberByIndex(i);
                if (member != MySteam.UserId)
                    Peer2Peer.CloseSession(member);
            }
        }
    }
}
