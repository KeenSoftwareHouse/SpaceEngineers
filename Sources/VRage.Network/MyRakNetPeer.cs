using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using VRage.Collections;
using VRage.Library.Utils;

namespace VRage.Network
{
    public abstract class MyRakNetPeer : IDisposable
    {
        private bool APPLY_NETWORK_SIMULATOR = false;
        //private uint MAX_OUTGOING_BITS_BANDWIDTH_PER_CONNECTION = 56 * 1000;
        //private float PACKET_LOSS = 0.25f;
        //private ushort MIN_EXTRA_PING = 100;
        //private ushort EXTRA_PING_VARIANCE = 400;

        public abstract bool IsServer { get; }

        public abstract ulong ServerId { get; protected set; }

        public abstract ulong MySteamID { get; protected set; }

        public int Version { get; protected set; }

        public void Update()
        {
            Packet packet = new Packet();
            while (RecieveMessage(out packet))
            {
                bool contains = m_messageHandlers.ContainsKey(packet.MessageID);
                Debug.Assert(contains, "Unhandled message: " + packet.MessageID.ToString());
                if (contains)
                {
                    m_messageHandlers[packet.MessageID](packet);
                }
                packet.Delete();
            }
        }

        protected Dictionary<ulong, RakNetGUID> m_steamIDToGUID = new Dictionary<ulong, RakNetGUID>();
        protected Dictionary<ulong, ulong> m_GUIDToSteamID = new Dictionary<ulong, ulong>();

        public RakNetGUID SteamIDToGUID(ulong steamID)
        {
            return m_steamIDToGUID[steamID];
        }

        public ulong GUIDToSteamID(ulong GUID)
        {
            return m_GUIDToSteamID[GUID];
        }

        protected RakPeer m_peer;
        protected MyConcurrentQueue<Packet> m_receiveQueue;
        private MyTimer m_timer;
        private Action m_timerAction;
        protected HashSet<MessageIDEnum> m_internalMessages = new HashSet<MessageIDEnum>();
        protected HashSet<MessageIDEnum> m_ignoredMessages = new HashSet<MessageIDEnum>();

        protected Dictionary<MessageIDEnum, Action<Packet>> m_messageHandlers = new Dictionary<MessageIDEnum, Action<Packet>>(256);

        //private RakNetStatistics m_stats;

        //private RakNetStatistics GetStats(RakNet.SystemAddress systemAddress = null)
        //{
        //    systemAddress = systemAddress ?? RakNet.RakNet.UNASSIGNED_SYSTEM_ADDRESS;
        //    if (m_stats == null)
        //    {
        //        m_stats = m_peer.GetStatistics(systemAddress);
        //    }
        //    else
        //    {
        //        m_stats = m_peer.GetStatistics(systemAddress, m_stats);
        //    }
        //    return m_stats;
        //}

        //public string GetStatsToString()
        //{
        //    string tmp;
        //    RakNet.RakNet.StatisticsToString(GetStats(), out tmp, 3);
        //    return tmp;
        //}

        //public ulong GetOutgoingBufferedBytes()
        //{
        //    var stats = GetStats();
        //    ulong bytes = stats.bytesInResendBuffer;

        //    foreach (var val in stats.bytesInSendBuffer)
        //    {
        //        bytes += (ulong)val;
        //    }

        //    return bytes;
        //}

        //[Conditional("DEBUG")]
        //private void ApplyNetworkSimulator(uint maxOutgoingBitsBandwidthPerConnection, float packetLoss, ushort minExtraPing, ushort extraPingVariance)
        //{
        //    m_peer.SetPerConnectionOutgoingBandwidthLimit(maxOutgoingBitsBandwidthPerConnection);
        //    m_peer.ApplyNetworkSimulator(packetLoss, minExtraPing, extraPingVariance);
        //}

        protected MyRakNetPeer(ulong steamID)
        {
            MySteamID = steamID;
            m_receiveQueue = new MyConcurrentQueue<Packet>();
            m_peer = new RakPeer(null);

            RegisterHandlers();

            if (APPLY_NETWORK_SIMULATOR)
            {
                //ApplyNetworkSimulator(MAX_OUTGOING_BITS_BANDWIDTH_PER_CONNECTION, PACKET_LOSS, MIN_EXTRA_PING, EXTRA_PING_VARIANCE);
            }
        }

        protected StartupResultEnum Startup(uint maxConnections, ushort port, string host)
        {
            StartupResultEnum result = m_peer.Startup(maxConnections, port, host);

            if (result == StartupResultEnum.RAKNET_STARTED)
            {
                m_timerAction = new Action(ReceiveMessageInternal);
                m_timer = new MyTimer(1, m_timerAction);
                m_timer.Start();
            }

            return result;
        }

        public event Action<ulong, string> OnChatMessage;
        public event Action<ulong> OnConnectionLost;
        public event Action<ulong> OnClientJoined;
        public event Action<ulong> OnClientLeft;

        //public event Action<ulong> OnClientConnecting;
        //public event Action<ulong> OnClientConnected;
        //public event Action<ulong> OnClientDisconnected;

        protected void AddIgnoredMessage(MessageIDEnum id)
        {
            m_ignoredMessages.Add(id);
        }

        protected void AddMessageHandlerAsync(MessageIDEnum id, Action<Packet> handler)
        {
            m_internalMessages.Add(id);
            AddMessageHandler(id, handler);
        }

        protected void AddMessageHandler(MessageIDEnum id, Action<Packet> handler)
        {
            m_messageHandlers[id] = handler;
        }

        private void RegisterHandlers()
        {
            AddMessageHandler(MessageIDEnum.CHAT_MESSAGE, ChatMessage);
            AddMessageHandler(MessageIDEnum.CONNECTION_LOST, ConnectionLost);

            AddMessageHandler(MessageIDEnum.SYNC_FIELD, SyncField);
            AddMessageHandler(MessageIDEnum.REPLICATION_CREATE, ReplicationCreate);
            AddMessageHandler(MessageIDEnum.REPLICATION_DESTROY, ReplicationDestroy);
        }

        private void ReplicationDestroy(Packet packet)
        {
            Debug.Assert(MyRakNetSyncLayer.Static != null);
            MyRakNetSyncLayer.Static.ProcessReplicationDestroy(packet.Data);
        }

        private void ReplicationCreate(Packet packet)
        {
            Debug.Assert(MyRakNetSyncLayer.Static != null);
            MyRakNetSyncLayer.Static.ProcessReplicationCreate(packet.Data);
        }

        private void SyncField(Packet packet)
        {
            Debug.Assert(MyRakNetSyncLayer.Static != null);
            MyRakNetSyncLayer.Static.ProcessSync(packet.Data);
        }

        protected abstract void ChatMessage(Packet packet);

        protected void RiseOnChatMessage(ulong senderSteamID, string message)
        {
            var handler = OnChatMessage;
            if (handler != null)
                handler(senderSteamID, message);
        }

        protected void RaiseOnClientJoined(ulong steamID)
        {
            var handler = OnClientJoined;
            if (handler != null)
                handler(steamID);
        }

        protected void RaiseOnClientLeft(ulong steamID)
        {
            var handler = OnClientLeft;
            if (handler != null)
                handler(steamID);
        }

        private void ConnectionLost(Packet packet)
        {
            Debug.Assert(m_GUIDToSteamID.ContainsKey(packet.GUID.G));
            ulong steamID = m_GUIDToSteamID[packet.GUID.G];

            m_steamIDToGUID[steamID].Delete();
            m_steamIDToGUID.Remove(steamID);
            m_GUIDToSteamID.Remove(packet.GUID.G);

            RaiseOnClientLeft(steamID);

            var handler = OnConnectionLost;
            if (handler != null)
                handler(steamID);
        }

        private void ReceiveMessageInternal()
        {
            if (m_timer == null)
                return;

            lock (m_timer)
            {
                Packet? packet;
                while ((packet = m_peer.Receive()).HasValue)
                {
                    if (m_ignoredMessages.Contains(packet.Value.MessageID))
                    {
                        packet.Value.Delete();
                    }
                    else if (IsInternal(packet.Value.MessageID))
                    {
                        ProcessInternal(packet.Value);
                        packet.Value.Delete();
                    }
                    else
                    {
                        m_receiveQueue.Enqueue(packet.Value);
                    }
                }
            }
        }

        private bool IsInternal(MessageIDEnum msgID)
        {
            //if (msgID <= MessageIDEnum.RESERVED_9)
            //    return true;
            return m_internalMessages.Contains(msgID);
        }

        // this would be idealy raknet plugin but we cant because c#
        private void ProcessInternal(Packet packet)
        {
            bool contains = m_messageHandlers.ContainsKey(packet.MessageID);
            Debug.Assert(contains, "Unhandled internal message: " + packet.MessageID.ToString());
            if (contains)
            {
                m_messageHandlers[packet.MessageID](packet);
            }
        }

        ~MyRakNetPeer()
        {
            Debug.Fail("RakNetPeer not disposed!");
            Dispose();
        }

        public void Dispose()
        {
            if (m_timer != null)
            {
                lock (m_timer)
                {
                    Packet packet;
                    while (m_receiveQueue.Count > 0)
                    {
                        if (m_receiveQueue.TryDequeue(out packet))
                        {
                            packet.Delete();
                        }
                    }
                    m_timer.Dispose();
                    m_timer = null;
                    m_timerAction = null;
                }
            }

            if (!m_peer.IsNull)
            {
                m_peer.Shutdown(300);
                m_peer.Delete();
            }
            GC.SuppressFinalize(this);
        }

        public abstract void SendChatMessage(string message);

        public uint SendMessage(BitStream bs, ulong steamID, PacketPriorityEnum packetPriorityEnum, PacketReliabilityEnum packetReliabilityEnum, byte channel = 0)
        {
            Debug.Assert(m_steamIDToGUID.ContainsKey(steamID));
            RakNetGUID recipent = m_steamIDToGUID[steamID];
            Debug.Assert(recipent != RakNetGUID.UNASSIGNED_RAKNET_GUID);
            return SendMessage(bs, recipent, packetPriorityEnum, packetReliabilityEnum, channel);
        }

        public uint SendMessage(BitStream data, RakNetGUID recipent, PacketPriorityEnum priority = PacketPriorityEnum.LOW_PRIORITY, PacketReliabilityEnum reliability = PacketReliabilityEnum.UNRELIABLE, byte channel = 0)
        {
            Debug.Assert(data.GetNumberOfBytesUsed() > 0, "no data");
            uint ret = m_peer.Send(data, priority, reliability, channel, recipent, false);
            Debug.Assert(ret != 0, "bad input?");
            return ret;
        }

        public bool RecieveMessage(out Packet msg)
        {
            ReceiveMessageInternal();
            return m_receiveQueue.TryDequeue(out msg);
        }
    }
}
