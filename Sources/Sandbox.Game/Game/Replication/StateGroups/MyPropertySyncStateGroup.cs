using Sandbox.Engine.Multiplayer;
using Sandbox.Game.Multiplayer;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using VRage.Collections;
using VRage.Library.Collections;
using VRage.Library.Utils;
using VRage.Network;
using VRage.Profiler;
using VRage.Sync;

namespace Sandbox.Game.Replication.StateGroups
{
    /// <summary>
    /// Property sync state group.
    /// Synchronizes Sync members in SyncType.
    /// </summary>
    public class MyPropertySyncStateGroup : IMyStateGroup
    {
        public delegate float PriorityAdjustDelegate(int frameCountWithoutSync, MyClientStateBase clientState, float basePriority);

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

        private Dictionary<EndpointId , ClientData> m_clientData = new Dictionary<EndpointId, ClientData>();
        private ListReader<SyncBase> m_properties;

        public IMyReplicable Owner { get;  private set; }

        public Func<MyEventContext, bool> GlobalValidate = (context) => true;
        public PriorityAdjustDelegate PriorityAdjust = (frames, state, priority) => priority;

        public int PropertyCount { get { return m_properties.Count; } }
        public StateGroupEnum GroupType { get { return StateGroupEnum.Properties; } }

        public MyPropertySyncStateGroup(IMyReplicable ownerReplicable, SyncType syncType)
        {
            Owner = ownerReplicable;
            syncType.PropertyChanged += Notify;
            m_properties = syncType.Properties;
        }

        void Notify(SyncBase sync)
        {
            if (Owner != null && sync != null)
            {
                if (Sync.IsServer)
                {
                    foreach (var data in m_clientData)
                    {
                        if (data.Value != null)
                        {
                            data.Value.DirtyProperties[sync.Id] = true;
                        }
                    }

                    if (MyMultiplayer.GetReplicationServer() != null)
                        MyMultiplayer.GetReplicationServer().AddToDirtyGroups(this);
                }
                else
                {
                    m_dirtyProperties[sync.Id] = true;
                }
            }
        }

        /// <summary>
        /// Marks dirty for all clients.
        /// </summary>
        public void MarkDirty()
        {
            foreach(var client in m_clientData)
            {
                client.Value.DirtyProperties.Reset(true);
            }

            MyMultiplayer.GetReplicationServer().AddToDirtyGroups(this);
        }

        public void CreateClientData(MyClientStateBase forClient)
        {
            m_clientData.Add(forClient.EndpointId, new ClientData());
            m_clientData[forClient.EndpointId].DirtyProperties.Reset(true);
        }

        public void DestroyClientData(MyClientStateBase forClient)
        {
            m_clientData.Remove(forClient.EndpointId);
        }

        public void ClientUpdate(MyTimeSpan clientTimestamp)
        {
            const int ClientUpdateSleepFrames = 6;

            // Don't sync more often then once per 6 frames (~100 ms)
            if (m_dirtyProperties.Bits != 0 && MyMultiplayer.Static.FrameCounter - m_lastUpdateFrame >= ClientUpdateSleepFrames)
            {
                foreach (var property in m_properties)
                {
                    if (m_dirtyProperties[property.Id])
                    {
                        MyMultiplayer.RaiseEvent(this, x => x.SyncPropertyChanged_Implementation, (byte)property.Id, (BitReaderWriter)property);
                    }
                }
                m_dirtyProperties.Reset(false);
                m_lastUpdateFrame = MyMultiplayer.Static.FrameCounter;
            }
        }

        public void Destroy()
        {
            Owner = null;
        }

        [Event, Reliable, Server]
        private void SyncPropertyChanged_Implementation(byte propertyIndex, BitReaderWriter reader)
        {
            if (!GlobalValidate(MyEventContext.Current))
                return;

            if (propertyIndex >= m_properties.Count) // Client data validation
                return;

            bool isValid = reader.ReadData(m_properties[propertyIndex], true);

            // Validation succeeded, mark property as clean
            if (MyEventContext.Current.ClientState != null && m_clientData.ContainsKey(MyEventContext.Current.ClientState.EndpointId))
            {
                m_clientData[MyEventContext.Current.ClientState.EndpointId].DirtyProperties[propertyIndex] = !isValid;
            }
        }

        /// <summary>
        /// Gets group priority, when overloaded it can be useful to scale base priority.
        /// </summary>
        public virtual float GetGroupPriority(int frameCountWithoutSync, MyClientInfo client)
        {
            ProfilerShort.Begin("MyPropertySyncStateGroup::GetGroupPriority");

            if (m_properties.Count == 0)
            {
                ProfilerShort.End();
                return 0;
            }

            if (client.HasReplicable(Owner) == false)
            {
                ProfilerShort.End();
                return 0;
            }
            // Called only on server
            float priority = client.GetPriority(Owner);
            if (priority <= 0)
            {
                ProfilerShort.End();
                return 0;
            }

            float adjustedPriority = 0;
            if (m_clientData[client.State.EndpointId].DirtyProperties.Bits > 0 && m_clientData[client.State.EndpointId].PacketId == null)
                adjustedPriority = PriorityAdjust(frameCountWithoutSync, client.State, priority);

            ProfilerShort.End();

            return adjustedPriority;
        }

        public void Serialize(BitStream stream, EndpointId forClient, MyTimeSpan timestamp, byte packetId, int maxBitPosition)
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
            var data = m_clientData[forClient.EndpointId];
            Debug.Assert(data.PacketId == null || data.PacketId == packetId, "Packet ID does not match, error in replication server, reporting ACK for something invalid");
            
            if (delivered)
            {
                data.SentProperties.Bits = 0; // Nothing sent now
            }
            else
            {
                // Not delivered, add sent to dirty
                data.DirtyProperties.Bits |= data.SentProperties.Bits;
                MyMultiplayer.GetReplicationServer().AddToDirtyGroups(this);
            }
            data.PacketId = null;
        }

        public void ForceSend(MyClientStateBase clientData)
        {

        }

        public void TimestampReset(MyTimeSpan timestamp)
        {
        }

        public bool IsStillDirty(EndpointId forClient)
        {
            return m_clientData[forClient].DirtyProperties.Bits > 0;
        }
    }
}
