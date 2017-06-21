#define AABB_REPLICABLES

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using VRage.Collections;
using VRage.Library.Collections;
using VRage.Library.Utils;
using VRage.Profiler;
using VRage.Replication;
using VRage.Serialization;
using VRage.Trace;
using VRage.Utils;
using VRageMath;

namespace VRage.Network
{
    public enum JoinResult
    {
        OK,
        AlreadyJoined,
        TicketInvalid,
        SteamServersOffline,
        NotInGroup,
        GroupIdInvalid,
        ServerFull,
        BannedByAdmins,

        TicketCanceled,
        TicketAlreadyUsed,
        LoggedInElseWhere,
        NoLicenseOrExpired,
        UserNotConnected,
        VACBanned,
        VACCheckTimedOut
    }

    public struct JoinResultMsg
    {
        public JoinResult JoinResult;

        public ulong Admin;
    }

    public struct ServerDataMsg
    {
        [Serialize(MyObjectFlags.Nullable)]
        public string WorldName;

        public MyGameModeEnum GameMode;

        public float InventoryMultiplier;

        public float AssemblerMultiplier;

        public float RefineryMultiplier;

        [Serialize(MyObjectFlags.Nullable)]
        public string HostName;

        public ulong WorldSize;

        public int AppVersion;

        public int MembersLimit;

        [Serialize(MyObjectFlags.Nullable)]
        public string DataHash;

        public float WelderMultiplier;

        public float GrinderMultiplier;
    }

    public struct KeyValueDataMsg
    {
        public MyStringHash Key;
        public string Value;
    }

    public struct ChatMsg
    {
        public string Text;
        public ulong Author; // Ignored when sending message from client to server
    }

    public struct ConnectedClientDataMsg
    {
        public ulong SteamID;

        [Serialize(MyObjectFlags.Nullable)]
        public string Name;
        
        public bool IsAdmin;
        public bool Join;
        [Serialize(MyObjectFlags.Nullable)]
        public byte[] Token;
    }

    public class MyReplicationServer : MyReplicationLayer
    {
        internal class MyDestroyBlocker
        {
            public bool Remove = false;
            public bool IsProcessing = false;
            public List<IMyReplicable> Blockers = new List<IMyReplicable>();
        }

        public struct UpdateLayerDesc
        {
            public float Radius;
            public int UpdateInterval;
            public int SendInterval;
        }

        public struct UpdateLayer
        {
            public UpdateLayerDesc Descriptor;
            public HashSet<IMyReplicable> Replicables;
            public MyDistributedUpdater<List<IMyReplicable>, IMyReplicable> Updater;
            public MyDistributedUpdater<List<IMyReplicable>, IMyReplicable> Sender;
        }

        internal class ClientData
        {

            public readonly MyClientStateBase State;

            public readonly Dictionary<IMyReplicable, IMyReplicable> ForcedReplicables = new Dictionary<IMyReplicable, IMyReplicable>();

            // Additional per replicable information for client
            public readonly MyConcurrentDictionary<IMyReplicable, MyReplicableClientData> Replicables = new MyConcurrentDictionary<IMyReplicable, MyReplicableClientData>(InstanceComparer<IMyReplicable>.Default);

            // Temporary blocked network id by blocking event. Bool flag indicates if it should be destroyed right after it is replicationReady.
            public readonly MyConcurrentDictionary<IMyReplicable, MyDestroyBlocker> BlockedReplicables = new MyConcurrentDictionary<IMyReplicable, MyDestroyBlocker>();

            // Additional per state-group information for client
            public readonly MyConcurrentDictionary<IMyStateGroup, MyStateDataEntry> StateGroups = new MyConcurrentDictionary<IMyStateGroup, MyStateDataEntry>(InstanceComparer<IMyStateGroup>.Default);
            public readonly MyConcurrentHashSet<IMyStateGroup> DirtyGroups = new MyConcurrentHashSet<IMyStateGroup>();
            public readonly List<IMyStateGroup> DirtyGroupsToRemove = new List<IMyStateGroup>();
            public readonly MyPacketQueue EventQueue;

            public readonly HashSet<IMyReplicable> PausedReplicables = new HashSet<IMyReplicable>();

            // First sent packet id is 1 (it's incremented first)
            public byte StateSyncPacketId = 0;
            public byte LastReceivedAckId = 0;
            public byte LastStateSyncPacketId = 0;
            public byte LastClientPacketId = 0;
            public byte LastProcessedClientPacketId = 255;
            public MyTimeSpan StartingServerTimeStamp = MyTimeSpan.Zero;
            public MyTimeSpan LastClientRealtime;

            public float PriorityMultiplier = 1;

            public bool WaitingForReset = false;

            public bool IsReady = false;

            public UpdateLayer[] UpdateLayers;


            // 16 KB per client
            public readonly List<IMyStateGroup>[] PendingStateSyncAcks = Enumerable.Range(0, 256).Select(s => new List<IMyStateGroup>(8)).ToArray();
            public MyPacketTracker ClientTracker = new MyPacketTracker();
            public MyPacketStatistics ClientStats = new MyPacketStatistics();

            public struct MyOrderedPacket
            {
                public byte Id;
                public MyPacket Packet;

                public override string ToString()
                {
                    return Id.ToString();
                }
            }
            public List<MyOrderedPacket> IncomingBuffer = new List<MyOrderedPacket>();
            public bool IncomingBuffering = true;
            public bool ProcessedPacket;
            public MyTimeSpan LastStateSyncTimeStamp;

            public ClientData(MyClientStateBase emptyState, Action<BitStream, EndpointId> sender)
            {
                State = emptyState;
                EventQueue = new MyPacketQueue(512, sender);

#if AABB_REPLICABLES
                UpdateLayers = new UpdateLayer[UpdateLayerDescriptors.Length];
                for (int i = 0; i < UpdateLayerDescriptors.Length; i++)
                {
                    var desc = UpdateLayerDescriptors[i];
                    UpdateLayers[i] = new UpdateLayer()
                    {
                        Descriptor = desc,
                        Replicables = new HashSet<IMyReplicable>(),
                        Updater = new MyDistributedUpdater<List<IMyReplicable>, IMyReplicable>(desc.UpdateInterval),
                        Sender = new MyDistributedUpdater<List<IMyReplicable>, IMyReplicable>(desc.SendInterval)
                    };
                }
#endif
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

#if AABB_REPLICABLES
        // Defines distance layers from client center and how often replicables in this layer should be updated
        static UpdateLayerDesc[] UpdateLayerDescriptors = 
        {
            new UpdateLayerDesc() { Radius = 20, UpdateInterval = 60, SendInterval = 1 },
            new UpdateLayerDesc() { Radius = 80, UpdateInterval = 100, SendInterval = 5  },
            new UpdateLayerDesc() { Radius = 300, UpdateInterval = 200, SendInterval = 10  },
            new UpdateLayerDesc() { Radius = 800, UpdateInterval = 300, SendInterval = 20  },
            new UpdateLayerDesc() { Radius = 3000, UpdateInterval = 500, SendInterval = 50  },
            //More distant replicables are discarded
        };
#endif


        const int MAX_NUM_STATE_SYNC_PACKETS_PER_CLIENT = 7;
        private bool m_replicationPaused = false;
        private EndpointId? m_localClientEndpoint;
        private readonly bool m_usePlayoutDelayBuffer;
        private IReplicationServerCallback m_callback;
        private Action<BitStream, EndpointId> m_eventQueueSender;
        private CacheList<IMyStateGroup> m_tmpGroups = new CacheList<IMyStateGroup>(4);
        private CacheList<MyStateDataEntry> m_tmpSortEntries = new CacheList<MyStateDataEntry>();
        private CacheList<MyStateDataEntry> m_tmpStreamingEntries = new CacheList<MyStateDataEntry>();
        private CacheList<MyStateDataEntry> m_tmpSentEntries = new CacheList<MyStateDataEntry>();
        HashSet<IMyReplicable> m_toDeleteHash = new HashSet<IMyReplicable>();
        CacheList<IMyReplicable> m_tmp = new CacheList<IMyReplicable>();
        HashSet<IMyReplicable> m_tmpHash = new HashSet<IMyReplicable>();
        HashSet<IMyReplicable> m_layerUpdateHash = new HashSet<IMyReplicable>();

        private MyBandwidthLimits m_limits = new MyBandwidthLimits();
        private ConcurrentCachingHashSet<IMyReplicable> m_priorityUpdates = new ConcurrentCachingHashSet<IMyReplicable>();
        private MyTimeSpan m_serverTimeStamp = MyTimeSpan.Zero;
        private long m_serverFrame = 0;

        // Packet received out of order with number preceding closely last packet is accepted.
        // When multiplied by 16.66ms, this gives you minimum time after which packet can be resent.
        private const byte m_outOfOrderAcceptThreshold = 6; // ~100 ms

        // How much to increase packed ID after reset, this is the number of packets client call still ACK.
        private const byte m_outOfOrderResetProtection = 64;

        public MyTimeSpan MaxSleepTime = MyTimeSpan.FromSeconds(5);

        public static SerializableVector3I StressSleep = new SerializableVector3I(0,0,0);
        public int Stats_ObjectsRefreshed = 0;
        public int Stats_ObjectsSent = 0;

        /// <summary>
        /// All replicables on server.
        /// </summary>
#if !AABB_REPLICABLES
        MyReplicablesBase m_replicables = new MyReplicablesLinear();
#else
        MyReplicablesBase m_replicables = new MyReplicablesAABB();
#endif 

        /// <summary>
        /// All replicable state groups.
        /// </summary>
        Dictionary<IMyReplicable, List<IMyStateGroup>> m_replicableGroups = new Dictionary<IMyReplicable, List<IMyStateGroup>>();

        /// <summary>
        /// Network objects and states which are actively replicating to clients.
        /// </summary>
        Dictionary<EndpointId, ClientData> m_clientStates = new Dictionary<EndpointId, ClientData>();

        static FastResourceLock tmpGroupsLock = new FastResourceLock();

        public MyReplicationServer(IReplicationServerCallback callback, EndpointId? localClientEndpoint,
            bool usePlayoutDelayBuffer)
            : base(true)
        {
            Debug.Assert(localClientEndpoint == null || localClientEndpoint.Value.IsValid, "localClientEndpoint can be null (for DS), but not be zero!");
            m_localClientEndpoint = localClientEndpoint;
            m_usePlayoutDelayBuffer = usePlayoutDelayBuffer;
            m_callback = callback;
            m_clientStates = new Dictionary<EndpointId, ClientData>();
            m_eventQueueSender = (s, e) => m_callback.SendEvent(s, false, e);
        }

        public override void Dispose()
        {
            base.Dispose();

            // Dispose client streams.
            foreach(var clientData in m_clientStates.Values)
            {
                clientData.EventQueue.Dispose();
            }

            SendStream.Dispose();
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


            if (obj.IsReadyForReplication)
            {
                m_priorityUpdates.Add(obj);
            }
            else
            {
                obj.ReadyForReplicationAction.Add( obj, delegate() 
                { 
                    Replicate(obj); 
                } 
                );
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

            // HACK: uncomment this to test serialization
            //m_sendStream.ResetWrite(MessageIDEnum.REPLICATION_CREATE);
            //stateData.Replicate(m_sendStream);
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
        public void ForceReplicable(IMyReplicable obj, IMyReplicable parent = null)
        {
            Debug.Assert(obj != null, "Null replicable!");
            if (obj == null)
                return;

            ProfilerShort.Begin("ForceReplicate by dependency");

            foreach (var client in m_clientStates)
            {
                if (parent != null)
                {
                    if (!client.Value.Replicables.ContainsKey(parent))
                    {
                        continue;
                    }
                }

                if (!client.Value.Replicables.ContainsKey(obj))
                {
                    AddForClient(obj, client.Key, client.Value, 0, true);
                }
            }
            ProfilerShort.End();
        }

        public void ForceReplicable(IMyEventProxy proxy, IMyEventProxy dependency)
        {
            IMyReplicable replicable = GetProxyTarget(proxy) as IMyReplicable;
            IMyReplicable dep = dependency != null ? GetProxyTarget(dependency) as IMyReplicable : null;
            Debug.Assert(replicable != null, "Proxy does not point to replicable!");
            if (replicable == null)
                return;

            ForceReplicable(replicable, dep);
        }

        /// <summary>
        /// Hack to allow thing like: CreateCharacter, Respawn sent from server
        /// </summary>
        public void ForceReplicable(IMyReplicable obj, EndpointId clientEndpoint)
        {
            if (m_localClientEndpoint == clientEndpoint || clientEndpoint.IsNull) // Local client has always everything
                return;

            Debug.Assert(obj != null, "Null replicable!");
            if (obj == null)
                return;

            Debug.Assert(m_clientStates.ContainsKey(clientEndpoint), "Replication for non existing client");
            if (!m_clientStates.ContainsKey(clientEndpoint))
                return;

            ProfilerShort.Begin("ForceReplicate");
            var client = m_clientStates[clientEndpoint];
            if (!client.Replicables.ContainsKey(obj))
            {
                AddForClient(obj, clientEndpoint, client, 0,true);
            }
            ProfilerShort.End();
        }

        public void ForceReplicable(IMyEventProxy proxy, EndpointId clientEndpoint)
        {
            IMyReplicable replicable = GetProxyTarget(proxy) as IMyReplicable;
            Debug.Assert(replicable != null, "Proxy does not point to replicable!");
            if (replicable == null)
                return;
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
                if (replicableA == null)
                    continue;

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
            m_replicables.IterateRoots(replicable => ForceReplicable(replicable, clientEndpoint));
        }

        public void Destroy(IMyReplicable obj)
        {
            Debug.Assert(obj != null);
            if (!IsTypeReplicated(obj.GetType()))
            {
                return;
            }

            m_priorityUpdates.ApplyChanges();
                                              //may be ready but not processed 
            if (!obj.IsReadyForReplication || obj.ReadyForReplicationAction.Count > 0) 
            {
                return;
            }

            var id = GetNetworkIdByObject(obj);
            if (id.IsInvalid) //TODO: Can be invalid in ME!
            {
               // Debug.Fail("Destroying object which is not present");
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
                    if(!obj.HasToBeChild && !m_priorityUpdates.Contains(obj))
                    {
                        m_priorityUpdates.Add(obj);
                    }
                    isAnyClientPending = true;
                    continue;
                }

                RemoveForClient(obj, client.Key, client.Value, true);
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

        public void OnClientConnected(EndpointId endpointId, MyClientStateBase clientState)
        {
            bool hasClient = m_clientStates.ContainsKey(endpointId);
            Debug.Assert(!hasClient, "Client entry already exists, bad call to OnClientJoined?");
            if (hasClient)
            {
                return;
            }

            clientState.EndpointId = endpointId;       
            m_clientStates.Add(endpointId, new ClientData(clientState, m_eventQueueSender));
        }

        public void OnClientReady(EndpointId endpointId)
        {
            ClientData clientState;
            if(m_clientStates.TryGetValue(endpointId,out clientState))
            {
                clientState.IsReady = true;
            }
         
            SendServerData(endpointId);
        }

        private void SendServerData(EndpointId endpointId)
        {
            SendStream.ResetWrite();
            SerializeTypeTable(SendStream);
            m_callback.SendServerData(SendStream, endpointId);
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

        public override void SetPriorityMultiplier(EndpointId id, float priority)
        {
            ClientData clientData;
            if (m_clientStates.TryGetValue(id, out clientData))
            {
                clientData.PriorityMultiplier = priority;
            }
        }


        public void OnClientJoined(EndpointId endpointId,MyClientStateBase clientState)
        {
            OnClientConnected(endpointId,clientState);
        }

        private const int MINIMUM_INCOMING_BUFFER = 4;
        private const int MAXIMUM_INCOMING_BUFFER = 10;

        private void AddIncomingPacketSorted(ClientData clientData, ClientData.MyOrderedPacket packet)
        {
            int idx = clientData.IncomingBuffer.Count - 1;
            while (idx >= 0 && packet.Id < clientData.IncomingBuffer[idx].Id && !(packet.Id < 64 && clientData.IncomingBuffer[idx].Id > 192))
                idx--;
            idx++;

            clientData.IncomingBuffer.Insert(idx, packet);
        }

        public void OnClientAcks(MyPacket packet)
        {
            ClientData clientData;
            if (!m_clientStates.TryGetValue(packet.Sender, out clientData))
                return; // Unknown client, probably kicked

            ReceiveStream.ResetRead(packet, false);

            // Read ACKs
            clientData.LastStateSyncPacketId = ReceiveStream.ReadByte();
            byte count = ReceiveStream.ReadByte();
            for (int i = 0; i < count; i++)
            {
                OnAck(ReceiveStream.ReadByte(), clientData);
            }
            ReceiveStream.CheckTerminator();

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
        }

        public void OnClientUpdate(MyPacket packet)
        {
            ClientData clientData;
            if (!m_clientStates.TryGetValue(packet.Sender, out clientData))
                return; // Unknown client, probably kicked

            if (m_usePlayoutDelayBuffer)
            {
                ReceiveStream.ResetRead(packet, false);
                var packetId = ReceiveStream.ReadByte();
                var orderedPacket = new ClientData.MyOrderedPacket {Id = packetId, Packet = packet};
                orderedPacket.Packet.Data = (byte[])packet.Data.Clone();
                AddIncomingPacketSorted(clientData, orderedPacket);
            }
            else
            {
                ProcessIncomingPacket(clientData, packet, false);
                clientData.ProcessedPacket = true;
            }
        }

        [System.Runtime.ExceptionServices.HandleProcessCorruptedStateExceptions]
        [System.Security.SecurityCriticalAttribute]
        public override void UpdateBefore()
        {
            if (m_usePlayoutDelayBuffer)
            {
                ulong clientIdToDisconnect = ulong.MaxValue;
                foreach (var client in m_clientStates)
                {
                    try
                    {
                        UpdateIncoming(client.Value);
                    }
                    catch (Exception ex)
                    {
                        MyLog.Default.WriteLine(ex);
                        clientIdToDisconnect = client.Key.Value;
                    }
                }
                if (clientIdToDisconnect != ulong.MaxValue)
                {
                    m_callback.DisconnectClient(clientIdToDisconnect);
                }
            }
            else
            {
                foreach (var client in m_clientStates)
                {
                    if (!client.Value.ProcessedPacket)
                        client.Value.State.Update();
                    client.Value.ProcessedPacket = false;
                }
            }
        }

        private readonly MyTimeSpan MAXIMUM_PACKET_GAP = MyTimeSpan.FromSeconds(0.4f);

        private void UpdateIncoming(ClientData clientData)
        {
            if (clientData.IncomingBuffer.Count == 0 ||
                clientData.IncomingBuffering && clientData.IncomingBuffer.Count < MINIMUM_INCOMING_BUFFER)
            {
                if (MyCompilationSymbols.EnableNetworkServerIncomingPacketTracking)
                {
                    if (clientData.IncomingBuffer.Count == 0)
                        MyTrace.Send(TraceWindow.Multiplayer, "Incoming buffer empty");
                    else MyTrace.Send(TraceWindow.Multiplayer, "Buffering incoming packets: " + clientData.IncomingBuffer.Count + 
                                                                                       " of " + MINIMUM_INCOMING_BUFFER);
                }
                clientData.IncomingBuffering = true;
                clientData.LastProcessedClientPacketId = 255;
                clientData.State.Update();
                return;
            }

            if (clientData.IncomingBuffering)
                clientData.LastProcessedClientPacketId = (byte)(clientData.IncomingBuffer[0].Id - 1);

            clientData.IncomingBuffering = false;

            // process out of order, but do not apply controls
            bool skipped;
            string buf = "";
            do
            {
                bool skipControls = clientData.IncomingBuffer.Count > MINIMUM_INCOMING_BUFFER;
                skipped = ProcessIncomingPacket(clientData, clientData.IncomingBuffer[0].Packet, skipControls);
                
                if (MyCompilationSymbols.EnableNetworkServerIncomingPacketTracking)
                {
                    buf = clientData.IncomingBuffer[0].Id + ", " + buf;
                    if (skipped)
                        buf = "-" + buf;
                }

                clientData.IncomingBuffer.RemoveAt(0);
            } while (clientData.IncomingBuffer.Count > MINIMUM_INCOMING_BUFFER || skipped && clientData.IncomingBuffer.Count > 0);

            if (MyCompilationSymbols.EnableNetworkServerIncomingPacketTracking)
                VRage.Trace.MyTrace.Send(VRage.Trace.TraceWindow.Multiplayer, buf + "; left: " + clientData.IncomingBuffer.Count);
        }

        private bool ProcessIncomingPacket(ClientData clientData, MyPacket packet, bool skipControls)
        {
            ReceiveStream.ResetRead(packet);

            // Read last state sync packet id
            clientData.LastClientPacketId = ReceiveStream.ReadByte();
            clientData.LastClientRealtime = MyTimeSpan.FromMilliseconds(ReceiveStream.ReadDouble());
            clientData.ClientStats.Update(clientData.ClientTracker.Add(clientData.LastClientPacketId));

            var newId = clientData.LastClientPacketId;
            // protection for out of ordere packets arriving later (again, potential overflow check)
            bool outOfOrder = !(newId > clientData.LastProcessedClientPacketId || (clientData.LastProcessedClientPacketId > 192 && newId < 64));
            if (!outOfOrder)
                clientData.LastProcessedClientPacketId = newId;

            // Read client state
            skipControls |= outOfOrder;
            clientData.State.Serialize(ReceiveStream, skipControls);
            ReceiveStream.CheckTerminator();
            return skipControls;
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

        public override void UpdateAfter()
        {
        }

        public override void UpdateClientStateGroups()
        {
        }

        // Child replicable is replicable with dependency which does not have separate priority (will need another flag on replicable)
        // When replicable is added for client, all children are added, same for removal, child priority is never checked
        // Dependency can change during child lifetime (e.g. moving blocks between grids), replication server will check dependency once per 300 updates (5s) and update internal structures appropriatelly.
        public override void SendUpdate()
        {
            m_serverTimeStamp = m_callback.GetUpdateTime();
            m_serverFrame++;

            if (m_clientStates.Count == 0)
                return;

            Stats_ObjectsRefreshed = 0;
            Stats_ObjectsSent = 0;

            m_priorityUpdates.ApplyChanges();

            if (m_priorityUpdates.Count > 0)
            {
                m_tmpHash.Clear();

                ProfilerShort.Begin("RefreshReplicables - m_priorityUpdates");
                foreach (var replicable in m_priorityUpdates)
                {
                    if (!replicable.HasToBeChild)
                        RefreshReplicable(replicable);
                    m_priorityUpdates.Remove(replicable);

                    m_tmpHash.Add(replicable);
                }
                ProfilerShort.End();

                ProfilerShort.Begin("SendStateSync - m_priorityUpdates");
                foreach (var client in m_clientStates)
                {
                    if (client.Value.IsReady)
                    {
                        SendStateSync(client.Value, m_tmpHash);
                    }
                }
                ProfilerShort.End();

                m_priorityUpdates.ApplyRemovals();
                return;
            }
            
            

#if AABB_REPLICABLES
            ProfilerShort.Begin("RefreshReplicables");
            foreach (var client in m_clientStates)
            {
                if (client.Value.IsReady)
                {
                    m_layerUpdateHash.Clear();
                    m_toDeleteHash.Clear();
                    
                    int layerIndex = client.Value.UpdateLayers.Length;
                    
                    foreach (var layer in client.Value.UpdateLayers)
                    {
                        --layerIndex;

                        BoundingBoxD aabb = new BoundingBoxD(client.Value.State.Position - new Vector3D(layer.Descriptor.Radius), client.Value.State.Position + new Vector3D(layer.Descriptor.Radius));
                        m_replicables.GetReplicablesInBox(aabb, layer.Updater.List);

                        if (layerIndex == 0)
                        {
                            foreach (var rep in layer.Replicables)
                            {
                                if (!m_layerUpdateHash.Contains(rep))
                                    m_toDeleteHash.Add(rep);
                            }
                        }

                        //Put only replicables into layer which are not in any previous layer
                        layer.Replicables.Clear();
                        foreach (var rep in layer.Updater.List)
                        {
                            AddReplicableToLayer(rep, layer);
                        }

                        layer.Updater.List.Clear();
                        layer.Updater.List.AddRange(layer.Replicables);

                        layer.Sender.List.Clear();
                        layer.Sender.List.AddRange(layer.Replicables);

                        //Move internal cursor
                        layer.Updater.Update();

                        //Iterate replicables which fits into current update interval
                        layer.Updater.Iterate(replicable =>
                        {
                            m_replicables.RefreshChildrenHierarchy(replicable);
                            RefreshReplicable(replicable, client.Key, client.Value);
                        });
                    }

                    foreach (var replicableToDelete in m_toDeleteHash)
                    {
                        if (client.Value.HasReplicable(replicableToDelete))
                            RemoveForClient(replicableToDelete, client.Key, client.Value, true);
                    }

                    m_toDeleteHash.Clear();
                }               
            }
            ProfilerShort.End();


            ProfilerShort.Begin("SendStateSync");
            foreach (var client in m_clientStates)
            {
                if (client.Value.IsReady)
                {
                    foreach (var layer in client.Value.UpdateLayers)
                    {
                        layer.Sender.Update();

                        m_tmpHash.Clear();
                        layer.Sender.Iterate(x => 
                            {
                                using (m_tmp)
                                {
                                    m_tmpHash.Add(x);
                                }
                            });
                        
                        if (m_tmpHash.Count > 0)
                            SendStateSync(client.Value, m_tmpHash);                        
                    }
                }
            }
            ProfilerShort.End();
#else
            ProfilerShort.Begin("RefreshReplicables");

            m_replicables.IterateRange(replicable =>
            {
                m_replicables.RefreshChildrenHierarchy(replicable);
                RefreshReplicable(replicable);
            });

            ProfilerShort.End();


            ProfilerShort.Begin("SendStateSync");
            foreach (var client in m_clientStates)
            {
                if (client.Value.IsReady)
                {
                    SendStateSync(client.Value, x => true);
                }
            }
            ProfilerShort.End();
#endif

            foreach (var client in m_clientStates)
            {
                if (m_serverTimeStamp > client.Value.LastStateSyncTimeStamp + MAXIMUM_PACKET_GAP)
                    SendEmptyStateSync(client.Value);
            }

            if (StressSleep.X > 0)
            {
                int sleep;
                if (StressSleep.Z == 0)
                    sleep = MyRandom.Instance.Next(StressSleep.X, StressSleep.Y);
                else
                    sleep = (int) (Math.Sin(m_serverTimeStamp.Milliseconds * Math.PI / StressSleep.Z) * StressSleep.Y + StressSleep.X);
                System.Threading.Thread.Sleep(sleep);
            }
        }

        private void AddReplicableToLayer(IMyReplicable rep, UpdateLayer layer)
        {
            if (!m_layerUpdateHash.Contains(rep))
            {
                AddReplicableToLayerSingle(rep, layer);

                if (rep.GetDependencies() != null)
                {
                    foreach (var replicableDependency in rep.GetDependencies())
                    {
                        AddReplicableToLayerSingle(replicableDependency, layer);
                    }
                }
            }
        }

        private void AddReplicableToLayerSingle(IMyReplicable rep, UpdateLayer layer)
        {
            layer.Replicables.Add(rep);
            m_layerUpdateHash.Add(rep);
            m_toDeleteHash.Remove(rep);
        }

        private void RefreshReplicable(IMyReplicable replicable)
        {
            foreach (var client in m_clientStates)
            {
                RefreshReplicable(replicable, client.Key, client.Value);
            }
        }

        /// <summary>
        /// Refreshes replicable priorities per client
        /// </summary>
        /// <param name="replicable"></param>
        private void RefreshReplicable(IMyReplicable replicable, EndpointId endPoint, ClientData clientData)
        {
            ProfilerShort.Begin("m_timeFunc");
            MyTimeSpan now = m_callback.GetUpdateTime();
            ProfilerShort.End();

            if (clientData.IsReady == false)
            {
                return;
            }

            ProfilerShort.Begin("RefreshReplicablePerClient");

            ProfilerShort.Begin("TryGetValue Replicables");
            MyReplicableClientData replicableInfo;
            bool hasObj = clientData.Replicables.TryGetValue(replicable, out replicableInfo);
            ProfilerShort.End();

            ProfilerShort.Begin("GetPriority");
            ProfilerShort.Begin(replicable.GetType().Name);
            float priority = replicable.GetPriority(new MyClientInfo(clientData), false);

            if (hasObj)
            {
                replicableInfo.Priority = priority;
            }

            bool hasPriority = priority > 0;
            ProfilerShort.End();
            ProfilerShort.End();

            if (hasPriority && !hasObj)
            {
                ProfilerShort.Begin("CheckReady");
                var parent = replicable.GetParent();
                hasPriority = parent == null || clientData.IsReplicableReady(parent);
                ProfilerShort.End();

                if (hasPriority)
                {
                    ProfilerShort.Begin("AddForClient");
                    AddForClient(replicable, endPoint, clientData, priority, false);
                    ProfilerShort.End();
                }
            }
            else if (hasObj)
            {
                ProfilerShort.Begin("UpdateSleepAndRemove");
                // Hysteresis
                replicableInfo.UpdateSleep(hasPriority, now);
                if (replicableInfo.ShouldRemove(now, MaxSleepTime))
                    RemoveForClient(replicable, endPoint, clientData, true);
                ProfilerShort.End();
            }

            if (hasPriority && hasObj)
            {
                using (m_tmp)
                {
                    m_replicables.GetAllChildren(replicable, m_tmp);

                    foreach (var child in m_tmp)
                    {
                        if (!clientData.HasReplicable(child))
                        {
                            AddForClient(child, endPoint, clientData, priority, false);
                        }
                    }
                }
            }

            Stats_ObjectsRefreshed++;

            ProfilerShort.End();
        }

        private void SendEmptyStateSync(ClientData clientData)
        {
            WritePacketHeader(clientData, false);
            m_callback.SendStateSync(SendStream, clientData.State.EndpointId, false);
        }

        public void AddToDirtyGroups(IMyStateGroup group)
        {
            foreach (var client in m_clientStates)
            {
                if (client.Value.StateGroups.ContainsKey(group))
                    client.Value.DirtyGroups.Add(group);
            }
        }


        private void SendStateSync(ClientData clientData, HashSet<IMyReplicable> replicables)
        {
            if (clientData.StateGroups.Count == 0)
                return;

            if (clientData.DirtyGroups.Count == 0)
                return;

            EndpointId endpointId = clientData.State.EndpointId;

            // TODO: Limit events
            clientData.EventQueue.Send();

            using (m_tmpStreamingEntries)
            {
                using (m_tmpSortEntries)
                {
                    ProfilerShort.Begin("UpdateGroupPriority");

                    foreach (var group in clientData.DirtyGroups)
                    {
                        if (!replicables.Contains(group.Owner.GetParent() ?? group.Owner) || !clientData.Replicables.ContainsKey(group.Owner))
                            continue;

                        if (!clientData.Replicables[group.Owner].HasActiveStateSync && group.GroupType != StateGroupEnum.Streaming)
                            continue;

                        var entry = clientData.StateGroups[group];
                                  
                        ProfilerShort.Begin("GetGroupPriority");

                        entry.Priority = entry.Group.GetGroupPriority((int)(m_serverFrame - entry.LastSyncedFrame), new MyClientInfo(clientData));

                        if (entry.Priority > 0)
                        {
                            if (entry.Group.GroupType == StateGroupEnum.Streaming)
                            {
                                m_tmpStreamingEntries.Add(entry);
                                Stats_ObjectsSent++;
                            }
                            else
                            {
                                m_tmpSortEntries.Add(entry);
                                Stats_ObjectsSent++;
                            }
                        }

                        if (!group.IsStillDirty(endpointId))
                            clientData.DirtyGroupsToRemove.Add(group);

                        ProfilerShort.End(Stats_ObjectsSent);
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

                    int numSent = 0;
                    while (SendStateSync(clientData, ref endpointId) && numSent <= MAX_NUM_STATE_SYNC_PACKETS_PER_CLIENT)
                    {
                        foreach (var entry in m_tmpSentEntries)
                        {
                            m_tmpSortEntries.Remove(entry);
                        }
                        numSent++;
                        m_tmpSentEntries.Clear();
                    }
                    m_tmpSentEntries.Clear();
                    if (m_tmpStreamingEntries.Count > 0)
                    {
                        int  messageBitSize = m_callback.GetMTRSize(endpointId) * 8;
                        ProfilerShort.Begin("Sort streaming entities");
                        m_tmpStreamingEntries.Sort(MyStateDataEntryComparer.Instance);
                        ProfilerShort.End();

                        var serverTimeStamp = WritePacketHeader(clientData, true);

                        var entry = m_tmpStreamingEntries.FirstOrDefault();

                        var oldWriteOffset = SendStream.BitPosition;

                        SendStream.WriteNetworkId(entry.GroupId);

                        ProfilerShort.Begin("serialize streaming entities");
                        entry.Group.Serialize(SendStream, clientData.State.EndpointId, serverTimeStamp, clientData.StateSyncPacketId, messageBitSize);
                        ProfilerShort.End();

                        int bitsWritten = SendStream.BitPosition - oldWriteOffset;

                        if (m_limits.Add(entry.Group.GroupType, bitsWritten))
                        {
                            clientData.PendingStateSyncAcks[clientData.StateSyncPacketId].Add(entry.Group);

                            entry.LastSyncedFrame = m_serverFrame;
                        }
                        else
                        {
                            // When serialize returns false, restore previous bit position and tell group it was not delivered
                            entry.Group.OnAck(clientData.State, clientData.StateSyncPacketId, false);
                        }
                        m_callback.SendStateSync(SendStream, endpointId, true);

                        IMyReplicable parentReplicable = entry.Group.Owner;
                        if (parentReplicable != null)
                        {
                            using (m_tmp)
                            {
                                m_replicables.GetAllChildren(parentReplicable, m_tmp);

                                foreach (var child in m_tmp)
                                {
                                    if (!clientData.HasReplicable(child))
                                    {
                                        AddForClient(child, endpointId, clientData, parentReplicable.GetPriority(new MyClientInfo(clientData), true), false);
                                    }
                                }
                            }
                        }
                    }
                    //Server.SendMessage(m_sendStream, guid, PacketReliabilityEnum.UNRELIABLE, PacketPriorityEnum.MEDIUM_PRIORITY, MyChannelEnum.StateDataSync);
                }
            }

            foreach (var group in clientData.DirtyGroupsToRemove)
            {
                clientData.DirtyGroups.Remove(group);
            }
            clientData.DirtyGroupsToRemove.Clear();
        }

        private MyTimeSpan WritePacketHeader(ClientData clientData, bool streaming)
        {
            clientData.LastStateSyncTimeStamp = m_serverTimeStamp;
            clientData.StateSyncPacketId++;

            if (clientData.StartingServerTimeStamp == MyTimeSpan.Zero)
                clientData.StartingServerTimeStamp = m_serverTimeStamp;

            MyTimeSpan serverTimeStamp = m_serverTimeStamp - clientData.StartingServerTimeStamp;
            SendStream.ResetWrite();
            SendStream.WriteBool(streaming);
            SendStream.WriteByte(clientData.StateSyncPacketId);
            clientData.ClientStats.Write(SendStream);
            clientData.ClientStats.Reset();
            SendStream.WriteDouble(serverTimeStamp.Milliseconds);
            SendStream.WriteDouble(clientData.LastClientRealtime.Milliseconds);
            clientData.LastClientRealtime = MyTimeSpan.FromMilliseconds(-1);

            m_callback.SendCustomState(SendStream, clientData.State.EndpointId);

            return serverTimeStamp;
        }

        private bool SendStateSync(ClientData clientData, ref EndpointId endpointId)
        {
            var serverTimeStamp = WritePacketHeader(clientData, false);

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
                //if (clientData.State.GetControlledEntity() != entry.Group.Entity)
                //    continue;
                ProfilerShort.Begin("serializing entry counter");
                var oldWriteOffset = SendStream.BitPosition;
                SendStream.WriteNetworkId(entry.GroupId);

                entry.Group.Serialize(SendStream, clientData.State.EndpointId, serverTimeStamp, clientData.StateSyncPacketId, messageBitSize);

                int bitsWritten = SendStream.BitPosition - oldWriteOffset;
                if (bitsWritten > 0 && SendStream.BitPosition <= messageBitSize && m_limits.Add(entry.Group.GroupType, bitsWritten))
                {
                    clientData.PendingStateSyncAcks[clientData.StateSyncPacketId].Add(entry.Group);
                    sent++;
                    entry.LastSyncedFrame = m_serverFrame;
                    m_tmpSentEntries.Add(entry);
                }
                else
                {
                    numOverLoad++;
                    // When serialize returns false, restore previous bit position and tell group it was not delivered
                    entry.Group.OnAck(clientData.State, clientData.StateSyncPacketId, false);
                    SendStream.SetBitPositionWrite(oldWriteOffset);
                }
                ProfilerShort.End();
                if (sent >= maxToSend || SendStream.BitPosition >= messageBitSize || numOverLoad > 10)
                {
                   
                    break;
                }
            }

            ProfilerShort.End();
            m_callback.SendStateSync(SendStream, endpointId, false);
            return numOverLoad >0;
        }

        private void AddForClient(IMyReplicable replicable, EndpointId clientEndpoint, ClientData clientData, float priority,bool force)
        {
            if (!replicable.IsReadyForReplication) 
                return;
            if (clientData.HasReplicable(replicable))
                return; // already added

            ProfilerShort.Begin("AddClientReplicable");
            AddClientReplicable(replicable, clientData, priority, force);
            ProfilerShort.End();

            ProfilerShort.Begin("SendReplicationCreate");
            SendReplicationCreate(replicable, clientData, clientEndpoint);
            ProfilerShort.End();
        }

        private void RemoveForClient(IMyReplicable replicable, EndpointId clientEndpoint, ClientData clientData, bool sendDestroyToClient)
        {
            using (m_tmp)
            {
                m_replicables.RefreshChildrenHierarchy(replicable);

                m_replicables.GetAllChildren(replicable, m_tmp);

                m_tmp.Add(replicable);

                foreach (var replicableItem in m_tmp)
                {
                    // It should not be in this list in normal situation, but there are many overrides that
                    // can remove replicable before it finished streaming. Just remove it now than.
                    clientData.BlockedReplicables.Remove(replicableItem);

                    if (sendDestroyToClient)
                    {
                        ProfilerShort.Begin("SendReplicationDestroy");
                        SendReplicationDestroy(replicableItem, clientData, clientEndpoint);
                        ProfilerShort.End();
                    }

                    ProfilerShort.Begin("RemoveClientReplicable");
                    RemoveClientReplicable(replicableItem, clientData);
                    ProfilerShort.End();
                }

                foreach (var layer in clientData.UpdateLayers)
                {
                    layer.Replicables.Remove(replicable);
                }
            }
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

            SendStream.ResetWrite();
            SendStream.WriteTypeId(typeId);
            SendStream.WriteNetworkId(networkId);


            var streamable = obj as IMyStreamableReplicable;

            bool sendStreamed = streamable != null && streamable.NeedsToBeStreamed;
            if (streamable != null && streamable.NeedsToBeStreamed == false)
            {
                SendStream.WriteByte((byte)(groups.Count - 1));
            }
            else
            {
                SendStream.WriteByte((byte)groups.Count);
            }


            for (int i = 0; i < groups.Count; i++)
            {
                if (sendStreamed == false && groups[i].GroupType == StateGroupEnum.Streaming)
                {
                    continue;
                }
                SendStream.WriteNetworkId(GetNetworkIdByObject(groups[i]));
            }

            ProfilerShort.End();

            ProfilerShort.Begin("SaveReplicable");

            if (sendStreamed)
            {
                clientData.Replicables[obj].IsStreaming = true;
                m_callback.SendReplicationCreateStreamed(SendStream, clientEndpoint);
            }
            else
            {
                obj.OnSave(SendStream);
                ProfilerShort.Begin("Send");
                m_callback.SendReplicationCreate(SendStream, clientEndpoint);
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

            SendStream.ResetWrite();
            SendStream.WriteNetworkId(GetNetworkIdByObject(obj));
            m_callback.SendReplicationDestroy(SendStream, clientEndpoint);
            //Server.SendMessage(m_sendStream, clientId, PacketReliabilityEnum.RELIABLE, PacketPriorityEnum.LOW_PRIORITY, MyChannelEnum.Replication);
        }

        public void ReplicableReady(MyPacket packet)
        {
            ReceiveStream.ResetRead(packet);
            var networkId = ReceiveStream.ReadNetworkId();
            bool loaded = ReceiveStream.ReadBool();
            ReceiveStream.CheckTerminator();

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
                    if (!replicable.HasToBeChild)
                        m_priorityUpdates.Add(replicable);  //character after respawn, to switch controller
                    using (m_tmp)
                    {
                        foreach (var forcedReplicable in clientData.ForcedReplicables)
                        {
                            if (forcedReplicable.Value == replicable)
                            {
                                m_tmp.Add(forcedReplicable.Key);
                                if (!clientData.Replicables.ContainsKey(forcedReplicable.Key) && m_replicableGroups.ContainsKey(forcedReplicable.Key))
                                {
                                    AddForClient(forcedReplicable.Key, packet.Sender, clientData, 0, true);
                                }
                            }
                        }
                        foreach (var replicableToRemove in m_tmp)
                        {
                            clientData.ForcedReplicables.Remove(replicableToRemove);
                        }
                    }
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
        
        void AddStateGroups(IMyReplicable replicable)
        {
            using (tmpGroupsLock.AcquireExclusiveUsing())
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

        private void AddClientReplicable(IMyReplicable replicable, ClientData clientData, float priority, bool force)
        {
            // Add replicable
            clientData.Replicables.Add(replicable, new MyReplicableClientData() { Priority = priority });

            // Add state groups
            foreach (var group in m_replicableGroups[replicable])
            {
                var netId = GetNetworkIdByObject(group);

                if (group.GroupType == StateGroupEnum.Streaming)
                {
                    if((replicable as IMyStreamableReplicable).NeedsToBeStreamed == false)
                    {
                        continue;
                    }
                }

                clientData.StateGroups.Add(group, new MyStateDataEntry(netId, group));
                clientData.DirtyGroups.Add(group);

                group.CreateClientData(clientData.State);

                if (force)
                {
                    group.ForceSend(clientData.State);
                }
            }
        }

        private void RemoveClientReplicable(IMyReplicable replicable, ClientData clientData)
        {
            if (!m_replicableGroups.ContainsKey(replicable))
                return;

            foreach (var g in m_replicableGroups[replicable])
            {
                g.DestroyClientData(clientData.State);
                clientData.StateGroups.Remove(g);
                clientData.DirtyGroups.Remove(g);
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
                replicableInfo = client.Replicables[((IMyStateGroup)eventInstance).Owner];
                priority = 1;
                return isReliable || replicableInfo.HasActiveStateSync || replicableInfo.IsStreaming;
            }
            else if (eventInstance is IMyReplicable && (client.Replicables.TryGetValue((IMyReplicable)eventInstance, out replicableInfo) || FixedObjects.Contains(eventInstance)))
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
            stream.CheckTerminator();

            // Send event in case it has [Broadcast], [BroadcastExcept] or [Client] attribute
            if (site.HasClientFlag || site.HasBroadcastFlag || site.HasBroadcastExceptFlag)
            {
                DispatchEvent(stream, site, source, sendAs, 1.0f);
            }
        }

        public void SendJoinResult(ref JoinResultMsg msg, ulong sendTo)
        {
            SendStream.ResetWrite();
            SendStream.WriteUInt16((ushort)msg.JoinResult);
            SendStream.WriteUInt64(msg.Admin);

            m_callback.SendJoinResult(SendStream,new EndpointId(sendTo));
        }

        public void SendWorldData(ref ServerDataMsg msg)
        {
            foreach (var client in m_clientStates)
            {
                SendStream.ResetWrite();

                VRage.Serialization.MySerializer.Write(SendStream, ref msg);
                m_callback.SendWorldData(SendStream, client.Key);
            }
        }

        public ConnectedClientDataMsg OnClientConnected(MyPacket packet)
        {
            ReceiveStream.ResetRead(packet);
            ConnectedClientDataMsg msg = VRage.Serialization.MySerializer.CreateAndRead<ConnectedClientDataMsg>(ReceiveStream);
            return msg;
        }

        public void SendClientConnected(ref ConnectedClientDataMsg msg,ulong sendTo)
        {
            SendStream.ResetWrite();
            VRage.Serialization.MySerializer.Write<ConnectedClientDataMsg>(SendStream, ref msg);
            m_callback.SentClientJoined(SendStream,new EndpointId(sendTo));
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
