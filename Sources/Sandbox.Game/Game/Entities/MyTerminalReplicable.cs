using Sandbox.Engine.Multiplayer;
using Sandbox.Engine.Physics;
using Sandbox.Engine.Utils;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.Gui;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.Replicables;
using Sandbox.Game.World;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using VRage;
using VRage.Collections;
using VRage.Library.Collections;
using VRage.Library.Sync;
using VRage.Network;
using VRage.Replication;
using VRage.Serialization;

namespace Sandbox.Game.Entities
{
    /// <summary>
    /// Responsible for synchronizing entity physics over network
    /// </summary>
    class MyTerminalReplicable : MyExternalReplicable<MyTerminalBlock>, IMyStateGroup, IMyProxyTarget
    {
        class ClientData
        {
            public SmallBitField DirtyProperties = new SmallBitField(true); // Dirty everything by default

            // Properties sent in last state sync
            public SmallBitField SentProperties = new SmallBitField(false);
            public byte? PacketId;
        }

        public MyTerminalBlock Block { get { return Instance; } }

        public IMyEventProxy Target { get { return Instance; } }

        /// <summary>
        /// Client stores here dirty properties.
        /// </summary>
        private SmallBitField m_dirtyProperties;
        private uint m_lastUpdateFrame;

        private Dictionary<MyClientStateBase, ClientData> m_clientData = new Dictionary<MyClientStateBase, ClientData>();
        private ListReader<SyncBase> m_properties;

        private IMyReplicable m_gridReplicable { get { return FindByObject(Block.CubeGrid); } }

        public StateGroupEnum GroupType { get { return StateGroupEnum.Terminal; } }

        protected override void OnHook()
        {
            Debug.Assert(MyMultiplayer.Static != null, "Should not get here without multiplayer");
            Block.SyncType.PropertyChanged += Notify;
            Block.OnClose += (entity) => RaiseDestroyed();
            m_properties = Block.SyncType.Properties;
        }

        void Notify(SyncBase sync)
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
        }

        public void CreateClientData(MyClientStateBase forClient)
        {
            m_clientData.Add(forClient, new ClientData());
        }

        public void DestroyClientData(MyClientStateBase forClient)
        {
            m_clientData.Remove(forClient);
        }

        public void ClientUpdate()
        {
            const int ClientUpdateSleepFrames = 6;

            // Don't sync more often then once per 6 frames (~100 ms)
            if (m_dirtyProperties.Bits != 0 && MyMultiplayer.Static.FrameCounter - m_lastUpdateFrame >= ClientUpdateSleepFrames)
            {
                foreach (var property in m_properties)
                {
                    if (m_dirtyProperties[property.Id])
                    {
                        MyMultiplayer.RaiseEvent(this, x => x.TerminalValueChanged_Implementation, (byte)property.Id, (BitReaderWriter)property);
                    }
                }
                m_dirtyProperties.Reset(false);
                m_lastUpdateFrame = MyMultiplayer.Static.FrameCounter;
            }
        }

        [Event, Reliable, Server]
        private void TerminalValueChanged_Implementation(byte propertyIndex, BitReaderWriter reader)
        {
            var state = MyEventContext.Current.ClientState;
            var client = state.GetClient();

            if (!HasRights(client))
                return;

            bool isValid = reader.ReadData(m_properties[propertyIndex], true);

            // Validation succeded, mark property as clean
            m_clientData[state].DirtyProperties[propertyIndex] = !isValid;
        }

        public float GetGroupPriority(int frameCountWithoutSync, MyClientStateBase client)
        {
            Debug.Assert(m_properties.Count > 0, "When no properties are defined, it should not get there");

            // Temporarily disabled
            //return 0;

            // Called only on server
            float priority = m_gridReplicable.GetPriority(client);
            if (priority <= 0)
                return 0;

            // TODO: Raise priority only when client is looking into terminal
            if (m_clientData[client].DirtyProperties.Bits > 0 && m_clientData[client].PacketId == null)
                return priority;
            else
                return 0;
        }

        public bool HasRights(MyNetworkClient client)
        {
            var relationToBlockOwner = Block.GetUserRelationToOwner(client.FirstPlayer.Identity.IdentityId);
            return relationToBlockOwner == Common.MyRelationsBetweenPlayerAndBlock.FactionShare || relationToBlockOwner == Common.MyRelationsBetweenPlayerAndBlock.Owner;
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

        #region IMyReplicable Implementation
        public override IMyReplicable GetDependency()
        {
            Debug.Assert(!Block.Closed && !Block.CubeGrid.Closed, "Sending terminal replicable on closed block/grid");
            return m_gridReplicable;
        }

        public override float GetPriority(MyClientStateBase client)
        {
            if (m_properties.Count == 0)
                return 0;

            // Same priority as grid
            return m_gridReplicable.GetPriority(client);
        }

        public override void OnSave(BitStream stream)
        {
            stream.WriteInt64(Block.EntityId);
        }

        protected override void OnLoad(BitStream stream, Action<MyTerminalBlock> loadingDoneHandler)
        {
            long blockEntityId = stream.ReadInt64();
            loadingDoneHandler((MyTerminalBlock)MyEntities.GetEntityById(blockEntityId));
        }

        public override void OnDestroy()
        {
        }

        public override void GetStateGroups(List<IMyStateGroup> resultList)
        {
            resultList.Add(this);
        }
        #endregion
    }
}
