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
    public class MyReplicationServer : MyReplicationLayer
    {
        class ClientData
        {
            public readonly MyClientStateBase State;

            // Additional per replicable information for client
            public readonly Dictionary<IMyReplicable, MyReplicableClientData> Replicables = new Dictionary<IMyReplicable, MyReplicableClientData>();

            // Additional per state-group information for client
            public readonly Dictionary<IMyStateGroup, MyStateDataEntry> StateGroups = new Dictionary<IMyStateGroup, MyStateDataEntry>();
            public readonly MyPacketQueue EventQueue;

            // First sent packet id is 1 (it's incremented first)
            public byte StateSyncPacketId = 0;
            public byte LastReceivedAckId = 0;
            public byte LastStateSyncPacketId = 0;

            public bool WaitingForReset = false;

            // 16 KB per client
            public readonly List<IMyStateGroup>[] PendingStateSyncAcks = Enumerable.Range(0, 256).Select(s => new List<IMyStateGroup>(8)).ToArray();

            public ClientData(MyClientStateBase emptyState, Action<BitStream, EndpointId> sender)
            {
                State = emptyState;
                EventQueue = new MyPacketQueue(512, sender);
            }

            public bool IsReplicableReady(IMyReplicable replicable)
            {
                MyReplicableClientData info;
                return Replicables.TryGetValue(replicable, out info) && !info.IsPending;
            }
        }

        private IReplicationServerCallback m_callback;
        private Action<BitStream, EndpointId> m_eventQueueSender;
        private CacheList<IMyStateGroup> m_tmpGroups = new CacheList<IMyStateGroup>(4);
        private CacheList<MyStateDataEntry> m_tmpSortEntries = new CacheList<MyStateDataEntry>();
        private int m_frameCounter;

        // Packet received out of order with number preceding closely last packet is accepted.
        // When multiplied by 16.66ms, this gives you minimum time after which packet can be resent.
        private const byte m_outOfOrderAcceptThreshold = 6; // ~100 ms

        // How much to increase packed ID after reset, this is the number of packets client call still ACK.
        private const byte m_outOfOrderResetProtection = 64;

        public MyTimeSpan MaxSleepTime = MyTimeSpan.FromMinutes(5);

        /// <summary>
        /// All replicable objects on server.
        /// </summary>
        Dictionary<NetworkId, IMyReplicable> m_replicables;

        /// <summary>
        /// All replicable groups.
        /// </summary>
        Dictionary<IMyReplicable, List<IMyStateGroup>> m_replicableGroups = new Dictionary<IMyReplicable, List<IMyStateGroup>>();

        /// <summary>
        /// Network objects and states which are actively replicating to clients.
        /// </summary>
        Dictionary<EndpointId, ClientData> m_clientStates = new Dictionary<EndpointId, ClientData>();

        /// <summary>
        /// Function which provides current update time
        /// </summary>
        Func<MyTimeSpan> m_timeFunc;

        public MyReplicationServer(IReplicationServerCallback callback, Func<MyTimeSpan> updateTimeGetter)
            : base(true)
        {
            m_callback = callback;
            m_timeFunc = updateTimeGetter;
            m_replicables = new Dictionary<NetworkId, IMyReplicable>();
            m_clientStates = new Dictionary<EndpointId, ClientData>();
            m_eventQueueSender = (s, e) => m_callback.SendEvent(s, false, e);
        }

        public override void Dispose()
        {
            base.Dispose();
            m_sendStream.Dispose();
        }

        public void Replicate(IMyReplicable obj)
        {
            if (!IsTypeReplicated(obj.GetType()))
            {
                Debug.Fail(String.Format("Type '{0}' not replicated, this should be checked before calling Replicate", obj.GetType().Name));
                return;
            }

            NetworkId networkId = AddNetworkObjectServer(obj);
            m_replicables.Add(networkId, obj);

            AddStateGroups(obj);

            // HACK: test serialization
            //m_sendStream.ResetWrite(MessageIDEnum.REPLICATION_CREATE);
            //stateData.Replicate(m_sendStream);
        }

        bool PrepareForceReplicable(IMyReplicable obj)
        {
            Debug.Assert(obj != null);
            if (!IsTypeReplicated(obj.GetType()))
            {
                Debug.Fail(String.Format("Cannot replicate {0}, type is not replicated", obj));
                return false;
            }

            NetworkId id;
            if (!TryGetNetworkIdByObject(obj, out id))
            {
                Replicate(obj);
            }
            return true;
        }

        /// <summary>
        /// Hack to allow thing like: CreateCharacter, Respawn sent from server
        /// </summary>
        public void ForceReplicable(IMyReplicable obj, IMyReplicable dependency = null)
        {
            PrepareForceReplicable(obj);
            foreach (var client in m_clientStates)
            {
                if (dependency != null)
                {
                    if (!client.Value.Replicables.ContainsKey(dependency))
                    {
                        continue;
                    }
                }

                if (!client.Value.Replicables.ContainsKey(obj))
                {
                    AddForClient(obj, client.Key, client.Value);
                }
            }
        }

        /// <summary>
        /// Hack to allow thing like: CreateCharacter, Respawn sent from server
        /// </summary>
        public void ForceReplicable(IMyReplicable obj, EndpointId clientEndpoint)
        {
            PrepareForceReplicable(obj);
            var client = m_clientStates[clientEndpoint];
            if (!client.Replicables.ContainsKey(obj))
            {
                AddForClient(obj, clientEndpoint, client);
            }
        }

        public void Destroy(IMyReplicable obj)
        {
            Debug.Assert(obj != null);
            if (!IsTypeReplicated(obj.GetType()))
            {
                return;
            }

            var id = GetNetworkIdByObject(obj);
            if (id.IsInvalid)
            {
                Debug.Fail("Destroying object which is not present");
                return;
            }

            // Remove from client states, remove from client replicables, send destroy
            foreach (var client in m_clientStates)
            {
                // TODO: Postpone removing for client (we don't want to peak network when a lot of objects get removed)
                if (client.Value.Replicables.ContainsKey(obj))
                {
                    RemoveForClient(obj, client.Key, client.Value, true);
                }
            }

            RemoveStateGroups(obj);
            var netId = RemoveNetworkedObject(obj);
            m_replicables.Remove(netId);
        }

        /// <summary>
        /// Destroys replicable for all clients (used for testing and debugging).
        /// </summary>
        public void ResetForClients(IMyReplicable obj)
        {
            foreach(var client in m_clientStates)
            {
                if(client.Value.Replicables.ContainsKey(obj))
                {
                    RemoveForClient(obj, client.Key, client.Value, true);
                }
            }
        }

        public void OnClientReady(EndpointId endpointId, MyClientStateBase clientState)
        {
            Debug.Assert(!m_clientStates.ContainsKey(endpointId), "Client entry already exists, bad call to OnClientJoined?");
            clientState.EndpointId = endpointId;
            m_clientStates.Add(endpointId, new ClientData(clientState, m_eventQueueSender));
            SendServerData(endpointId);
        }

        private void SendServerData(EndpointId endpointId)
        {
            m_sendStream.ResetWrite();
            SerializeTypeTable(m_sendStream);
            m_callback.SendServerData(m_sendStream, endpointId);
        }

        public void OnClientLeft(EndpointId endpointId)
        {
            Debug.Assert(m_clientStates.ContainsKey(endpointId), "Client entry does not exists, bad call to OnClientLeft?");
            ClientData data;
            if (m_clientStates.TryGetValue(endpointId, out data))
            {
                while (data.Replicables.Count > 0)
                    RemoveForClient(data.Replicables.FirstPair().Key, endpointId, data, false);
                m_clientStates.Remove(endpointId);
            }
        }

        public void OnClientUpdate(MyPacket packet)
        {
            Debug.Assert(m_clientStates.ContainsKey(packet.Sender), "Client entry not found, client should be already joined at this point");
            var clientData = m_clientStates[packet.Sender];

            m_receiveStream.ResetRead(packet);

            // Read last state sync packet id
            clientData.LastStateSyncPacketId = m_receiveStream.ReadByte();

            // Read ACKs
            byte count = m_receiveStream.ReadByte();
            for (int i = 0; i < count; i++)
            {
                OnAck(m_receiveStream.ReadByte(), clientData);
            }

            byte firstDropId, lastDropId;
            if (clientData.WaitingForReset)
            {
                // Client wasn't sending ACKs for long time (m_outOfOrderResetProtection = 64)
                // Set new packet id
                // Mark everything except 64 packets before new packed id as FAIL
                clientData.StateSyncPacketId = (byte)(clientData.LastStateSyncPacketId + m_outOfOrderResetProtection);
                firstDropId = (byte)(clientData.StateSyncPacketId + 1);
                lastDropId = (byte)(firstDropId - m_outOfOrderResetProtection);
                clientData.WaitingForReset = false;
            }
            else
            {
                // Process not delivered ACKs (m_outOfOrderAcceptThreshold = 6)
                // LastReceivedAckId - 6 ... StateSyncPacketId is waiting for ACK
                // StateSyncPacketId + 1 ... LastReceivedAckId - 7 is FAIL
                firstDropId = (byte)(clientData.StateSyncPacketId + 1); // Intentional overflow
                lastDropId = (byte)(clientData.LastReceivedAckId - m_outOfOrderAcceptThreshold); // Intentional overflow
            }

            for (byte i = firstDropId; i != lastDropId; i++) // Intentional overflow
            {
                RaiseAck(clientData, i, false);
            }

            // Read client state
            clientData.State.Serialize(m_receiveStream);
        }

        void OnAck(byte ackId, ClientData clientData)
        {
            if (IsPreceding(ackId, clientData.LastReceivedAckId, m_outOfOrderAcceptThreshold))
            {
                // Accept ACK (out of order, but OK)
                RaiseAck(clientData, ackId, true);
            }
            else
            {
                // Accept ACK and set LastReceivedAckId
                RaiseAck(clientData, ackId, true);
                clientData.LastReceivedAckId = ackId;
            }
        }

        void RaiseAck(ClientData clientData, byte ackId, bool delivered)
        {
            foreach (var group in clientData.PendingStateSyncAcks[ackId])
            {
                // When group is not there, it was already destroyed for that client
                if (clientData.StateGroups.ContainsKey(group))
                    group.OnAck(clientData.State, ackId, delivered);
            }
            clientData.PendingStateSyncAcks[ackId].Clear();
        }

        /// <summary>
        /// Returns true when current packet is closely preceding last packet (is within threshold)
        /// </summary>
        bool IsPreceding(int currentPacketId, int lastPacketId, int threshold)
        {
            if (lastPacketId < currentPacketId)
                lastPacketId += 256;
            return (lastPacketId - currentPacketId) <= threshold;
        }

        public override void Update()
        {
            m_frameCounter++;

            // TODO: Send only limited number of objects!
            // TODO: Optimize, no need to go through all objects of all client every frame
            foreach (var pair in m_replicables)
            {
                RefreshReplicable(pair.Value);
            }

            foreach (var client in m_clientStates)
            {
                SendStateSync(client.Value.State);
            }
        }

        private void RefreshReplicable(IMyReplicable replicable)
        {
            MyTimeSpan now = m_timeFunc();

            foreach (var client in m_clientStates)
            {
                MyReplicableClientData replicableInfo;
                bool hasObj = client.Value.Replicables.TryGetValue(replicable, out replicableInfo);
                bool isRelevant = replicable.GetPriority(client.Value.State) > 0;
                if(isRelevant)
                {
                    var dependency = replicable.GetDependency();
                    isRelevant = dependency == null || client.Value.IsReplicableReady(dependency);
                }                

                if (!hasObj && isRelevant)
                {
                    AddForClient(replicable, client.Key, client.Value);
                }
                else if (hasObj)
                {
                    // Hysteresis
                    replicableInfo.UpdateSleep(isRelevant, now);
                    if (replicableInfo.ShouldRemove(now, MaxSleepTime))
                        RemoveForClient(replicable, client.Key, client.Value, true);
                }
            }
        }

        private void SendStateSync(MyClientStateBase state)
        {
            var now = m_timeFunc();

            ClientData clientData;
            if (m_clientStates.TryGetValue(state.EndpointId, out clientData))
            {
                if (clientData.StateGroups.Count == 0)
                    return;

                EndpointId endpointId = state.EndpointId;

                // TODO: Limit events
                clientData.EventQueue.Send();

                using (m_tmpSortEntries)
                {
                    foreach (var entry in clientData.StateGroups.Values)
                    {
                        // No state sync for pending or sleeping replicables
                        if (!clientData.Replicables[entry.Owner].HasActiveStateSync)
                            continue;

                        entry.FramesWithoutSync++;
                        entry.Priority = entry.Group.GetGroupPriority(entry.FramesWithoutSync, state);

                        if (entry.Priority > 0)
                            m_tmpSortEntries.Add(entry);
                    }

                    m_tmpSortEntries.Sort(MyStateDataEntryComparer.Instance);

                    byte firstWaitingPacket = (byte)(clientData.LastReceivedAckId - m_outOfOrderAcceptThreshold);
                    byte nextPacketId = (byte)(clientData.StateSyncPacketId + 1);

                    if (clientData.WaitingForReset || nextPacketId == firstWaitingPacket)
                    {
                        clientData.WaitingForReset = true;
                        return;
                    }

                    clientData.StateSyncPacketId++;

                    m_sendStream.ResetWrite();
                    m_sendStream.WriteByte(clientData.StateSyncPacketId);

                    int MTUBytes = (int)m_callback.GetMTUSize(endpointId);
                    int messageBitSize = 8 * (MTUBytes - 8 - 1); // MTU - headers

                    // TODO: Rewrite
                    int maxToSend = MTUBytes / 8; // lets assume the shortest message is 8 Bytes long
                    int sent = 0;

                    // TODO:SK limit to N in panic entries per packet
                    foreach (var entry in m_tmpSortEntries)
                    {
                        var oldWriteOffset = m_sendStream.BitPosition;
                        m_sendStream.WriteNetworkId(entry.GroupId);
                        entry.Group.Serialize(m_sendStream, state, clientData.StateSyncPacketId, messageBitSize);
                        if (m_sendStream.BitPosition > oldWriteOffset && m_sendStream.BitPosition <= messageBitSize)
                        {
                            clientData.PendingStateSyncAcks[clientData.StateSyncPacketId].Add(entry.Group);
                            sent++;
                            entry.FramesWithoutSync = 0;
                        }
                        else
                        {
                            // When serialize returns false, restore previous bit position
                            m_sendStream.SetBitPositionWrite(oldWriteOffset);
                        }

                        if (sent >= maxToSend)
                        {
                            break;
                        }
                    }
                    m_callback.SendStateSync(m_sendStream, endpointId);
                    //Server.SendMessage(m_sendStream, guid, PacketReliabilityEnum.UNRELIABLE, PacketPriorityEnum.MEDIUM_PRIORITY, MyChannelEnum.StateDataSync);
                }
            }
        }

        private void AddForClient(IMyReplicable replicable, EndpointId clientEndpoint, ClientData clientData)
        {
            AddClientReplicable(replicable, clientData);
            SendReplicationCreate(replicable, clientEndpoint);
            Console.WriteLine(String.Format("Sending replication create: {0}, {1}", GetNetworkIdByObject(replicable), replicable));
        }

        private void RemoveForClient(IMyReplicable replicable, EndpointId clientEndpoint, ClientData clientData, bool sendDestroyToClient)
        {
            if (sendDestroyToClient)
            {
                SendReplicationDestroy(replicable, clientEndpoint);
            }
            RemoveClientReplicable(replicable, clientData);
            Console.WriteLine(String.Format("Sending replication destroy: {0}", GetNetworkIdByObject(replicable)));
        }

        void SendReplicationCreate(IMyReplicable obj, EndpointId clientEndpoint)
        {
            var typeId = GetTypeIdByType(obj.GetType());
            var networkId = GetNetworkIdByObject(obj);

            var groups = m_replicableGroups[obj];
            Debug.Assert(groups.Count <= 255, "Unexpectedly high number of groups");

            m_sendStream.ResetWrite();
            m_sendStream.WriteTypeId(typeId);
            m_sendStream.WriteNetworkId(networkId);
            m_sendStream.WriteByte((byte)groups.Count);
            for (int i = 0; i < groups.Count; i++)
            {
                m_sendStream.WriteNetworkId(GetNetworkIdByObject(groups[i]));
            }
            obj.OnSave(m_sendStream);

            m_callback.SendReplicationCreate(m_sendStream, clientEndpoint);
            //Server.SendMessage(m_sendStream, clientId, PacketReliabilityEnum.RELIABLE, PacketPriorityEnum.LOW_PRIORITY, MyChannelEnum.Replication);
        }

        void SendReplicationDestroy(IMyReplicable obj, EndpointId clientEndpoint)
        {
            m_sendStream.ResetWrite();
            m_sendStream.WriteNetworkId(GetNetworkIdByObject(obj));
            m_callback.SendReplicationDestroy(m_sendStream, clientEndpoint);
            //Server.SendMessage(m_sendStream, clientId, PacketReliabilityEnum.RELIABLE, PacketPriorityEnum.LOW_PRIORITY, MyChannelEnum.Replication);
        }

        public void ReplicableReady(MyPacket packet)
        {
            m_receiveStream.ResetRead(packet);
            var networkId = m_receiveStream.ReadNetworkId();

            Debug.Assert(m_clientStates.ContainsKey(packet.Sender), "Client data not found");

            ClientData clientData = m_clientStates[packet.Sender];
            var replicable = GetObjectByNetworkId(networkId) as IMyReplicable;

            // Replicable can be destroyed for client or destroyed completely at this point
            MyReplicableClientData replicableClientData;
            if (replicable != null && clientData.Replicables.TryGetValue(replicable, out replicableClientData))
            {
                Debug.Assert(replicableClientData.IsPending == true, "Replicable ready from client, but it's not pending for client");
                replicableClientData.IsPending = false;
            }
        }

        public void AddStateGroups(IMyReplicable replicable)
        {
            using (m_tmpGroups)
            {
                replicable.GetStateGroups(m_tmpGroups);
                foreach (var group in m_tmpGroups)
                {
                    if (group != replicable)
                        AddNetworkObjectServer(group);
                }
                m_replicableGroups.Add(replicable, new List<IMyStateGroup>(m_tmpGroups));
            }
        }

        public void RemoveStateGroups(IMyReplicable replicable)
        {
            // Remove from actively replicating state groups and replicable
            foreach (var client in m_clientStates.Values)
            {
                RemoveClientReplicable(replicable, client);
            }

            // Remove groups
            foreach (var group in m_replicableGroups[replicable])
            {
                if (group != replicable)
                    RemoveNetworkedObject(group);
            }
            m_replicableGroups.Remove(replicable);
        }

        private void AddClientReplicable(IMyReplicable replicable, ClientData clientData)
        {
            // Add replicable
            clientData.Replicables.Add(replicable, new MyReplicableClientData());

            // Add state groups
            foreach (var group in m_replicableGroups[replicable])
            {
                var netId = GetNetworkIdByObject(group);
                clientData.StateGroups.Add(group, new MyStateDataEntry(replicable, netId, group));
                group.CreateClientData(clientData.State);
            }
        }

        private void RemoveClientReplicable(IMyReplicable replicable, ClientData clientData)
        {
            foreach (var g in m_replicableGroups[replicable])
            {
                g.DestroyClientData(clientData.State);
                clientData.StateGroups.Remove(g);
            }
            clientData.Replicables.Remove(replicable);
        }

        //internal void ResetPriorities(EndpointId endpointId)
        //{
        //    Debug.Assert(m_clientStates.ContainsKey(endpointId));
        //    foreach (var entry in m_clientStates[endpointId].StateGroups.Values)
        //    {
        //        entry.ResetPriority();
        //    }
        //}

        // Event dispatch:
        // 1) Reliable events are sent immediatelly when client has replicable or state group
        // 2) Unreliable events are added into queue with priority (they added only for relevant replicated objects or state groups)
        bool ShouldSendEvent(IMyNetObject eventInstance, bool isReliable, ClientData client, out float priority)
        {
            if (eventInstance == null)
            {
                // Static event
                priority = 1;
                return true;
            }

            MyStateDataEntry entry;
            MyReplicableClientData replicableInfo;
            if ((eventInstance is IMyStateGroup && client.StateGroups.TryGetValue((IMyStateGroup)eventInstance, out entry)))
            {
                // For state group, priority cannot be inherited, because it's changing with time
                // Maybe add another method IMyStateGroup.GetEventPriority()
                replicableInfo = client.Replicables[entry.Owner];
                priority = 1;
                return isReliable || replicableInfo.HasActiveStateSync;
            }
            else if (eventInstance is IMyReplicable && (client.Replicables.TryGetValue((IMyReplicable)eventInstance, out replicableInfo) || m_fixedObjects.Contains(eventInstance)))
            {
                // Event inherits replicated object priority
                priority = ((IMyReplicable)eventInstance).GetPriority(client.State);
                return isReliable || (priority > 0 && replicableInfo.HasActiveStateSync);
            }
            else
            {
                priority = 0;
                return false;
            }
        }

        protected override MyClientStateBase GetClientData(EndpointId endpointId)
        {
            ClientData client;
            return m_clientStates.TryGetValue(endpointId, out client) ? client.State : null;
        }

        internal override bool DispatchEvent(BitStream stream, CallSite site, EndpointId target, IMyNetObject eventInstance, float unreliablePriority)
        {
            Debug.Assert(site.HasClientFlag || site.HasBroadcastFlag || site.HasBroadcastExceptFlag, String.Format("Event '{0}' does not have Client, Broadcast or BroadcastExcept flag, it can't be invoked on client!", site));

            var replicable = eventInstance as IMyReplicable;
            if (site.HasRefreshReplicableFlag && replicable != null)
            {
                RefreshReplicable(replicable);
            }

            if (site.HasBroadcastFlag || site.HasBroadcastExceptFlag)
            {
                foreach (var client in m_clientStates)
                {
                    if (site.HasBroadcastExceptFlag && client.Key == target)
                        continue;

                    // Send it also to client who invoked this method on server
                    float priority;
                    if (ShouldSendEvent(eventInstance, site.IsReliable, client.Value, out priority))
                    {
                        DispatchEvent(client.Value, priority * unreliablePriority, client.Key, stream, site.IsReliable);
                    }
                }
            }
            else if (site.HasClientFlag)
            {
                Debug.Assert(target.IsValid, "Target cannot be null when invoking Client event");
                Debug.Assert(m_clientStates.ContainsKey(target), "Target client not found");

                ClientData clientData;
                float priority;
                if (m_clientStates.TryGetValue(target, out clientData) && ShouldSendEvent(eventInstance, site.IsReliable, clientData, out priority))
                {
                    DispatchEvent(clientData, priority, target, stream, site.IsReliable);
                }
            }

            return site.HasServerFlag; // Invoke locally when Local flag is set
        }

        void DispatchEvent(ClientData client, float priority, EndpointId endpointId, BitStream stream, bool reliable)
        {
            if (reliable)
            {
                m_callback.SendEvent(stream, true, endpointId);
            }
            else
            {
                client.EventQueue.Enqueue(stream, priority, endpointId);
            }
        }

        internal override void ProcessEvent(BitStream stream, CallSite site, object obj, IMyNetObject sendAs, EndpointId source)
        {
            // Return when validation fails
            if (!Invoke(site, stream, obj, source, GetClientData(source), true))
                return;

            // Send event in case it has [Broadcast], [BroadcastExcept] or [Client] attribute
            if (site.HasClientFlag || site.HasBroadcastFlag || site.HasBroadcastExceptFlag)
            {
                DispatchEvent(stream, site, source, sendAs, 1.0f);
            }
        }
    }
}
