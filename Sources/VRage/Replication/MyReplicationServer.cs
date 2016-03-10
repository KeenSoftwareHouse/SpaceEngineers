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
        internal class MyDestroyBlocker
        {
            public bool Remove = false;
            public bool IsProcessing = false;
            public List<IMyReplicable> Blockers = new List<IMyReplicable>();
        }

        internal class ClientData
        {

            public readonly MyClientStateBase State;

            public readonly Dictionary<IMyReplicable, IMyReplicable> ForcedReplicables = new Dictionary<IMyReplicable, IMyReplicable>();

            // Additional per replicable information for client
            public readonly Dictionary<IMyReplicable, MyReplicableClientData> Replicables = new Dictionary<IMyReplicable, MyReplicableClientData>(InstanceComparer<IMyReplicable>.Default);

            // Temporary blocked network id by blocking event. Bool flag indicates if it should be destroyed right after it is replicationReady.
            public readonly Dictionary<IMyReplicable, MyDestroyBlocker> BlockedReplicables = new Dictionary<IMyReplicable, MyDestroyBlocker>();

            // Additional per state-group information for client
            public readonly Dictionary<IMyStateGroup, MyStateDataEntry> StateGroups = new Dictionary<IMyStateGroup, MyStateDataEntry>(InstanceComparer<IMyStateGroup>.Default);
            public readonly MyPacketQueue EventQueue;

            public readonly HashSet<IMyReplicable> PausedReplicables = new HashSet<IMyReplicable>();

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
                return Replicables.TryGetValue(replicable, out info) && !info.IsPending && !info.IsStreaming;
            }

            public bool IsReplicablePending(IMyReplicable replicable)
            {
                MyReplicableClientData info;
                return Replicables.TryGetValue(replicable, out info) && (info.IsPending ||info.IsStreaming);
            }

            public bool HasReplicable(IMyReplicable replicable)
            {
                return Replicables.ContainsKey(replicable) || ForcedReplicables.ContainsKey(replicable);
            }
        }

        public class PauseToken : IDisposable
        {
            MyReplicationServer m_server;
            bool m_oldValue;

            public PauseToken(MyReplicationServer server)
            {
                m_server = server;
                m_oldValue = server.m_replicationPaused;
                server.m_replicationPaused = true;
            }

            public void Dispose()
            {
                if (m_server != null)
                {
                    m_server.m_replicationPaused = m_oldValue;
                    if (!m_server.m_replicationPaused)
                        m_server.ResumeReplication();
                    m_server = null;
                }
            }
        }

        private bool m_replicationPaused = false;
        private EndpointId? m_localClientEndpoint;
        private IReplicationServerCallback m_callback;
        private Action<BitStream, EndpointId> m_eventQueueSender;
        private CacheList<IMyStateGroup> m_tmpGroups = new CacheList<IMyStateGroup>(4);
        private CacheList<MyStateDataEntry> m_tmpSortEntries = new CacheList<MyStateDataEntry>();
        private CacheList<MyStateDataEntry> m_tmpStreamingEntries = new CacheList<MyStateDataEntry>();
        List<IMyReplicable> m_tmp = new List<IMyReplicable>();
        HashSet<EndpointId> m_processedClients = new HashSet<EndpointId>();

        private MyBandwidthLimits m_limits = new MyBandwidthLimits();
        private HashSet<IMyReplicable> m_priorityUpdates = new HashSet<IMyReplicable>();
        private int m_frameCounter;

        // Packet received out of order with number preceding closely last packet is accepted.
        // When multiplied by 16.66ms, this gives you minimum time after which packet can be resent.
        private const byte m_outOfOrderAcceptThreshold = 6; // ~100 ms

        // How much to increase packed ID after reset, this is the number of packets client call still ACK.
        private const byte m_outOfOrderResetProtection = 64;

        public MyTimeSpan MaxSleepTime = MyTimeSpan.FromMinutes(5);

        /// <summary>
        /// All replicables on server.
        /// </summary>
        MyReplicables m_replicables = new MyReplicables();

        /// <summary>
        /// All replicable state groups.
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

        public MyReplicationServer(IReplicationServerCallback callback, Func<MyTimeSpan> updateTimeGetter, EndpointId? localClientEndpoint)
            : base(true)
        {
            Debug.Assert(localClientEndpoint == null || localClientEndpoint.Value.IsValid, "localClientEndpoint can be null (for DS), but not be zero!");
            m_localClientEndpoint = localClientEndpoint;
            m_callback = callback;
            m_timeFunc = updateTimeGetter;
            m_clientStates = new Dictionary<EndpointId, ClientData>();
            m_eventQueueSender = (s, e) => m_callback.SendEvent(s, false, e);

            SetGroupLimit(StateGroupEnum.FloatingObjectPhysics, 136);
        }

        protected override bool IsLocal
        {
            get { return m_localClientEndpoint != null; }
        }

        public override void Dispose()
        {
            base.Dispose();

            // Dispose client streams.
            foreach(var clientData in m_clientStates.Values)
            {
                clientData.EventQueue.Dispose();
            }

            m_sendStream.Dispose();
        }

        public void SetGroupLimit(StateGroupEnum group, int bitSizePerFrame)
        {
            m_limits.SetLimit(group, bitSizePerFrame);
        }

        public int GetGroupLimit(StateGroupEnum group)
        {
            return m_limits.GetLimit(group);
        }

        public void Replicate(IMyReplicable obj)
        {
            if (!IsTypeReplicated(obj.GetType()))
            {
                Debug.Fail(String.Format("Type '{0}' not replicated, this should be checked before calling Replicate", obj.GetType().Name));
                return;
            }

            IMyReplicable parent;

            NetworkId networkId = AddNetworkObjectServer(obj);
            m_replicables.Add(obj, out parent);
            AddStateGroups(obj);

            if (parent != null)
            {
                // Replicate to all clients which has parent
                foreach (var client in m_clientStates)
                {
                    MyReplicableClientData parentInfo;
                    if (client.Value.Replicables.TryGetValue(parent, out parentInfo))
                    {
                        AddForClient(obj, client.Key, client.Value, parentInfo.Priority,false);
                    }
                }
            }
            else
            {
                m_priorityUpdates.Add(obj);
            }

            // HACK: uncomment this to test serialization
            //m_sendStream.ResetWrite(MessageIDEnum.REPLICATION_CREATE);
            //stateData.Replicate(m_sendStream);
        }

        bool PrepareForceReplicable(IMyReplicable obj)
        {
            Debug.Assert(obj != null);
            if (obj == null || !IsTypeReplicated(obj.GetType()))
            {
                Debug.Fail(String.Format("Cannot replicate {0}, type is not replicated", obj));
                return false;
            }

            NetworkId id;
            if (!TryGetNetworkIdByObject(obj, out id))
            {
                Debug.Fail("Force replicable dependency not replicated yet!");
                //Replicate(obj); // This would cause crashes
                return false;
            }
            return true;
        }

        /// <summary>
        /// Stops sending replication create until resumed.
        /// </summary>
        public PauseToken PauseReplication()
        {
            return new PauseToken(this);
        }

        void ResumeReplication()
        {
            foreach (var client in m_clientStates)
            {
                foreach (var item in client.Value.PausedReplicables)
                {
                    SendReplicationCreate(item, client.Value, client.Key);
                }
                client.Value.PausedReplicables.Clear();
            }
        }

        /// <summary>
        /// Hack to allow thing like: CreateCharacter, Respawn sent from server
        /// </summary>
        public void ForceReplicable(IMyReplicable obj, IMyReplicable dependency = null)
        {
            ProfilerShort.Begin("ForceReplicate by dependency");
            if (PrepareForceReplicable(obj))
            {
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
                        AddForClient(obj, client.Key, client.Value, 0, true);
                    }                  
                }
            }
            ProfilerShort.End();
        }

        public void ForceReplicable(IMyEventProxy proxy, IMyEventProxy dependency)
        {
            IMyReplicable replicable = GetProxyTarget(proxy) as IMyReplicable;
            IMyReplicable dep = dependency != null ? GetProxyTarget(dependency) as IMyReplicable : null;
            Debug.Assert(replicable != null, "Proxy does not point to replicable!");
            ForceReplicable(replicable, dep);
        }

        /// <summary>
        /// Hack to allow thing like: CreateCharacter, Respawn sent from server
        /// </summary>
        public void ForceReplicable(IMyReplicable obj, EndpointId clientEndpoint)
        {
            if (m_localClientEndpoint == clientEndpoint || clientEndpoint.IsNull) // Local client has always everything
                return;

            ProfilerShort.Begin("ForceReplicate");
            if (PrepareForceReplicable(obj))
            {
                var client = m_clientStates[clientEndpoint];
                if (!client.Replicables.ContainsKey(obj))
                {
                    AddForClient(obj, clientEndpoint, client, 0,true);
                }
            }
            ProfilerShort.End();
        }

        public void ForceReplicable(IMyEventProxy proxy, EndpointId clientEndpoint)
        {
            IMyReplicable replicable = GetProxyTarget(proxy) as IMyReplicable;
            Debug.Assert(replicable != null, "Proxy does not point to replicable!");
            ForceReplicable(replicable, clientEndpoint);
        }

        public void ForceClientRefresh(IMyEventProxy objA)
        {
            if (objA == null)
            {
                Debug.Fail("Replicable A not found!");
                return;
            }

            foreach (var client in m_clientStates)
            {
                IMyReplicable replicableA = GetProxyTarget(objA) as IMyReplicable;

                if (client.Value.Replicables.ContainsKey(replicableA))
                {
                    RemoveForClient(replicableA, client.Key, client.Value, true);
                    ForceReplicable(replicableA, client.Key);
                }
            }
        }

        public void RemoveForClientIfIncomplete(IMyEventProxy objA)
        {
            if (objA == null)
            {
                Debug.Fail("Replicable A not found!");
                return;
            }

            foreach (var client in m_clientStates)
            {
                IMyReplicable replicableA = GetProxyTarget(objA) as IMyReplicable;
                if (client.Value.IsReplicablePending(replicableA))
                {
                    RemoveForClient(replicableA, client.Key, client.Value, true);
                }              
            }
        }

        public void ForceBothOrNone(IMyReplicable replicableA, IMyReplicable replicableB)
        {
            if (replicableA == null)
            {
                Debug.Fail("Replicable A not found!");
                return;
            }

            if (replicableB == null)
            {
                Debug.Fail("Replicable B not found!");
                return;
            }

            foreach (var client in m_clientStates)
            {
                bool hasA = client.Value.Replicables.ContainsKey(replicableA);
                bool hasB = client.Value.Replicables.ContainsKey(replicableB);

                if (hasA != hasB)
                {
                    if (hasA) RemoveForClient(replicableA, client.Key, client.Value, true);
                    if (hasB) RemoveForClient(replicableB, client.Key, client.Value, true);
                }
            }
        }

        public void ForceBothOrNone(IMyEventProxy objA, IMyEventProxy objB)
        {
            ForceBothOrNone(GetProxyTarget(objA) as IMyReplicable, GetProxyTarget(objB) as IMyReplicable);
        }

        /// <summary>
        /// Sends everything in the world to client. Use with extreme caution!
        /// </summary>
        public void ForceEverything(EndpointId clientEndpoint)
        {
            foreach (var replicable in m_replicables.Roots)
            {
                ForceReplicable(replicable, clientEndpoint);
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

            m_priorityUpdates.Remove(obj);
            bool isAnyClientPending = false;

            // Remove from client states, remove from client replicables, send destroy
            foreach (var client in m_clientStates)
            {
                // Damn, this id is blocked, i cannot remove it yet!
                if (client.Value.BlockedReplicables.ContainsKey(obj))
                {
                    // Mark for remove after replication ready.
                    client.Value.BlockedReplicables[obj].Remove = true;
                    if(!obj.IsChild && !m_priorityUpdates.Contains(obj))
                    {
                        m_priorityUpdates.Add(obj);
                    }
                    isAnyClientPending = true;
                    continue;
                }

                // TODO: Postpone removing for client (we don't want to peak network when a lot of objects get removed)
                if (client.Value.Replicables.ContainsKey(obj))
                {
                    RemoveForClient(obj, client.Key, client.Value, true);
                }
            }

            // if noone pending than remove replicable.
            if (!isAnyClientPending)
            {
                RemoveStateGroups(obj);
                var netId = RemoveNetworkedObject(obj);
                m_replicables.RemoveHierarchy(obj);
            }
        }

        /// <summary>
        /// Destroys replicable for all clients (used for testing and debugging).
        /// </summary>
        public void ResetForClients(IMyReplicable obj)
        {
            foreach (var client in m_clientStates)
            {
                if (client.Value.Replicables.ContainsKey(obj))
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
            // This happens when client leaves during joining
            //Debug.Assert(m_clientStates.ContainsKey(endpointId), "Client entry does not exists, bad call to OnClientLeft?");
            ClientData data;
            if (m_clientStates.TryGetValue(endpointId, out data))
            {
                while (data.Replicables.Count > 0)
                    RemoveForClient(data.Replicables.FirstPair().Key, endpointId, data, false);

                data.EventQueue.Dispose();
                m_clientStates.Remove(endpointId);
            }
        }

        public void OnClientUpdate(MyPacket packet)
        {
            ClientData clientData;
            if (!m_clientStates.TryGetValue(packet.Sender, out clientData))
                return; // Unknown client, probably kicked

            if (m_processedClients.Contains(packet.Sender))
            {
                return;
            }

            m_processedClients.Add(packet.Sender);
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
            if (m_frameCounter % 2 == 0)
            {
                m_processedClients.Clear();
            }

            m_frameCounter++;
            if (m_clientStates.Count == 0)
                return;

            // TODO: Send only limited number of objects!
            // TODO: Optimize, no need to go through all objects of all client every frame
            // Optimization 1: Spread refresh over multiple frames // DONE
            // Optimization 2: Add hierarchy (root replicable, child replicables) // WIP
            // Child replicable is replicable with dependency which does not have separate priority (will need another flag on replicable)
            // When replicable is added for client, all children are added, same for removal, child priority is never checked
            // Dependency can change during child lifetime (e.g. moving blocks between grids), replication server will check dependency once per 300 updates (5s) and update internal structures appropriatelly.
            ProfilerShort.Begin("RefreshReplicables");

            while (m_priorityUpdates.Count > 0)
            {
                var replicable = m_priorityUpdates.FirstElement();
                m_priorityUpdates.Remove(replicable);
                RefreshReplicable(replicable);
            }

            const int spreadFrameCount = 60;
            int refreshCount = m_replicables.Roots.Count / spreadFrameCount + 1;
            for (int i = 0; i < refreshCount; i++)
            {
                var replicable = m_replicables.GetNextForUpdate();
                if (replicable == null)
                    break;

                m_replicables.RefreshChildrenHierarchy(replicable);
                RefreshReplicable(replicable);
            }
            ProfilerShort.End();

            ProfilerShort.Begin("SendStateSync");
            foreach (var client in m_clientStates)
            {
                SendStateSync(client.Value);
            }
            ProfilerShort.End();
        }

        private void RefreshReplicable(IMyReplicable replicable)
        {
            ProfilerShort.Begin("m_timeFunc");
            MyTimeSpan now = m_timeFunc();
            ProfilerShort.End();

            foreach (var client in m_clientStates)
            {
                ProfilerShort.Begin("RefreshReplicablePerClient");

                ProfilerShort.Begin("TryGetValue Replicables");
                MyReplicableClientData replicableInfo;
                bool hasObj = client.Value.Replicables.TryGetValue(replicable, out replicableInfo);
                ProfilerShort.End();

                ProfilerShort.Begin("GetPriority");
                ProfilerShort.Begin(replicable.GetType().Name);
                float priority = replicable.GetPriority(new MyClientInfo(client.Value));

                if (hasObj)
                {
                    replicableInfo.Priority = priority;
                }

                bool isRelevant = priority > 0;
                ProfilerShort.End();
                ProfilerShort.End();

                if (isRelevant && !hasObj)
                {
                    ProfilerShort.Begin("CheckReady");
                    var dependency = replicable.GetDependency();
                    isRelevant = dependency == null || client.Value.IsReplicableReady(dependency);
                    ProfilerShort.End();

                    if (isRelevant)
                    {
                        ProfilerShort.Begin("AddForClient");
                        AddForClient(replicable, client.Key, client.Value, priority, false);
                        ProfilerShort.End();
                    }
                }
                else if (hasObj)
                {
                    ProfilerShort.Begin("UpdateSleepAndRemove");
                    // Hysteresis
                    replicableInfo.UpdateSleep(isRelevant, now);
                    if (replicableInfo.ShouldRemove(now, MaxSleepTime))
                        RemoveForClient(replicable, client.Key, client.Value, true);
                    ProfilerShort.End();
                }

                ProfilerShort.End();
            }
        }

        private void SendStateSync(ClientData clientData)
        {
            var now = m_timeFunc();

            if (clientData.StateGroups.Count == 0)
                return;

            EndpointId endpointId = clientData.State.EndpointId;

            // TODO: Limit events
            clientData.EventQueue.Send();

            using (m_tmpStreamingEntries)
            {
                using (m_tmpSortEntries)
                {
                    ProfilerShort.Begin("UpdateGroupPriority");
                    foreach (var entry in clientData.StateGroups.Values)
                    {
                        // No state sync for pending or sleeping replicables
                        if (entry.Owner != null && !clientData.Replicables[entry.Owner].HasActiveStateSync)
                            continue;

                        entry.FramesWithoutSync++;
                        entry.Priority = entry.Group.GetGroupPriority(entry.FramesWithoutSync, new MyClientInfo(clientData));

                        if (entry.Priority > 0)
                        {
                            if (entry.Owner != null)
                            {
                                m_tmpSortEntries.Add(entry);
                            }
                            else
                            {
                                if (entry.Group.GroupType != StateGroupEnum.Streamining)
                                {
                                    Debug.Fail("Non streaming group !");
                                }
                                else
                                {
                                    m_tmpStreamingEntries.Add(entry);
                                }
                            }
                        }
                    }

                    ProfilerShort.End();

                    ProfilerShort.Begin("Sort entities");
                    m_tmpSortEntries.Sort(MyStateDataEntryComparer.Instance);
                    ProfilerShort.End();

                    byte firstWaitingPacket = (byte)(clientData.LastReceivedAckId - m_outOfOrderAcceptThreshold);
                    byte nextPacketId = (byte)(clientData.StateSyncPacketId + 1);

                    if (clientData.WaitingForReset || nextPacketId == firstWaitingPacket)
                    {
                        clientData.WaitingForReset = true;
                        return;
                    }

                    clientData.StateSyncPacketId++;

                    m_sendStream.ResetWrite();
                    m_sendStream.WriteBool(false);
                    m_sendStream.WriteByte(clientData.StateSyncPacketId);

                    int MTUBytes = (int)m_callback.GetMTUSize(endpointId);
                    int messageBitSize = 8 * (MTUBytes - 8 - 1); // MTU - headers

                    // TODO: Rewrite
                    int maxToSend = MTUBytes / 8; // lets assume the shortest message is 8 Bytes long
                    int sent = 0;

                    m_limits.Clear();
                    int numOverLoad = 0;
                    ProfilerShort.Begin("serializing");
                    // TODO:SK limit to N in panic entries per packet
                    foreach (var entry in m_tmpSortEntries)
                    {
                        ProfilerShort.Begin("serializing entry counter");
                        var oldWriteOffset = m_sendStream.BitPosition;
                        m_sendStream.WriteNetworkId(entry.GroupId);
                        entry.Group.Serialize(m_sendStream, clientData.State, clientData.StateSyncPacketId, messageBitSize);

                        int bitsWritten = m_sendStream.BitPosition - oldWriteOffset;
                        if (bitsWritten > 0 && m_sendStream.BitPosition <= messageBitSize && m_limits.Add(entry.Group.GroupType, bitsWritten))
                        {
                            clientData.PendingStateSyncAcks[clientData.StateSyncPacketId].Add(entry.Group);
                            sent++;
                            entry.FramesWithoutSync = 0;
                        }
                        else
                        {
                            numOverLoad++;
                            // When serialize returns false, restore previous bit position and tell group it was not delivered
                            entry.Group.OnAck(clientData.State, clientData.StateSyncPacketId, false);
                            m_sendStream.SetBitPositionWrite(oldWriteOffset);
                        }
                        ProfilerShort.End();
                        if (sent >= maxToSend || m_sendStream.BitPosition >= messageBitSize || numOverLoad >10)
                        {
                            break;
                        }
                    }

                    ProfilerShort.End();
                    m_callback.SendStateSync(m_sendStream, endpointId,false);

                    if (m_tmpStreamingEntries.Count > 0)
                    {
                        messageBitSize = m_callback.GetMTRSize(endpointId) * 8;
                        ProfilerShort.Begin("Sort streaming entities");
                        m_tmpStreamingEntries.Sort(MyStateDataEntryComparer.Instance);
                        clientData.StateSyncPacketId++;
                        ProfilerShort.End();

                        var entry = m_tmpStreamingEntries.FirstOrDefault();

                        m_sendStream.ResetWrite();
                        m_sendStream.WriteBool(true);
                        m_sendStream.WriteByte(clientData.StateSyncPacketId);
                        var oldWriteOffset = m_sendStream.BitPosition;

                        m_sendStream.WriteNetworkId(entry.GroupId);

                        ProfilerShort.Begin("serialize streaming entities");
                        entry.Group.Serialize(m_sendStream, clientData.State, clientData.StateSyncPacketId, messageBitSize);
                        ProfilerShort.End();

                        int bitsWritten = m_sendStream.BitPosition - oldWriteOffset;

                        if (m_limits.Add(entry.Group.GroupType, bitsWritten))
                        {
                            clientData.PendingStateSyncAcks[clientData.StateSyncPacketId].Add(entry.Group);

                            entry.FramesWithoutSync = 0;
                        }
                        else
                        {
                            // When serialize returns false, restore previous bit position and tell group it was not delivered
                            entry.Group.OnAck(clientData.State, clientData.StateSyncPacketId, false);
                        }
                        m_callback.SendStateSync(m_sendStream, endpointId,true);
                    }
                    //Server.SendMessage(m_sendStream, guid, PacketReliabilityEnum.UNRELIABLE, PacketPriorityEnum.MEDIUM_PRIORITY, MyChannelEnum.StateDataSync);
                }
            }
        }

        private void AddForClient(IMyReplicable replicable, EndpointId clientEndpoint, ClientData clientData, float priority,bool force)
        {
            if (clientData.HasReplicable(replicable))
                return; // already added

            ProfilerShort.Begin("AddClientReplicable");
            AddClientReplicable(replicable, clientData, priority,force);
            ProfilerShort.End();

            ProfilerShort.Begin("SendReplicationCreate");
            SendReplicationCreate(replicable, clientData, clientEndpoint);
            ProfilerShort.End();         
        }

        private void RemoveForClient(IMyReplicable replicable, EndpointId clientEndpoint, ClientData clientData, bool sendDestroyToClient)
        {
            // It should not be in this list in normal situation, but there are many overrides that
            // can remove replicable before it finished streaming. Just remove it now than.
            clientData.BlockedReplicables.Remove(replicable);

            m_replicables.RefreshChildrenHierarchy(replicable);

            foreach (var child in m_replicables.GetChildren(replicable))
            {
                RemoveForClient(child, clientEndpoint, clientData, sendDestroyToClient);
            }

            if (sendDestroyToClient)
            {
                ProfilerShort.Begin("SendReplicationDestroy");
                SendReplicationDestroy(replicable, clientData, clientEndpoint);
                ProfilerShort.End();
            }
            ProfilerShort.Begin("RemoveClientReplicable");
            RemoveClientReplicable(replicable, clientData);
            ProfilerShort.End();
        }

        void SendReplicationCreate(IMyReplicable obj, ClientData clientData, EndpointId clientEndpoint)
        {
            
            if (m_replicationPaused)
            {
                clientData.PausedReplicables.Add(obj);
                return;
            }

            ProfilerShort.Begin("PrepareSave");
            var typeId = GetTypeIdByType(obj.GetType());
            var networkId = GetNetworkIdByObject(obj);

            var groups = m_replicableGroups[obj];
            Debug.Assert(groups.Count <= 255, "Unexpectedly high number of groups");

            m_sendStream.ResetWrite();
            m_sendStream.WriteTypeId(typeId);
            m_sendStream.WriteNetworkId(networkId);


            var streamable = obj as IMyStreamableReplicable;

            bool sendStreamed = streamable != null && streamable.NeedsToBeStreamed;
            if (streamable != null && streamable.NeedsToBeStreamed == false)
            {
                m_sendStream.WriteByte((byte)(groups.Count - 1));
            }
            else
            {
                m_sendStream.WriteByte((byte)groups.Count);
            }


            for (int i = 0; i < groups.Count; i++)
            {
                if (sendStreamed == false && groups[i].GroupType == StateGroupEnum.Streamining)
                {
                    continue;
                }
                m_sendStream.WriteNetworkId(GetNetworkIdByObject(groups[i]));
            }

            ProfilerShort.End();

            ProfilerShort.Begin("SaveReplicable");

            if (sendStreamed)
            {
                clientData.Replicables[obj].IsStreaming = true;
                m_callback.SendReplicationCreateStreamed(m_sendStream, clientEndpoint);
            }
            else
            {
                obj.OnSave(m_sendStream);
                ProfilerShort.Begin("Send");
                m_callback.SendReplicationCreate(m_sendStream, clientEndpoint);
                ProfilerShort.End();
            }
            ProfilerShort.End();
       
            //Server.SendMessage(m_sendStream, clientId, PacketReliabilityEnum.RELIABLE, PacketPriorityEnum.LOW_PRIORITY, MyChannelEnum.Replication);
        }

        void SendReplicationDestroy(IMyReplicable obj, ClientData clientData, EndpointId clientEndpoint)
        {
            if (m_replicationPaused && clientData.PausedReplicables.Remove(obj))
            {
                return;
            }

            m_sendStream.ResetWrite();
            m_sendStream.WriteNetworkId(GetNetworkIdByObject(obj));
            m_callback.SendReplicationDestroy(m_sendStream, clientEndpoint);
            //Server.SendMessage(m_sendStream, clientId, PacketReliabilityEnum.RELIABLE, PacketPriorityEnum.LOW_PRIORITY, MyChannelEnum.Replication);
        }

        public void ReplicableReady(MyPacket packet)
        {
            m_receiveStream.ResetRead(packet);
            var networkId = m_receiveStream.ReadNetworkId();
            bool loaded = m_receiveStream.ReadBool();

            Debug.Assert(m_clientStates.ContainsKey(packet.Sender), "Client data not found");

            // Client left can be received in another channel (e.g. through lobby), so it may happen that it comes before this packet
            ClientData clientData;
            if (m_clientStates.TryGetValue(packet.Sender, out clientData))
            {
                var replicable = GetObjectByNetworkId(networkId) as IMyReplicable;

                MyReplicableClientData replicableClientData;
                if (replicable != null && !loaded)
                {
                    RemoveForClient(replicable, packet.Sender, clientData, false);
                }
                else if (replicable != null && clientData.Replicables.TryGetValue(replicable, out replicableClientData))
                {
                    // Replicable can be destroyed for client or destroyed completely at this point
                    Debug.Assert(replicableClientData.IsPending == true, "Replicable ready from client, but it's not pending for client");
                    replicableClientData.IsPending = false;
                    replicableClientData.IsStreaming = false;

                    foreach (var child in m_replicables.GetChildren(replicable))
                    {
                        AddForClient(child, packet.Sender, clientData, replicableClientData.Priority,false);
                    }

                    m_tmp.Clear();
                    foreach (var forcedReplicable in clientData.ForcedReplicables)
                    {
                        if (forcedReplicable.Value == replicable)
                        {
                            m_tmp.Add(forcedReplicable.Key);
                            if (!clientData.Replicables.ContainsKey(forcedReplicable.Key) && m_replicableGroups.ContainsKey(forcedReplicable.Key))
                            {
                                AddForClient(forcedReplicable.Key,packet.Sender, clientData, 0,true);
                            }
                        }
                    }
                    foreach (var replicableToRemove in m_tmp)
                    {
                        clientData.ForcedReplicables.Remove(replicableToRemove);
                    }
                    m_tmp.Clear();
                }

                // Check if this replicable was blocked, if yes than remove from blocking list.
                if (replicable != null)
                    this.ProcessBlocker(replicable, packet.Sender, clientData, null);

            }
        }

        private bool ProcessBlocker(IMyReplicable replicable, EndpointId endpoint, ClientData client, IMyReplicable parent)
        {
            if (client.BlockedReplicables.ContainsKey(replicable))
            {
                MyDestroyBlocker destroyBlocker = client.BlockedReplicables[replicable];
                if (destroyBlocker.IsProcessing)
                    return true;
                
                destroyBlocker.IsProcessing = true;

                foreach(IMyReplicable childRepl in destroyBlocker.Blockers)
                {

                    // Check if can remove
                    if (!client.IsReplicableReady(replicable) || !client.IsReplicableReady(childRepl))
                    {
                        destroyBlocker.IsProcessing = false;
                        return false;
                    }

                    bool childSuccess = true;
                    if (childRepl != parent)
                    {
                        childSuccess = this.ProcessBlocker(childRepl, endpoint, client, replicable);
                    }

                    if(!childSuccess)
                    {
                        destroyBlocker.IsProcessing = false;
                        return false;
                    }

                }

                // We can clean replicables only if all destory blockers are removed.
                client.BlockedReplicables.Remove(replicable);
                if (destroyBlocker.Remove)
                    this.RemoveForClient(replicable, endpoint, client, true);
                    
                destroyBlocker.IsProcessing = false;
            }

            return true;

        }

        public void AddStateGroups(IMyReplicable replicable)
        {
            using (m_tmpGroups)
            {
                IMyStreamableReplicable streamable = replicable as IMyStreamableReplicable;
                if (streamable != null)
                {
                    var group = streamable.GetStreamingStateGroup();
                    m_tmpGroups.Add(group);
                }

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
                group.Destroy();
            }
            m_replicableGroups.Remove(replicable);
        }

        private void AddClientReplicable(IMyReplicable replicable, ClientData clientData, float priority,bool force)
        {
            // Add replicable
            clientData.Replicables.Add(replicable, new MyReplicableClientData() { Priority = priority });

            // Add state groups
            foreach (var group in m_replicableGroups[replicable])
            {
                var netId = GetNetworkIdByObject(group);
                IMyReplicable parent = replicable;

                if (group.GroupType == StateGroupEnum.Streamining)
                {
                    if((replicable as IMyStreamableReplicable).NeedsToBeStreamed == false)
                    {
                        continue;
                    }
                    parent = null;
                }
            
                clientData.StateGroups.Add(group, new MyStateDataEntry(parent, netId, group));
                group.CreateClientData(clientData.State);

                if (force)
                {
                    group.ForceSend(clientData.State);
                }
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
                return isReliable || replicableInfo.HasActiveStateSync || replicableInfo.IsStreaming;
            }
            else if (eventInstance is IMyReplicable && (client.Replicables.TryGetValue((IMyReplicable)eventInstance, out replicableInfo) || m_fixedObjects.Contains(eventInstance)))
            {
                // Event inherits replicated object priority
                priority = replicableInfo.Priority;
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

        internal override bool DispatchBlockingEvent(BitStream stream, CallSite site, EndpointId target, IMyNetObject targetReplicable, IMyNetObject blockingReplicable, float unreliablePriority)
        {
            var blockedRepl = blockingReplicable as IMyReplicable;
            var replicable = targetReplicable as IMyReplicable;

            if (site.HasBroadcastFlag || site.HasBroadcastExceptFlag)
            {
                foreach (var client in m_clientStates)
                {
                    if (site.HasBroadcastExceptFlag && client.Key == target)
                        continue;

                    float priority;
                    if (ShouldSendEvent(targetReplicable, site.IsReliable, client.Value, out priority))
                    {
                        // Register networkId as blocked and streaming has to finish for it.
                        this.TryAddBlockerForClient(client.Value, replicable, blockedRepl);

                    }
                }
            }
            else if (site.HasClientFlag && m_localClientEndpoint != target)
            {
                ClientData clientData;
                // Register networkId as blocked and streaming has to finish for it.
                if (m_clientStates.TryGetValue(target, out clientData))
                {
                    this.TryAddBlockerForClient(clientData, replicable, blockedRepl);
                }
            }

            return DispatchEvent(stream, site, target, targetReplicable, unreliablePriority);
        }

        private void TryAddBlockerForClient(ClientData clientData, IMyReplicable targetReplicable, IMyReplicable blockingReplicable)
        {
            // Register networkId as blocked and streaming has to finish for it.
            if (!clientData.IsReplicableReady(targetReplicable) || !clientData.IsReplicableReady(blockingReplicable)
                || clientData.BlockedReplicables.ContainsKey(targetReplicable) || clientData.BlockedReplicables.ContainsKey(blockingReplicable))
            {
                // target to blocker
                MyDestroyBlocker destroyBlocker;
                if(!clientData.BlockedReplicables.TryGetValue(targetReplicable, out destroyBlocker))
                {
                    destroyBlocker = new MyDestroyBlocker();
                    clientData.BlockedReplicables.Add(targetReplicable, destroyBlocker);
                }
                destroyBlocker.Blockers.Add(blockingReplicable);

                // blocker to target
                MyDestroyBlocker destroyBlocker2;
                if (!clientData.BlockedReplicables.TryGetValue(blockingReplicable, out destroyBlocker2))
                {
                    destroyBlocker2 = new MyDestroyBlocker();
                    clientData.BlockedReplicables.Add(blockingReplicable, destroyBlocker2);
                }
                destroyBlocker2.Blockers.Add(targetReplicable);
            }
        }

        internal override bool DispatchEvent(BitStream stream, CallSite site, EndpointId target, IMyNetObject eventInstance, float unreliablePriority)
        {
            // Server can call server method through RaiseEvent, it's valid and common when using same code on client and server.
            //Debug.Assert(site.HasClientFlag || site.HasBroadcastFlag || site.HasBroadcastExceptFlag, String.Format("Event '{0}' does not have Client, Broadcast or BroadcastExcept flag, it can't be invoked on client!", site));
                    
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
            else if (site.HasClientFlag && m_localClientEndpoint != target)
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

            return ShouldServerInvokeLocally(site, m_localClientEndpoint, target);
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

        #region Debug methods

        public override string GetMultiplayerStat()
        {
            StringBuilder multiplayerStat = new StringBuilder();

            string baseStats = base.GetMultiplayerStat();

            multiplayerStat.Append(baseStats);

            multiplayerStat.AppendLine("Client state info:");
            foreach(var clientState in m_clientStates)
            {
                string clientStateInfo = "    Endpoint: " + clientState.Key + ", Blocked Close Msgs Count: " + clientState.Value.BlockedReplicables.Count;
                multiplayerStat.AppendLine(clientStateInfo);
            }

            return multiplayerStat.ToString();
        }

        #endregion

    }
}
