using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace VRage.Network
{
    public class MyRakNetServer : MyRakNetPeer
    {
        public override bool IsServer { get { return true; } }
        public override ulong ServerId { get; protected set; }
        public override ulong MySteamID { get { return ServerId; } protected set { ServerId = value; } }

        private List<ulong> m_mods = new List<ulong>();
        public List<ulong> Mods
        {
            get { return m_mods; }
            set { m_mods = value; }
        }

        public MyRakNetServer(ulong steamID)
            : base(steamID)
        {
            RegisterHandlers();
            RegisterEvents();
        }

        public new StartupResultEnum Startup(uint maxConnections, ushort port, string host)
        {
            m_peer.SetMaximumIncomingConnections((ushort)maxConnections);
            return base.Startup(maxConnections, port, host);
        }

        public event Action<ulong> OnRequestStateData;
        public event Action<ulong> OnClientReady;

        private void RegisterHandlers()
        {
            AddIgnoredMessage(MessageIDEnum.NEW_INCOMING_CONNECTION);

            AddMessageHandler(MessageIDEnum.CLIENT_DATA, ClientData);
            AddMessageHandler(MessageIDEnum.STATE_DATA_REQUEST, StateDataRequest);
            AddMessageHandler(MessageIDEnum.CLIENT_READY, ClientReady);
            AddMessageHandler(MessageIDEnum.DISCONNECTION_NOTIFICATION, DisconnectionNotification);
        }

        private void RegisterEvents()
        {
            OnConnectionLost += SendClientDisconnected;
        }

        private void DisconnectionNotification(Packet packet)
        {
            Debug.Assert(m_GUIDToSteamID.ContainsKey(packet.GUID.G));
            ulong steamID = m_GUIDToSteamID[packet.GUID.G];

            m_steamIDToGUID[steamID].Delete();
            m_steamIDToGUID.Remove(steamID);
            m_GUIDToSteamID.Remove(packet.GUID.G);

            SendClientDisconnected(steamID);

            RaiseOnClientLeft(steamID);
        }

        private void ClientReady(Packet packet)
        {
            ulong steamID = m_GUIDToSteamID[packet.GUID.G];

            var handler = OnClientReady;
            if (handler != null)
                handler(steamID);
        }

        private void StateDataRequest(Packet packet)
        {
            ulong steamID = m_GUIDToSteamID[packet.GUID.G];

            BitStream bs = new BitStream(null);
            bs.Write((byte)MessageIDEnum.STATE_DATA);

            MyRakNetSyncLayer.Static.SerializeStateData(steamID, bs);

            var handler = OnRequestStateData;
            if (handler != null)
                handler(steamID);

            SendMessage(bs, packet.GUID, PacketPriorityEnum.IMMEDIATE_PRIORITY, PacketReliabilityEnum.RELIABLE_ORDERED);
        }

        private void ClientData(Packet packet)
        {
            BitStream bs = packet.Data;
            long tmpLong;
            bool success = bs.Read(out tmpLong);
            Debug.Assert(success, "Failed to read steamID");
            ulong steamID = (ulong)tmpLong;

            // already connected
            // TODO:SK handle properly (who to disconnect?)
            if (m_steamIDToGUID.ContainsKey(steamID))
            {
                m_peer.CloseConnection(packet.GUID, false);
                return;
            }

            int version;
            success = bs.Read(out version);
            Debug.Assert(success, "Failed to read version");
            if (version != Version)
            {
                m_peer.CloseConnection(packet.GUID, true);
                return;
            }

            SendServerData(packet.GUID);

            var guid = new RakNetGUID(packet.GUID);
            m_steamIDToGUID.Add(steamID, guid);
            m_GUIDToSteamID.Add(guid.G, steamID);

            SendClientConnected(steamID, packet.GUID);

            RaiseOnClientJoined(steamID);
        }

        private void SendClientConnected(ulong steamID, RakNetGUID exclude)
        {
            BitStream bs = new BitStream(null);
            bs.Write((byte)MessageIDEnum.REMOTE_CLIENT_CONNECTED);
            bs.Write((long)steamID);
            BroadcastMessage(bs, PacketPriorityEnum.HIGH_PRIORITY, PacketReliabilityEnum.RELIABLE_ORDERED, 0, exclude);
        }

        private void SendClientDisconnected(ulong steamID)
        {
            BitStream bs = new BitStream(null);
            bs.Write((byte)MessageIDEnum.REMOTE_CLIENT_DISCONNECTED);
            bs.Write((long)steamID);
            BroadcastMessage(bs, PacketPriorityEnum.HIGH_PRIORITY, PacketReliabilityEnum.RELIABLE_ORDERED, 0, RakNetGUID.UNASSIGNED_RAKNET_GUID);
        }

        private void SendServerData(RakNetGUID recipent)
        {
            BitStream bs = new BitStream(null);
            bs.Write((byte)MessageIDEnum.SERVER_DATA);

            // connected client steamIDs
            bs.Write(m_steamIDToGUID.Count);
            foreach (var steamID in m_steamIDToGUID.Keys)
            {
                bs.Write((long)steamID);
            }

            // serverID
            bs.Write((long)ServerId);

            // Mods
            bs.Write(m_mods.Count);
            foreach (ulong modId in m_mods)
            {
                bs.Write((long)modId);
            }

            // type table
            var types = MyRakNetSyncLayer.Static.GetTypeTable();
            bs.Write(types.Count);
            foreach (var type in types)
            {
                bs.Write(type.GUID.ToByteArray(), 16);
            }

            SendMessage(bs, recipent, PacketPriorityEnum.IMMEDIATE_PRIORITY, PacketReliabilityEnum.RELIABLE);
        }

        protected override void ChatMessage(Packet packet)
        {
            string message;

            bool success = packet.Data.ReadCompressed(out message);
            Debug.Assert(success, "Failed to read chat message string");

            ulong steamID = m_GUIDToSteamID[packet.GUID.G];

            SendChatMessageInternal(steamID, message, packet.GUID);

            RiseOnChatMessage(steamID, message);
        }

        public override void SendChatMessage(string message)
        {
            SendChatMessageInternal(MySteamID, message);
        }

        private void SendChatMessageInternal(ulong steamID, string message, RakNetGUID? exclude = null)
        {
            BitStream bs = new BitStream(null);

            bs.Write((byte)MessageIDEnum.CHAT_MESSAGE);
            bs.Write((long)steamID);
            bs.WriteCompressed(message);

            BroadcastMessage(bs, PacketPriorityEnum.IMMEDIATE_PRIORITY, PacketReliabilityEnum.RELIABLE_ORDERED, 0, exclude);
        }

        public uint BroadcastMessage(BitStream data, PacketPriorityEnum priority = PacketPriorityEnum.LOW_PRIORITY, PacketReliabilityEnum reliability = PacketReliabilityEnum.UNRELIABLE, byte channel = 0, RakNetGUID? exclude = null)
        {
            exclude = exclude.HasValue ? exclude.Value : RakNetGUID.UNASSIGNED_RAKNET_GUID;
            Debug.Assert(data.GetNumberOfBytesUsed() > 0, "no data");
            uint ret = m_peer.Send(data, priority, reliability, channel, exclude.Value, true);
            Debug.Assert(ret != 0, "bad input?");
            return ret;
        }
    }
}
