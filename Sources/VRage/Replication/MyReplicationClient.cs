using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using VRage.Library.Collections;
using VRage.Library.Utils;
using VRage.Replication;
using VRage.Serialization;
using VRageMath;

namespace VRage.Network
{
    public class MyReplicationClient : MyReplicationLayer
    {
        bool m_clientReady;
        bool m_hasTypeTable;
        IReplicationClientCallback m_callback;
        CacheList<IMyStateGroup> m_tmpGroups = new CacheList<IMyStateGroup>(4);
        List<byte> m_acks = new List<byte>();
        byte m_lastStateSyncPacketId;

        // TODO: Maybe pool pending replicables?
        Dictionary<NetworkId, MyPendingReplicable> m_pendingReplicables = new Dictionary<NetworkId, MyPendingReplicable>(16);

        MyEventBuffer m_eventBuffer = new MyEventBuffer();
        MyEventBuffer.Handler m_eventHandler;

        public MyClientStateBase ClientState;

        public MyReplicationClient(IReplicationClientCallback callback, MyClientStateBase clientState)
            : base(false)
        {
            m_callback = callback;
            ClientState = clientState;
            m_eventHandler = base.ProcessEvent;
        }

        public override void Dispose()
        {
            m_eventBuffer.Dispose();
            base.Dispose();
        }

        public void OnLocalClientReady()
        {
            m_clientReady = true;
        }

        /// <summary>
        /// Marks replicable as successfully created, ready to receive events and state groups data.
        /// </summary>
        void SetReplicableReady(NetworkId networkId, IMyReplicable replicable)
        {
            MyPendingReplicable pendingReplicable;
            if (m_pendingReplicables.TryGetValue(networkId, out pendingReplicable))
            {
                var ids = pendingReplicable.StateGroupIds;

                AddNetworkObjectClient(networkId, replicable);

                using (m_tmpGroups)
                {
                    replicable.GetStateGroups(m_tmpGroups);
                    Debug.Assert(ids.Count == m_tmpGroups.Count, "Number of state groups on client and server for replicable does not match");
                    for (int i = 0; i < m_tmpGroups.Count; i++)
                    {
                        if (m_tmpGroups[i] != replicable)
                            AddNetworkObjectClient(ids[i], m_tmpGroups[i]);
                    }
                }

                m_pendingReplicables.Remove(networkId);
                m_eventBuffer.ProcessEvents(networkId, m_eventHandler);

                m_sendStream.ResetWrite();
                m_sendStream.WriteNetworkId(networkId);
                m_callback.SendReplicableReady(m_sendStream);
            }
            else
            {
                // Replicable was already destroyed on server, during it's load on client
                m_eventBuffer.RemoveEvents(networkId);
                replicable.OnDestroy();
            }
        }

        public void ProcessReplicationCreate(MyPacket packet)
        {
            m_receiveStream.ResetRead(packet);

            TypeId typeId = m_receiveStream.ReadTypeId();
            NetworkId networkID = m_receiveStream.ReadNetworkId();
            byte groupCount = m_receiveStream.ReadByte();

            var pendingReplicable = new MyPendingReplicable();
            for (int i = 0; i < groupCount; i++)
            {
                var id = m_receiveStream.ReadNetworkId();
                pendingReplicable.StateGroupIds.Add(id);
            }

            Type type = GetTypeByTypeId(typeId);
            IMyReplicable replicable = (IMyReplicable)Activator.CreateInstance(type);
            pendingReplicable.DebugObject = replicable;
            m_pendingReplicables.Add(networkID, pendingReplicable);

            replicable.OnLoad(m_receiveStream, () => SetReplicableReady(networkID, replicable));
        }

        public void ProcessReplicationDestroy(MyPacket packet)
        {
            m_receiveStream.ResetRead(packet);
            NetworkId networkID = m_receiveStream.ReadNetworkId();

            if (!m_pendingReplicables.Remove(networkID)) // When it wasn't in pending replicables, it's already active and in scene, destroy it
            {
                IMyReplicable replicable = (IMyReplicable)GetObjectByNetworkId(networkID);
                using (m_tmpGroups)
                {
                    replicable.GetStateGroups(m_tmpGroups);
                    foreach (var g in m_tmpGroups)
                    {
                        if (g != replicable)
                            RemoveNetworkedObject(g);
                    }
                }

                RemoveNetworkedObject(replicable);
                replicable.OnDestroy();
            }
        }

        public void ProcessServerData(MyPacket packet)
        {
            m_receiveStream.ResetRead(packet);
            SerializeTypeTable(m_receiveStream);
            m_hasTypeTable = true;
        }

        [Conditional("DEBUG")]
        void CheckPending()
        {
            foreach (var pend in m_pendingReplicables)
            {
                pend.Value.DebugCounter++;
                Debug.Assert(pend.Value.DebugCounter != 300, "Replicable '" + pend.Value.DebugObject.GetType().Name + "' is pending for more than 300 frames, forgot to call loadingDoneHandler?");
            }
        }

        public override void Update()
        {
            if (!m_clientReady || !m_hasTypeTable || ClientState == null)
                return;

            CheckPending();

            // Update state groups on client
            foreach (var obj in NetworkObjects)
            {
                var stateGroup = obj as IMyStateGroup;
                if (stateGroup != null)
                {
                    stateGroup.ClientUpdate();
                }
            }

            m_sendStream.ResetWrite();

            // Write last state sync packet id
            m_sendStream.WriteByte(m_lastStateSyncPacketId);

            // Write ACKs
            byte num = (byte)m_acks.Count;
            m_sendStream.WriteByte(num);
            for (int i = 0; i < num; i++)
            {
                m_sendStream.WriteByte(m_acks[i]);
            }
            m_acks.Clear();

            // Write Client state
            ClientState.Serialize(m_sendStream);
            m_callback.SendClientUpdate(m_sendStream);
            //Client.SendMessageToServer(m_sendStream, PacketReliabilityEnum.UNRELIABLE, PacketPriorityEnum.IMMEDIATE_PRIORITY, MyChannelEnum.StateDataSync);
        }

        internal override bool DispatchEvent(BitStream stream, CallSite site, EndpointId target, IMyNetObject instance, float unreliablePriority)
        {
            Debug.Assert(site.HasServerFlag, String.Format("Event '{0}' does not have server flag, it can't be invoked on server!", site));

            if (site.HasServerFlag)
            {
                m_callback.SendEvent(stream, site.IsReliable);
                //Client.SendMessageToServer(stream, site.Reliability, PacketPriorityEnum.LOW_PRIORITY, MyChannelEnum.Replication);
            }
            else if (site.HasClientFlag)
            {
                // Invoke locally only when it has ClientFlag and no ServerFlag
                return true;
            }
            return false;
        }

        protected override void ProcessEvent(BitStream stream, NetworkId networkId, uint eventId, EndpointId sender)
        {
            if (networkId.IsValid && m_pendingReplicables.ContainsKey(networkId))
            {
                m_eventBuffer.EnqueueEvent(stream, networkId, eventId, sender);
            }
            else
            {
                base.ProcessEvent(stream, networkId, eventId, sender);
            }
        }

        internal override void ProcessEvent(BitStream stream, CallSite site, object obj, IMyNetObject sendAs, EndpointId source)
        {
            // Client blindly invokes everything received from server (without validation)
            Invoke(site, stream, obj, source, null, false);
        }

        /// <summary>
        /// Processes state sync sent by server.
        /// </summary>
        public void ProcessStateSync(MyPacket packet)
        {
            // Simulated packet loss
           // if (MyRandom.Instance.NextFloat() > 0.05f) return;

            m_receiveStream.ResetRead(packet);
            m_lastStateSyncPacketId = m_receiveStream.ReadByte();

            while (m_receiveStream.BytePosition < m_receiveStream.ByteLength)
            {
                NetworkId networkID = m_receiveStream.ReadNetworkId();
                IMyNetObject obj = GetObjectByNetworkId(networkID);
                
                var pos = m_receiveStream.BytePosition;
                NetProfiler.Begin(obj.GetType().Name);
                Debug.Assert(obj != null && obj is IMyStateGroup, "IMyStateGroup not found by NetworkId");
                ((IMyStateGroup)obj).Serialize(m_receiveStream, null, m_lastStateSyncPacketId, 0);

                NetProfiler.End(m_receiveStream.ByteLength - pos);
            }

            if (!m_acks.Contains(m_lastStateSyncPacketId))
            {
                m_acks.Add(m_lastStateSyncPacketId);
            }
        }
    }
}
