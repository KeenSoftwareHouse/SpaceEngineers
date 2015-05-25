using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using VRage.Utils;

namespace VRage.Network
{
    public class MyRakNetClient : MyRakNetPeer
    {
        public override bool IsServer { get { return false; } }
        public override ulong ServerId { get; protected set; }
        public override ulong MySteamID { get; protected set; }

        public MyRakNetClient(ulong steamID)
            : base(steamID)
        {
            RegisterHandlers();
        }

        public StartupResultEnum Startup(ushort port)
        {
            return base.Startup(1, port, null);
        }

        public event Action OnConnectionAttemptFailed;
        public event Action OnConnectionBanned;
        public event Action OnInvalidPassword;
        public event Action<List<ulong>> OnModListRecieved;
        public event Action OnAlreadyConnected;
        public event Action<uint, uint, uint> OnStateDataDownloadProgress;
        public event Action OnDisconnectionNotification;

        private void RegisterHandlers()
        {
            AddMessageHandler(MessageIDEnum.CONNECTION_ATTEMPT_FAILED, ConnectionAttemptFailed);
            AddMessageHandler(MessageIDEnum.CONNECTION_BANNED, ConnectionBanned);
            AddMessageHandler(MessageIDEnum.INVALID_PASSWORD, InvalidPassword);
            AddMessageHandler(MessageIDEnum.ALREADY_CONNECTED, AlreadyConnected);
            AddMessageHandler(MessageIDEnum.DISCONNECTION_NOTIFICATION, DisconnectionNotification);

            AddMessageHandler(MessageIDEnum.CONNECTION_REQUEST_ACCEPTED, SendClientData);
            AddMessageHandler(MessageIDEnum.SERVER_DATA, ServerData);
            AddMessageHandler(MessageIDEnum.STATE_DATA, StateData);
            AddMessageHandler(MessageIDEnum.DOWNLOAD_PROGRESS, DownloadProgress);

            AddMessageHandler(MessageIDEnum.REMOTE_CLIENT_CONNECTED, RemoteClientConnected);
            AddMessageHandler(MessageIDEnum.REMOTE_CLIENT_DISCONNECTED, RemoteClientDisconnected);
        }

        private void DisconnectionNotification(Packet packet)
        {
            //ulong steamID = m_GUIDToSteamID[packet.GUID.g];

            var handler = OnDisconnectionNotification;
            if (handler != null)
                handler();

            foreach (var guid in m_steamIDToGUID.Values)
            {
                guid.Delete();
            }
            m_steamIDToGUID.Clear();
            m_GUIDToSteamID.Clear();
        }

        private void RemoteClientDisconnected(Packet packet)
        {
            long tmpLong;
            bool success = packet.Data.Read(out tmpLong);
            Debug.Assert(success, "Failed to read remote client disconnected steamID");

            ulong steamID = (ulong)tmpLong;

            RaiseOnClientLeft(steamID);
        }

        private void RemoteClientConnected(Packet packet)
        {
            long tmpLong;
            bool success = packet.Data.Read(out tmpLong);
            Debug.Assert(success, "Failed to read remote client connected steamID");

            ulong steamID = (ulong)tmpLong;

            RaiseOnClientJoined(steamID);
        }

        private void DownloadProgress(Packet packet)
        {
            BitStream bs = packet.Data;
            bool success;
            byte[] tmp = new byte[4 * 3];

            uint progress;
            uint total;
            uint partLength;

            success = bs.Read(tmp, 4 * 3);
            Debug.Assert(success, "Failed to read download progress");
            progress = BitConverter.ToUInt32(tmp, 0);
            total = BitConverter.ToUInt32(tmp, 4);
            partLength = BitConverter.ToUInt32(tmp, 8);

            byte msgID;
            success = bs.Read(out msgID);
            Debug.Assert(success, "Failed to read message ID");

            if ((MessageIDEnum)msgID == MessageIDEnum.STATE_DATA)
            {
                var handler = OnStateDataDownloadProgress;
                if (handler != null)
                    handler(progress, total, partLength);
            }
        }

        private void StateData(Packet packet)
        {
            m_peer.SetSplitMessageProgressInterval(0);

            MyRakNetSyncLayer.Static.DeserializeStateData(packet.Data);

            SendClientReady();
        }

        private void SendClientReady()
        {
            BitStream bs = new BitStream(null);
            bs.Write((byte)MessageIDEnum.CLIENT_READY);
            SendMessageToServer(bs, PacketPriorityEnum.IMMEDIATE_PRIORITY, PacketReliabilityEnum.RELIABLE);
        }

        private void AlreadyConnected(Packet packet)
        {
            var handler = OnAlreadyConnected;
            if (handler != null)
                handler();
        }

        private void ServerData(Packet packet)
        {
            BitStream bs = packet.Data;
            bool success;
            int count;
            long tmpLong;
            byte[] tmpGUID = new byte[16];

            success = bs.Read(out count);
            Debug.Assert(success, "Failed to read client count");
            List<ulong> clients = new List<ulong>(count);

            for (int i = 0; i < count; i++)
            {
                success = bs.Read(out tmpLong);
                Debug.Assert(success, "Failed to read client SteamID " + (i + 1) + "/" + count);
                clients.Add((ulong)tmpLong);
            }

            success = bs.Read(out tmpLong);
            Debug.Assert(success, "Failed to read serverID");
            ServerId = (ulong)tmpLong;

            var guid = new RakNetGUID(packet.GUID);
            m_steamIDToGUID.Add(ServerId, guid);
            m_GUIDToSteamID.Add(guid.G, ServerId);

            RaiseOnClientJoined(ServerId);

            success = bs.Read(out count);
            Debug.Assert(success, "Failed to read mods count");
            List<ulong> mods = new List<ulong>(count);

            for (int i = 0; i < count; i++)
            {
                success = bs.Read(out tmpLong);
                Debug.Assert(success, "Failed to read mod " + (i + 1) + "/" + count);
                mods.Add((ulong)tmpLong);
            }

            var handler = OnModListRecieved;
            if (handler != null)
                handler(mods);

            success = bs.Read(out count);
            Debug.Assert(success, "Failed to read type table count");
            List<Guid> types = new List<Guid>(count);

            for (int i = 0; i < count; i++)
            {
                success = bs.Read(tmpGUID, 16);
                Debug.Assert(success, "Failed to read type table GUID " + (i + 1) + "/" + count);
                types.Add(new Guid(tmpGUID));
            }

            MyRakNetSyncLayer.Static.SetTypeTable(types);

            SendStateDataRequest();
        }

        private void SendStateDataRequest()
        {
            m_peer.SetSplitMessageProgressInterval(1); // MTU
            BitStream bs = new BitStream(null);
            bs.Write((byte)MessageIDEnum.STATE_DATA_REQUEST);
            SendMessageToServer(bs, PacketPriorityEnum.IMMEDIATE_PRIORITY, PacketReliabilityEnum.RELIABLE);
        }

        private void InvalidPassword(Packet packet)
        {
            var handler = OnInvalidPassword;
            if (handler != null)
                handler();
        }

        private void ConnectionBanned(Packet packet)
        {
            var handler = OnConnectionBanned;
            if (handler != null)
                handler();
        }

        private void ConnectionAttemptFailed(Packet packet)
        {
            var handler = OnConnectionAttemptFailed;
            if (handler != null)
                handler();
        }

        private void SendClientData(Packet packet)
        {
            BitStream bs = new BitStream(null);
            bs.Write((byte)MessageIDEnum.CLIENT_DATA);
            bs.Write((long)MySteamID);
            bs.Write((int)Version);
            SendMessageToServer(bs, PacketPriorityEnum.IMMEDIATE_PRIORITY, PacketReliabilityEnum.RELIABLE);
        }

        protected override void ChatMessage(Packet packet)
        {
            string message;
            long tmpLong;
            bool success = packet.Data.Read(out tmpLong);
            Debug.Assert(success, "Failed to read chat message sender steamID");

            ulong senderSteamID = (ulong)tmpLong;

            success = packet.Data.ReadCompressed(out message);
            Debug.Assert(success, "Failed to read chat message string");

            RiseOnChatMessage(senderSteamID, message);
        }

        public override void SendChatMessage(string message)
        {
            BitStream bs = new BitStream(null);

            bs.Write((byte)MessageIDEnum.CHAT_MESSAGE);
            bs.WriteCompressed(message);

            SendMessageToServer(bs, PacketPriorityEnum.IMMEDIATE_PRIORITY, PacketReliabilityEnum.RELIABLE_ORDERED);
        }

        public uint SendMessageToServer(BitStream RnBitStream, PacketPriorityEnum priority = PacketPriorityEnum.LOW_PRIORITY, PacketReliabilityEnum reliability = PacketReliabilityEnum.UNRELIABLE, byte channel = 0)
        {
            Debug.Assert(RnBitStream.GetNumberOfBytesUsed() > 0, "no data");
            uint ret = m_peer.Send(RnBitStream, priority, reliability, channel, RakNetGUID.UNASSIGNED_RAKNET_GUID, true);
            Debug.Assert(ret != 0, "bad input?");
            return ret;
        }

        public ConnectionAttemptResultEnum Connect(string ip, ushort port, string password = "")
        {
            return m_peer.Connect(ip, port, password);
        }
    }
}
