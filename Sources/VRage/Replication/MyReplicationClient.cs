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
using VRage.Utils;
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
        int m_frameIndex = 0;

        // TODO: Maybe pool pending replicables?
        Dictionary<NetworkId, MyPendingReplicable> m_pendingReplicables = new Dictionary<NetworkId, MyPendingReplicable>(16);

        MyEventsBuffer m_eventBuffer = new MyEventsBuffer();
        MyEventsBuffer.Handler m_eventHandler;
        MyEventsBuffer.IsBlockedHandler m_isBlockedHandler;

        public MyClientStateBase ClientState;

        public MyReplicationClient(IReplicationClientCallback callback, MyClientStateBase clientState)
            : base(false)
        {
            m_callback = callback;
            ClientState = clientState;
            m_eventHandler = base.ProcessEvent;
            m_isBlockedHandler = this.IsBlocked;
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
        void SetReplicableReady(NetworkId networkId, IMyReplicable replicable, bool loaded)
        {
            MyPendingReplicable pendingReplicable;
            if (m_pendingReplicables.TryGetValue(networkId, out pendingReplicable))
            {
                m_pendingReplicables.Remove(networkId);

                if (loaded)
                {
                    var ids = pendingReplicable.StateGroupIds;

                    AddNetworkObjectClient(networkId, replicable);

                    using (m_tmpGroups)
                    {
                        
                        IMyStreamableReplicable streamable = replicable as IMyStreamableReplicable;
                        if (streamable != null && pendingReplicable.IsStreaming)
                        {
                            var group = streamable.GetStreamingStateGroup();
                            m_tmpGroups.Add(group);
                        }

                        replicable.GetStateGroups(m_tmpGroups);
                        Debug.Assert(ids.Count == m_tmpGroups.Count, "Number of state groups on client and server for replicable does not match");
                        for (int i = 0; i < m_tmpGroups.Count; i++)
                        {
                            if (m_tmpGroups[i] != replicable && m_tmpGroups[i].GroupType != StateGroupEnum.Streamining)
                            {
                                AddNetworkObjectClient(ids[i], m_tmpGroups[i]);
                            }
                        }
                    }
                    m_eventBuffer.ProcessEvents(networkId, m_eventHandler, m_isBlockedHandler, NetworkId.Invalid);
                }
                else
                {
                    MyLog.Default.WriteLine("Failed to create replicable ! Type : " + replicable.ToString());
                    m_eventBuffer.RemoveEvents(networkId);

                    IMyStreamableReplicable streamable = replicable as IMyStreamableReplicable;
                    if (streamable != null && pendingReplicable.IsStreaming)
                    {
                        var group = streamable.GetStreamingStateGroup();
                        group.Destroy();
                        NetworkId streaingGroupId;
                        if (TryGetNetworkIdByObject(group, out streaingGroupId))
                        {
                            RemoveNetworkedObject(group);
                        }
                        MyLog.Default.WriteLine("removing streaming group for not loaded replicable !");
                    }
                }

                m_sendStream.ResetWrite();
                m_sendStream.WriteNetworkId(networkId);
                m_sendStream.WriteBool(loaded);
                m_callback.SendReplicableReady(m_sendStream);
            }
            else
            {
                m_pendingReplicables.Remove(networkId);
                using (m_tmpGroups)
                {
                    IMyStreamableReplicable streamable = replicable as IMyStreamableReplicable;
                    if (streamable != null && streamable.NeedsToBeStreamed)
                    {
                        var group = streamable.GetStreamingStateGroup();
                        m_tmpGroups.Add(group);
                        MyLog.Default.WriteLine("removing streaming group for not loaded replicable !" );
                    }

                    replicable.GetStateGroups(m_tmpGroups);
                    foreach (var g in m_tmpGroups)
                    {
                        if (g != null) // when terminal repblicable fails to attach to block its state group is null becouase its created inside hook method.
                        {
                            g.Destroy();
                        }
                    }
                }      
                replicable.OnDestroy();
            }
        }

        public void ProcessReplicationCreateBegin(MyPacket packet)
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

            var ids = pendingReplicable.StateGroupIds;

            using (m_tmpGroups)
            {
                IMyStreamableReplicable streamable = replicable as IMyStreamableReplicable;
                if (streamable != null)
                {
                    pendingReplicable.IsStreaming = true;
                    var group = streamable.GetStreamingStateGroup();
                    m_tmpGroups.Add(group);
                }

                for (int i = 0; i < m_tmpGroups.Count; i++)
                {
                    if (m_tmpGroups[i] != replicable)
                    {
                        AddNetworkObjectClient(ids[i], m_tmpGroups[i]);
                        pendingReplicable.StreamingGroupId = ids[i];
                    }
                }
            }

            replicable.OnLoadBegin(m_receiveStream, (loaded) => SetReplicableReady(networkID, replicable, loaded));
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
            pendingReplicable.IsStreaming = false;

            m_pendingReplicables.Add(networkID, pendingReplicable);

            replicable.OnLoad(m_receiveStream, (loaded) => SetReplicableReady(networkID, replicable, loaded));
        }

        public void ProcessReplicationDestroy(MyPacket packet)
        {
            m_receiveStream.ResetRead(packet);
            NetworkId networkID = m_receiveStream.ReadNetworkId();

            MyPendingReplicable pendingReplicable;

            if (!m_pendingReplicables.TryGetValue(networkID, out pendingReplicable)) // When it wasn't in pending replicables, it's already active and in scene, destroy it
            {
                IMyReplicable replicable = (IMyReplicable)GetObjectByNetworkId(networkID);
                // Debug.Assert(replicable != null, "Client received ReplicationDestroy, but object no longer exists (removed locally?)");
                if (replicable != null)
                {
                    using (m_tmpGroups)
                    {

                        var streamable = replicable as IMyStreamableReplicable;
                        if (streamable != null && streamable.NeedsToBeStreamed)
                        {
                            m_tmpGroups.Add(streamable.GetStreamingStateGroup());
                        }

                        replicable.GetStateGroups(m_tmpGroups);

                        foreach (var g in m_tmpGroups)
                        {
                            if (g == null)
                            {
                                continue;
                            }

                            if (g != replicable)
                                RemoveNetworkedObject(g);
                            g.Destroy();
                        }
                    }

                    RemoveNetworkedObject(replicable);
                    replicable.OnDestroy();
                }
            }
            else
            {
                m_pendingReplicables.Remove(networkID);
                if (pendingReplicable.IsStreaming)
                {
                    IMyStateGroup group = (IMyStateGroup)GetObjectByNetworkId(pendingReplicable.StreamingGroupId);
                    if (group != null)
                    {
                        RemoveNetworkedObject(group);
                        group.Destroy();
                    }
                }
                m_eventBuffer.RemoveEvents(networkID);   
            }
        }

        public void ProcessServerData(MyPacket packet)
        {
            m_receiveStream.ResetRead(packet);
            SerializeTypeTable(m_receiveStream);
            m_hasTypeTable = true;
        }

        public override void Update()
        {
            if (!m_clientReady || !m_hasTypeTable || ClientState == null)
                return;

            m_frameIndex++;
            if (m_frameIndex % 2 == 0) // Client updates once per 2 frames
                return;


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

        internal override bool DispatchBlockingEvent(BitStream stream, CallSite site, EndpointId recipient, IMyNetObject eventInstance, IMyNetObject blockedNetObj, float unreliablePriority)
        {
            Debug.Fail("Client should not call blocking events");
            // For client this code is old. Only server can dispatch blocking events.
            return DispatchEvent(stream, site, recipient, eventInstance, unreliablePriority);
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
                // THIS IS NO LONGER USED and IT'S NOT VALID
                //return true;
            }
            return false;
        }

        /// <summary>
        /// Checks if network id is blocked by other network id.
        /// </summary>
        /// <param name="networkId">Target network id.</param>
        /// <param name="blockedNetId">Blocking network id.</param>
        /// <returns></returns>
        private bool IsBlocked(NetworkId networkId, NetworkId blockedNetId)
        {
            bool anyReplPending = m_pendingReplicables.ContainsKey(networkId) || m_pendingReplicables.ContainsKey(blockedNetId);
            bool anyDoesNotExist = GetObjectByNetworkId(networkId) == null || (blockedNetId.IsValid && GetObjectByNetworkId(blockedNetId) == null);
            

            if (networkId.IsValid && (anyReplPending || anyDoesNotExist))
            {
                return true;
            }

            return false;
        }

        protected override void ProcessEvent(BitStream stream, NetworkId networkId, NetworkId blockedNetId, uint eventId, EndpointId sender)
        {
            // Check if any of them is not blocked already.
            bool anyContainsEvents = m_eventBuffer.ContainsEvents(networkId) || m_eventBuffer.ContainsEvents(blockedNetId);

            if (this.IsBlocked(networkId, blockedNetId) || anyContainsEvents)
            {
                m_eventBuffer.EnqueueEvent(stream, networkId, blockedNetId, eventId, sender);
                // Only enqueue barrier if blocking network id is set
                if(blockedNetId.IsValid)
                    m_eventBuffer.EnqueueBarrier(blockedNetId, networkId);
            }
            else
            {
                base.ProcessEvent(stream, networkId, blockedNetId, eventId, sender);
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
            // if (MyRandom.Instance.NextFloat() > 0.3f) return;

            m_receiveStream.ResetRead(packet);
            bool isStreaming = m_receiveStream.ReadBool();
            m_lastStateSyncPacketId = m_receiveStream.ReadByte();

            while (m_receiveStream.BytePosition < m_receiveStream.ByteLength)
            {
                NetworkId networkID = m_receiveStream.ReadNetworkId();
                IMyStateGroup obj = GetObjectByNetworkId(networkID) as IMyStateGroup;

                if (obj == null)
                {
                    if (isStreaming == false)
                    {
                        Debug.Fail("IMyStateGroup not found by NetworkId");
                        break;
                    }
                    else
                    {
                        return;
                    }
                }

               if(isStreaming && obj.GroupType != StateGroupEnum.Streamining)
               {
                   Debug.Fail("group type mismatch !");
                   MyLog.Default.WriteLine("recieved streaming flag but group is not streaming !");
                   return;
               }

               if (!isStreaming && obj.GroupType == StateGroupEnum.Streamining)
               {
                   Debug.Fail("group type mismatch !");
                   MyLog.Default.WriteLine("recieved non streaming flag but group wants to stream !");
                   return;
               }

                var pos = m_receiveStream.BytePosition;
                NetProfiler.Begin(obj.GetType().Name);
                obj.Serialize(m_receiveStream, null, m_lastStateSyncPacketId, 0);
                NetProfiler.End(m_receiveStream.ByteLength - pos);
            }

            if (!m_acks.Contains(m_lastStateSyncPacketId))
            {
                m_acks.Add(m_lastStateSyncPacketId);
            }
        }

        #region Debug methods

        public override string GetMultiplayerStat()
        {
            StringBuilder multiplayerStat = new StringBuilder();
            
            string baseStats = base.GetMultiplayerStat();

            multiplayerStat.Append(baseStats);

            multiplayerStat.AppendLine("Pending Replicables:");
            foreach(var pendingRep in m_pendingReplicables)
            {
                string pendingRepInfo = "   NetworkId: " + pendingRep.Key.ToString() + ", IsStreaming: " + pendingRep.Value.IsStreaming;
                multiplayerStat.AppendLine(pendingRepInfo);
            }

            multiplayerStat.Append(m_eventBuffer.GetEventsBufferStat());

            return multiplayerStat.ToString();
        }

        #endregion

    }
}
