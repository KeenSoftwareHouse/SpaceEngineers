//using System;
//using System.Collections.Generic;
//using System.Diagnostics;
//using System.Linq;
//using System.Net;
//using System.Text;
//using VRage.Library.Collections;
//using VRage.Utils;
//using VRageMath;

//namespace VRage.Network
//{
//    public class MyRakNetClient : MyRakNetPeer
//    {
//        public MyReplicationClient ReplicationClient { get { return (MyReplicationClient)ReplicationLayer; } }
//        protected new IMyClientCallback Callback { get { return (IMyClientCallback)base.Callback; } }

//        public MyRakNetClient()
//            : base(false)
//        {
//            ReplicationLayer = new MyReplicationClient(this);
//            RegisterHandlers();
//        }

//        public override void Dispose()
//        {
//            base.Dispose();
//            ReplicationLayer.Dispose();
//        }

//        public void Startup(ushort port, IMyClientCallback callback)
//        {
//            base.Startup(1, port, null, callback);
//        }

//        private void RegisterHandlers()
//        {
//            AddMessageHandler(MessageIDEnum.CONNECTION_ATTEMPT_FAILED, ConnectionAttemptFailed);
//            AddMessageHandler(MessageIDEnum.CONNECTION_BANNED, ConnectionBanned);
//            AddMessageHandler(MessageIDEnum.INVALID_PASSWORD, InvalidPassword);
//            AddMessageHandler(MessageIDEnum.ALREADY_CONNECTED, AlreadyConnected);
//            AddMessageHandler(MessageIDEnum.DISCONNECTION_NOTIFICATION, DisconnectionNotification);

//            AddMessageHandler(MessageIDEnum.CONNECTION_REQUEST_ACCEPTED, SendClientData);
//            AddMessageHandler(MessageIDEnum.SERVER_DATA, ServerData);
//            AddMessageHandler(MessageIDEnum.STATE_DATA_FULL, StateDataFull);
//            AddMessageHandler(MessageIDEnum.DOWNLOAD_PROGRESS, DownloadProgress);

//            AddMessageHandler(MessageIDEnum.REPLICATION_CREATE, ReplicationCreate);
//            AddMessageHandler(MessageIDEnum.REPLICATION_DESTROY, ReplicationDestroy);

//            //hacks
//            AddMessageHandler(MessageIDEnum.STEAM_HACK_WORLD_RESULT, WorldRecieved);
//        }

//        private void DisconnectionNotification(Packet packet)
//        {
//            Callback.OnDisconnectionNotification();
//            UnregisterAllClients();
//        }

//        private void DownloadProgress(Packet packet)
//        {
//            m_receiveStream.SetPacket(packet, false);

//            uint progress = m_receiveStream.ReadUInt32();
//            uint total = m_receiveStream.ReadUInt32();
//            uint partLength = m_receiveStream.ReadUInt32();

//            MessageIDEnum msgID = m_receiveStream.ReadMessageId();

//            if (msgID == MessageIDEnum.STEAM_HACK_WORLD_RESULT)
//            {
//                Callback.OnStateDataDownloadProgress(progress, total, partLength);
//            }
//        }

//        private void WorldRecieved(Packet packet)
//        {
//            m_peer.SetSplitMessageProgressInterval(0);

//            m_receiveStream.SetPacket(packet);
//            Callback.OnWorldReceived(m_receiveStream);

//            SendClientReady();
//        }

//        private void StateDataFull(Packet packet)
//        {
//            m_peer.SetSplitMessageProgressInterval(0);

//            //MyRakNetSyncLayer.Static.DeserializeStateData(packet.Data);

//            SendClientReady();
//        }

//        private void SendClientReady()
//        {
//            m_sendStream.ResetWrite(MessageIDEnum.CLIENT_READY);
//            SendMessageToServer(m_sendStream, PacketReliabilityEnum.RELIABLE, PacketPriorityEnum.IMMEDIATE_PRIORITY);

//            ReplicationClient.OnLocalClientReady();
//            Callback.OnLocalClientReady();
//        }

//        private void AlreadyConnected(Packet packet)
//        {
//            Callback.OnAlreadyConnected();
//        }

//        private void ServerData(Packet packet)
//        {
//            m_receiveStream.SetPacket(packet);
//            ReplicationLayer.SerializeTypeTable(m_receiveStream);

//            var client = RegisterClient(packet.GUID);
//            OnClientJoined(client.EndpointId);

//            Callback.OnServerDataReceived(m_receiveStream);

//            //hack for now
//            //SendStateDataRequest();
//            SendWorldRequest();
//        }

//        private void SendWorldRequest()
//        {
//            m_peer.SetSplitMessageProgressInterval(1); // MTU
//            m_sendStream.ResetWrite(MessageIDEnum.STEAM_HACK_WORLD_REQUEST);
//            SendMessageToServer(m_sendStream, PacketReliabilityEnum.RELIABLE, PacketPriorityEnum.IMMEDIATE_PRIORITY);
//        }

//        private void SendStateDataRequest()
//        {
//            m_peer.SetSplitMessageProgressInterval(1); // MTU
//            m_sendStream.ResetWrite(MessageIDEnum.STATE_DATA_FULL_REQUEST);
//            SendMessageToServer(m_sendStream, PacketReliabilityEnum.RELIABLE, PacketPriorityEnum.IMMEDIATE_PRIORITY);
//        }

//        private void InvalidPassword(Packet packet)
//        {
//            Callback.OnInvalidPassword();
//        }

//        private void ConnectionBanned(Packet packet)
//        {
//            Callback.OnConnectionBanned();
//        }

//        private void ConnectionAttemptFailed(Packet packet)
//        {
//            Callback.OnConnectionAttemptFailed();
//        }

//        private void ReplicationDestroy(Packet packet)
//        {
//            ReplicationClient.ProcessReplicationDestroy(packet);
//        }

//        private void ReplicationCreate(Packet packet)
//        {
//            ReplicationClient.ProcessReplicationCreate(packet);
//        }

//        private void SendClientData(Packet packet)
//        {
//            m_sendStream.ResetWrite(MessageIDEnum.CLIENT_DATA);
//            Callback.OnSendClientData(m_sendStream);
//            SendMessageToServer(m_sendStream, PacketReliabilityEnum.RELIABLE, PacketPriorityEnum.IMMEDIATE_PRIORITY);
//        }
        
//        internal uint SendMessageToServer(BitStream bs, PacketReliabilityEnum reliability = PacketReliabilityEnum.UNRELIABLE, PacketPriorityEnum priority = PacketPriorityEnum.LOW_PRIORITY, MyChannelEnum channel = MyChannelEnum.Default)
//        {
//            return SendMessage(bs, priority, reliability, channel, RakNetGUID.UNASSIGNED_RAKNET_GUID, true);
//        }

//        public void Connect(string ip, ushort port, string password = "")
//        {
//            var result = m_peer.Connect(ip, port, password);
//            if(result != ConnectionAttemptResultEnum.CONNECTION_ATTEMPT_STARTED)
//            {
//                throw new MyRakNetConnectionException(string.Format("RakNet failed to start connecting: " + result), result);
//            }
//        }
//    }
//}
