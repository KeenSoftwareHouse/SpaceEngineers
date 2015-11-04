//using System;
//using System.Collections.Generic;
//using System.Diagnostics;
//using System.Linq;
//using System.Text;
//using VRage.Collections;
//using VRage.Library.Collections;
//using VRage.Library.Utils;

//namespace VRage.Network
//{
//    public abstract class MyRakNetPeer : IDisposable
//    {
//        private bool APPLY_NETWORK_SIMULATOR = false;
//        //private uint MAX_OUTGOING_BITS_BANDWIDTH_PER_CONNECTION = 56 * 1000;
//        //private float PACKET_LOSS = 0.25f;
//        //private ushort MIN_EXTRA_PING = 100;
//        //private ushort EXTRA_PING_VARIANCE = 400;

//        protected RakPeer m_peer;
//        protected MyConcurrentQueue<Packet> m_receiveQueue;
//        //private MyTimer m_timer;
//        //private Action m_timerAction;
//        protected HashSet<MessageIDEnum> m_internalMessages = new HashSet<MessageIDEnum>();
//        protected HashSet<MessageIDEnum> m_ignoredMessages = new HashSet<MessageIDEnum>();

//        protected Dictionary<MessageIDEnum, Action<Packet>> m_messageHandlers = new Dictionary<MessageIDEnum, Action<Packet>>(256);

//        // need to store delegate to prevent GC, because it goes to native code as function pointer
//        private IncomingDatagramEventHandler m_datagramHandler;
//        private List<IncomingDatagramEventHandler> m_datagramHandlerList = new List<IncomingDatagramEventHandler>();

//        private Queue<int> m_freeClientIndexes;

//        protected Dictionary<int, MyClientEntry> m_clientIndexToClientEntry;
//        protected Dictionary<EndpointId, MyClientEntry> m_endpointIdToClient;

//        protected VRage.Library.Collections.BitStream m_sendStream = new Library.Collections.BitStream();
//        protected VRage.Library.Collections.BitStream m_receiveStream = new Library.Collections.BitStream();
//        protected IMyPeerCallback Callback { get; private set; }

//        public readonly bool IsServer;

//        public uint MaxClients { get; private set; }
//        public ushort Port { get; protected set; }
//        public MyReplicationLayer ReplicationLayer { get; protected set; }

//        protected MyRakNetPeer(bool isServer)
//        {
//            IsServer = isServer;

//            m_receiveQueue = new MyConcurrentQueue<Packet>();
//            m_peer = new RakPeer(null);

//            RegisterHandlers();

//            m_datagramHandler = new IncomingDatagramEventHandler(OnDatagramReceived);

//            if (APPLY_NETWORK_SIMULATOR)
//            {
//                //ApplyNetworkSimulator(MAX_OUTGOING_BITS_BANDWIDTH_PER_CONNECTION, PACKET_LOSS, MIN_EXTRA_PING, EXTRA_PING_VARIANCE);
//            }
//        }

//        public virtual void Dispose()
//        {
//            m_sendStream.Dispose();
//            m_receiveStream.Dispose();

//            //if (m_timer != null)
//            //{
//            //    lock (m_timer)
//            //    {
//            //        // TODO: this should not be called from finalizer ever (it's not safe to touch any reference member)
//            //        if (!m_peer.IsNull)
//            //        {
//            //            Packet packet;
//            //            while (m_receiveQueue.TryDequeue(out packet))
//            //            {
//            //                m_peer.DeallocatePacket(packet);
//            //            }
//            //        }
//            //        m_timer.Dispose();
//            //        m_timer = null;
//            //        m_timerAction = null;
//            //    }
//            //}

//            if (!m_peer.IsNull)
//            {
//                m_peer.SetIncomingDatagramEventHandler(IntPtr.Zero);
//                m_peer.Shutdown(300);
//                m_peer.Delete();
//            }
//            GC.SuppressFinalize(this);
//        }

//        ~MyRakNetPeer()
//        {
//            Debug.Fail("RakNetPeer not disposed!");
//            Dispose();
//        }

//        public void Update()
//        {
//            Packet packet;
//            while (RecieveMessage(out packet))
//            {
//                bool contains = m_messageHandlers.ContainsKey(packet.MessageID);
//                Debug.Assert(contains, "Unhandled message: " + packet.MessageID.ToString());
//                if (contains)
//                {
//                    m_messageHandlers[packet.MessageID](packet);
//                }
//                m_peer.DeallocatePacket(packet);
//            }
//        }

//        protected MyClientEntry RegisterClient(RakNetGUID guid)
//        {
//            var clientIndex = m_freeClientIndexes.Dequeue();
//            var client = new MyClientEntry(guid, clientIndex);

//            m_clientIndexToClientEntry.Add(clientIndex, client);
//            m_endpointIdToClient.Add(client.EndpointId, client);

//            return client;
//        }

//        protected void UnregisterClient(MyClientEntry client)
//        {
//            Debug.Assert(m_clientIndexToClientEntry.ContainsKey(client.ClientIndex));
//            Debug.Assert(m_endpointIdToClient.ContainsKey(client.EndpointId));
//            Debug.Assert(!m_freeClientIndexes.Contains(client.ClientIndex));

//            m_freeClientIndexes.Enqueue(client.ClientIndex);

//            m_clientIndexToClientEntry.Remove(client.ClientIndex);
//            m_endpointIdToClient.Remove(client.EndpointId);
//        }

//        protected void UnregisterAllClients()
//        {
//            while (m_clientIndexToClientEntry.Count > 0)
//            {
//                UnregisterClient(m_clientIndexToClientEntry.First().Value);
//            }
//            Debug.Assert(m_clientIndexToClientEntry.Count == 0);
//            Debug.Assert(m_endpointIdToClient.Count == 0);
//        }

//        public MyClientEntry GetClientEntryFromClientIndex(int clientIndex)
//        {
//            bool contains = m_clientIndexToClientEntry.ContainsKey(clientIndex);
//            Debug.Assert(contains);
//            if (contains)
//            {
//                return m_clientIndexToClientEntry[clientIndex];
//            }
//            else
//            {
//                return null;
//            }
//        }

//        public MyClientEntry GetClientEntryFromEndpointId(EndpointId endpoint)
//        {
//            bool contains = m_endpointIdToClient.ContainsKey(endpoint);
//            Debug.Assert(contains);
//            if (contains)
//            {
//                return m_endpointIdToClient[endpoint];
//            }
//            else
//            {
//                return null;
//            }
//        }

//        public event IncomingDatagramEventHandler DatagramReceived
//        {
//            add { m_datagramHandlerList.Add(value); UpdateDatagramHandler(); }
//            remove { m_datagramHandlerList.Remove(value); UpdateDatagramHandler(); }
//        }

//        void UpdateDatagramHandler()
//        {
//            if (m_datagramHandlerList.Count > 0)
//                m_peer.SetIncomingDatagramEventHandler(System.Runtime.InteropServices.Marshal.GetFunctionPointerForDelegate(m_datagramHandler));
//            else
//                m_peer.SetIncomingDatagramEventHandler(IntPtr.Zero);
//        }

//        bool OnDatagramReceived(ref NativeDatagram datagram)
//        {
//            foreach (var handler in m_datagramHandlerList)
//            {
//                if (!handler(ref datagram))
//                    return false;
//            }
//            return true;
//        }

//        public Statistics GetStats()
//        {
//            return m_peer.GetStatistics(RakNetGUID.UNASSIGNED_RAKNET_GUID);
//        }

//        public Statistics GetStats(RakNetGUID guid)
//        {
//            return m_peer.GetStatistics(guid);
//        }

//        public int GetMTUSize(RakNetGUID guid)
//        {
//            return m_peer.GetMTUSize(guid);
//        }

//        //[Conditional("DEBUG")]
//        //private void ApplyNetworkSimulator(uint maxOutgoingBitsBandwidthPerConnection, float packetLoss, ushort minExtraPing, ushort extraPingVariance)
//        //{
//        //    m_peer.SetPerConnectionOutgoingBandwidthLimit(maxOutgoingBitsBandwidthPerConnection);
//        //    m_peer.ApplyNetworkSimulator(packetLoss, minExtraPing, extraPingVariance);
//        //}

//        protected void Startup(uint maxConnections, ushort port, string host, IMyPeerCallback callback)
//        {
//            StartupResultEnum result = m_peer.Startup(maxConnections, port, host);
//            if (result != StartupResultEnum.RAKNET_STARTED)
//                throw new MyRakNetStartupException("RakNet startup failed: " + result, result);

//            MaxClients = maxConnections;
//            m_freeClientIndexes = new Queue<int>((int)maxConnections);
//            Port = port;

//            for (int i = 0; i < maxConnections; i++)
//            {
//                m_freeClientIndexes.Enqueue(i);
//            }
//            m_clientIndexToClientEntry = new Dictionary<int, MyClientEntry>((int)maxConnections);
//            m_endpointIdToClient = new Dictionary<EndpointId, MyClientEntry>((int)maxConnections);

//            Callback = callback;

//            // TODO: Weird behavior
//            //m_timerAction = new Action(ReceiveMessageInternal);
//            //m_timer = new MyTimer(1, m_timerAction);
//            //m_timer.Start();
//        }

//        protected void AddIgnoredMessage(MessageIDEnum id)
//        {
//            m_ignoredMessages.Add(id);
//        }

//        protected void AddMessageHandlerAsync(MessageIDEnum id, Action<Packet> handler)
//        {
//            m_internalMessages.Add(id);
//            AddMessageHandler(id, handler);
//        }

//        protected void AddMessageHandler(MessageIDEnum id, Action<Packet> handler)
//        {
//            m_messageHandlers[id] = handler;
//        }

//        private void RegisterHandlers()
//        {
//            AddMessageHandler(MessageIDEnum.CONNECTION_LOST, ConnectionLost);

//            AddMessageHandler(MessageIDEnum.STATE_DATA_FULL_REQUEST, StateDataFullRequest);
//            AddMessageHandler(MessageIDEnum.STATE_SYNC, StateSync);
//            AddMessageHandler(MessageIDEnum.EVENT, OnEvent);
//        }

//        private void StateDataFullRequest(Packet packet)
//        {
//            //BitStream bs = new BitStream(MessageIDEnum.STATE_DATA_FULL);
//            //ReplicationLayer.FullReplication(bs);
//        }

//        private void OnEvent(Packet packet)
//        {
//            ReplicationLayer.ProcessEvent(packet);
//        }

//        private void StateSync(Packet packet)
//        {
//            ReplicationLayer.ProcessStateData(packet);
//        }

//        protected virtual void OnClientJoined(EndpointId endpoint)
//        {
//            Callback.OnClientJoined(endpoint);
//        }

//        protected virtual void OnClientLeft(EndpointId endpoint)
//        {
//            Callback.OnClientLeft(endpoint);
//        }

//        protected virtual void OnConnectionLost(EndpointId endpoint)
//        {
//            Callback.OnConnectionLost(endpoint);
//        }

//        private void ConnectionLost(Packet packet)
//        {
//            var ep = packet.GUID.ToEndpoint();
//            if (!m_endpointIdToClient.ContainsKey(ep))
//                return;

//            var client = GetClientEntryFromEndpointId(ep);

//            UnregisterClient(client);
//            OnConnectionLost(ep);
//            OnClientLeft(ep);
//        }

//        private void ReceiveMessageInternal()
//        {
//            //if (m_timer == null)
//            //    return;

//            //lock (m_timer)
//            {
//                Packet packet;
//                while (m_peer.Receive(out packet))
//                {
//                    MyRakNetLogger.OnReceive(this, packet, m_ignoredMessages.Contains(packet.MessageID));
//                    if (m_ignoredMessages.Contains(packet.MessageID))
//                    {
//                        m_peer.DeallocatePacket(packet);
//                    }
//                    else if (IsInternal(packet.MessageID))
//                    {
//                        ProcessInternal(packet);
//                        m_peer.DeallocatePacket(packet);
//                    }
//                    else
//                    {
//                        m_receiveQueue.Enqueue(packet);
//                    }
//                }
//            }
//        }

//        private bool IsInternal(MessageIDEnum msgID)
//        {
//            //if (msgID <= MessageIDEnum.RESERVED_9)
//            //    return true;
//            return m_internalMessages.Contains(msgID);
//        }

//        private void ProcessInternal(Packet packet)
//        {
//            bool contains = m_messageHandlers.ContainsKey(packet.MessageID);
//            Debug.Assert(contains, "Unhandled internal message: " + packet.MessageID.ToString());
//            if (contains)
//            {
//                m_messageHandlers[packet.MessageID](packet);
//            }
//        }

//        public uint SendMessage(BitStream bs, RakNetGUID recipent, PacketReliabilityEnum reliability = PacketReliabilityEnum.UNRELIABLE, PacketPriorityEnum priority = PacketPriorityEnum.LOW_PRIORITY, MyChannelEnum channel = MyChannelEnum.Default)
//        {
//            return SendMessage(bs, priority, reliability, channel, recipent, false);
//        }

//        /// <summary>
//        /// Sends message, when broadcasting, recipient is peer who won't receive the message.
//        /// </summary>
//        internal uint SendMessage(BitStream bs, PacketPriorityEnum priority, PacketReliabilityEnum reliability, MyChannelEnum channel, RakNetGUID recipent, bool broadcast)
//        {
//            Debug.Assert(broadcast || recipent != RakNetGUID.UNASSIGNED_RAKNET_GUID, "Sending non-broadcast message, but recipient not specified");
//            Debug.Assert(bs.BytePosition > 0, "no data");
//            MyRakNetLogger.OnSend(this, bs.DataPointer, bs.BytePosition, priority, reliability, (MyChannelEnum)channel, recipent, broadcast);
//            uint ret = m_peer.Send(bs.DataPointer, bs.BytePosition, priority, reliability, (byte)channel, recipent, broadcast);
//            Debug.Assert(ret != 0, "bad input?");
//            return ret;
//        }

//        private bool RecieveMessage(out Packet msg)
//        {
//            ReceiveMessageInternal();
//            return m_receiveQueue.TryDequeue(out msg);
//        }

//        public void SendRaw(ref NativeDatagram data)
//        {
//            m_peer.SendRaw(ref data);
//        }
//    }
//}
