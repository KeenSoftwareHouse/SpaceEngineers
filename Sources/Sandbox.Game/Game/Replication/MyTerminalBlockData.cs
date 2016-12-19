using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.Multiplayer;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using VRage;
using VRage.Collections;
using VRage.Library.Collections;
using VRage.Network;
using VRage.Sync;

namespace Sandbox.Game.Replication
{
    class MyTerminalBlockData
    {
        // Per-client data
        class ClientData
        {
            public SmallBitField DirtyProperties = new SmallBitField(false); // Nothing dirty by default (transferred in object builder)

            // Properties sent in last state sync
            public SmallBitField SentProperties = new SmallBitField(false);
            public byte? PacketId;
        }

        /// <summary>
        /// Client stores here dirty properties.
        /// </summary>
        private SmallBitField m_dirtyProperties;
        private uint m_lastUpdateFrame;

        private Dictionary<MyClientStateBase, ClientData> m_clientData = new Dictionary<MyClientStateBase, ClientData>();
        private ListReader<SyncBase> m_properties;
        private Action<MyTerminalBlockData> m_propertyDirty;

        public readonly MySyncedBlock Block;

        public MyTerminalBlockData(MySyncedBlock block, Action<MyTerminalBlockData> propertyDirty)
        {
            Block = block;
            Block.SyncType.PropertyChanged += Notify;
            m_properties = Block.SyncType.Properties;
            m_propertyDirty = propertyDirty;
        }

        public void Destroy()
        {
            Block.SyncType.PropertyChanged -= Notify;
        }

        void Notify(SyncBase sync)
        {
            if (sync != null)
            {
                if (Sync.IsServer)
                {
                    foreach (var data in m_clientData)
                    {
                        data.Value.DirtyProperties[sync.Id] = true;
                    }
                }
                else
                {
                    m_dirtyProperties[sync.Id] = true;
                }
                m_propertyDirty(this);
            }
        }

        public bool IsDirty(MyClientStateBase forClient)
        {
            return m_clientData[forClient].DirtyProperties.Bits != 0;
        }

        public void CreateClientData(MyClientStateBase forClient)
        {
            m_clientData.Add(forClient, new ClientData());
        }

        public void DestroyClientData(MyClientStateBase forClient)
        {
            m_clientData.Remove(forClient);
        }

        public void Serialize(BitStream stream, MyClientStateBase forClient, byte packetId, int maxBitPosition)
        {
            SmallBitField dirtyFlags;
            if (stream.Writing)
            {
                var data = m_clientData[forClient];
                dirtyFlags = data.DirtyProperties;
                stream.WriteUInt64(dirtyFlags.Bits, m_properties.Count);
            }
            else
            {
                dirtyFlags.Bits = stream.ReadUInt64(m_properties.Count);
            }

            for (int i = 0; i < m_properties.Count; i++)
            {
                if (dirtyFlags[i])
                {
                    m_properties[i].Serialize(stream, false); // Received from server, don't validate
                    if (stream.Reading) // Received from server, it's no longer dirty
                        m_dirtyProperties[i] = false;
                }
            }
            if (stream.Writing && stream.BitPosition <= maxBitPosition)
            {
                var data = m_clientData[forClient];
                data.PacketId = packetId;
                data.SentProperties.Bits = data.DirtyProperties.Bits;
                data.DirtyProperties.Bits = 0;
            }
        }

        public void OnAck(MyClientStateBase forClient, byte packetId, bool delivered)
        {
            var data = m_clientData[forClient];
            Debug.Assert(data.PacketId == packetId, "Packet ID does not match, error in replication server, reporting ACK for something invalid");

            if (delivered)
            {
                data.SentProperties.Bits = 0; // Nothing sent now
            }
            else
            {
                // Not delivered, add sent to dirty
                data.DirtyProperties.Bits |= data.SentProperties.Bits;
            }
            data.PacketId = null;
        }
    }
}
