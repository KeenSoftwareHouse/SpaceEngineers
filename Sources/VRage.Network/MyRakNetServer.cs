//using System;
//using System.Collections.Generic;
//using System.Diagnostics;
//using System.Linq;
//using System.Text;
//using VRage.Library.Collections;
//using VRageMath;

//namespace VRage.Network
//{
//    public delegate void ValidationDelegate(EndpointId endpoint, byte[] clientData);

//    public class MyRakNetServer : MyRakNetPeer
//    {
//        List<Vector3D> m_areasOfInterestTmp = new List<Vector3D>();

//        public List<ulong> Mods = new List<ulong>();
//        public MyReplicationServer ReplicationServer { get { return (MyReplicationServer)ReplicationLayer; } }

//        protected new IMyServerCallback Callback { get { return (IMyServerCallback)base.Callback; } }

//        public MyRakNetServer()
//            : base(true)
//        {
//            ReplicationLayer = new MyReplicationServer(this);
//            RegisterHandlers();
//        }

//        public override void Dispose()
//        {
//            base.Dispose();
//            ReplicationLayer.Dispose();
//        }

//        public void Startup(uint maxConnections, ushort port, string host, IMyServerCallback callback)
//        {
//            m_peer.SetMaximumIncomingConnections((ushort)maxConnections);
//            base.Startup(maxConnections, port, host, callback);
//        }

//        private void RegisterHandlers()
//        {
//            AddIgnoredMessage(MessageIDEnum.NEW_INCOMING_CONNECTION);

//            AddMessageHandler(MessageIDEnum.CLIENT_DATA, ClientData);
//            AddMessageHandler(MessageIDEnum.STATE_DATA_FULL_REQUEST, StateDataFullRequest);
//            AddMessageHandler(MessageIDEnum.CLIENT_READY, ClientReady);
//            AddMessageHandler(MessageIDEnum.DISCONNECTION_NOTIFICATION, DisconnectionNotification);
//            AddMessageHandler(MessageIDEnum.CLIENT_UPDATE, ClientUpdate);

//            AddMessageHandlerAsync(MessageIDEnum.SND_RECEIPT_ACKED, SendReceiptAcked);
//            AddMessageHandlerAsync(MessageIDEnum.SND_RECEIPT_LOSS, SendReceiptLoss);

//            // hacks
//            AddMessageHandler(MessageIDEnum.STEAM_HACK_WORLD_REQUEST, WorldRequest);
//        }

//        private void WorldRequest(Packet packet)
//        {
//            m_sendStream.ResetWrite(MessageIDEnum.STEAM_HACK_WORLD_RESULT);

//            Callback.OnRequestWorld(packet.GUID.ToEndpoint(), m_sendStream);

//            SendMessage(m_sendStream, packet.GUID, PacketReliabilityEnum.RELIABLE_ORDERED, PacketPriorityEnum.IMMEDIATE_PRIORITY);

//            StartReplicating(packet.GUID.ToEndpoint());
//        }

//        private void ClientUpdate(Packet packet)
//        {
//            m_receiveStream.SetPacket(packet);

//            ReplicationServer.SetClientState(packet.GUID.ToEndpoint(), m_receiveStream);
//        }

//        private void SendReceiptLoss(Packet packet)
//        {
//            // TODO:SK endianity is probably wrong here
//            m_receiveStream.SetPacket(packet);
//            uint packetID = m_receiveStream.ReadUInt32();
//            //MyRakNetSyncLayer.Static.ProcessSendReceiptLoss(packetID);
//        }

//        private void SendReceiptAcked(Packet packet)
//        {
//            // TODO:SK endianity is probably wrong here
//            m_receiveStream.SetPacket(packet);
//            uint packetID = m_receiveStream.ReadUInt32();
//            //MyRakNetSyncLayer.Static.ProcessSendReceiptAcked(packetID);
//        }

//        private void DisconnectionNotification(Packet packet)
//        {
//            MyClientEntry client;
//            if (m_endpointIdToClient.TryGetValue(packet.GUID.ToEndpoint(), out client))
//            {
//                UnregisterClient(client);
//                OnClientLeft(packet.GUID.ToEndpoint());
//            }
//            else
//            {
//                Debug.Fail("DisconnectionNotification, client not found");
//            }
//        }

//        protected override void OnClientLeft(EndpointId endpoint)
//        {
//            ReplicationServer.OnClientLeft(endpoint);
//            base.OnClientLeft(endpoint);
//        }

//        private void ClientReady(Packet packet)
//        {
//            MyClientEntry client;
//            if (m_endpointIdToClient.TryGetValue(packet.GUID.ToEndpoint(), out client))
//            {
//                Callback.OnClientReady(client.EndpointId);
//            }
//            else
//            {
//                Debug.Fail("ClientReady, client not found");
//            }
//        }

//        private void StartReplicating(EndpointId endpoint)
//        {
//            MyClientEntry client;
//            if (m_endpointIdToClient.TryGetValue(endpoint, out client))
//            {
//                var clientState = Callback.CreateClientState();
//                clientState.EndpointId = client.EndpointId;
//                ReplicationServer.OnClientJoined(client.EndpointId, clientState);
//            }
//            else
//            {
//                Debug.Fail("StartReplicating, client not found");
//            }
//        }

//        private void StateDataFullRequest(Packet packet)
//        {
//            MyClientEntry client;
//            if (m_endpointIdToClient.TryGetValue(packet.GUID.ToEndpoint(), out client))
//            {
//                m_sendStream.ResetWrite(MessageIDEnum.STATE_DATA_FULL);
//                Callback.OnRequestStateData(client.EndpointId, m_sendStream);

//                SendMessage(m_sendStream, packet.GUID, PacketReliabilityEnum.RELIABLE_ORDERED, PacketPriorityEnum.IMMEDIATE_PRIORITY);
//            }
//            else
//            {
//                Debug.Fail("StateDataFullRequest, client not found");
//            }
//        }

//        private void ClientData(Packet packet)
//        {
//            m_receiveStream.SetPacket(packet);

//            // already connected
//            // TODO:SK handle properly on the client
//            if (m_endpointIdToClient.ContainsKey(packet.GUID.ToEndpoint()))
//            {
//                m_peer.CloseConnection(packet.GUID, true);
//                return;
//            }

//            Callback.ValidateUser(packet.GUID.ToEndpoint(), m_receiveStream);
//        }

//        public void ValidationSuccessful(EndpointId endpoint)
//        {
//            var guid = new RakNetGUID(endpoint.Value);
//            var client = RegisterClient(guid);
//            OnClientJoined(endpoint);
//            SendServerData(client);
//        }

//        public void ValidationUnsuccessful(EndpointId endpoint)
//        {
//            var guid = new RakNetGUID(endpoint.Value);
//            m_peer.CloseConnection(guid, true);
//        }

//        private void SendServerData(MyClientEntry client)
//        {
//            m_sendStream.ResetWrite(MessageIDEnum.SERVER_DATA);
//            ReplicationLayer.SerializeTypeTable(m_sendStream);

//            Callback.OnRequestServerData(client.EndpointId, m_sendStream);

//            SendMessage(m_sendStream, client.GUID, PacketReliabilityEnum.RELIABLE, PacketPriorityEnum.IMMEDIATE_PRIORITY);
//        }

//        internal uint BroadcastMessage(BitStream bs, PacketReliabilityEnum reliability = PacketReliabilityEnum.UNRELIABLE, PacketPriorityEnum priority = PacketPriorityEnum.LOW_PRIORITY, MyChannelEnum channel = MyChannelEnum.Default, RakNetGUID? exclude = null)
//        {
//            return SendMessage(bs, priority, reliability, channel, exclude ?? RakNetGUID.UNASSIGNED_RAKNET_GUID, true);
//        }

//        public bool Kick(EndpointId endpoint, bool sendNotification = true)
//        {
//            MyClientEntry client;
//            if (m_endpointIdToClient.TryGetValue(endpoint, out client))
//            {
//                m_peer.CloseConnection(client.GUID, sendNotification);

//                UnregisterClient(client);
//                OnClientLeft(endpoint);
//                return true;
//            }
//            return false;
//        }
//    }
//}
