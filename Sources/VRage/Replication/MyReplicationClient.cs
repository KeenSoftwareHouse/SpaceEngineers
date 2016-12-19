using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using VRage.Library.Collections;
using VRage.Library.Utils;
using VRage.Profiler;
using VRage.Replication;
using VRage.Utils;

namespace VRage.Network
{
    public class MyReplicationClient : MyReplicationLayer
    {
        public static SerializableVector3I StressSleep = new SerializableVector3I(0,0,0);

        private MyTimeSpan Timestamp;

        private readonly MyClientStateBase ClientState;
        private bool m_clientReady;
        private bool m_hasTypeTable;
        private readonly IReplicationClientCallback m_callback;
        private readonly CacheList<IMyStateGroup> m_tmpGroups = new CacheList<IMyStateGroup>(4);
        private readonly List<byte> m_acks = new List<byte>();
        private byte m_lastStateSyncPacketId;
        private byte m_clientPacketId;
        private MyTimeSpan m_lastServerTimeStamp = MyTimeSpan.Zero;

        // TODO: Maybe pool pending replicables?
        private readonly Dictionary<NetworkId, MyPendingReplicable> m_pendingReplicables = new Dictionary<NetworkId, MyPendingReplicable>(16);

        private readonly MyEventsBuffer m_eventBuffer = new MyEventsBuffer();
        private readonly MyEventsBuffer.Handler m_eventHandler;
        private readonly MyEventsBuffer.IsBlockedHandler m_isBlockedHandler;

        private MyTimeSpan m_clientStartTimeStamp = MyTimeSpan.Zero;

        private readonly float m_simulationTimeStep;

        public MyReplicationClient(IReplicationClientCallback callback, MyClientStateBase clientState,
            float simulationTimeStep)
            : base(false)
        {
            m_simulationTimeStep = simulationTimeStep;
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
        [System.Runtime.ExceptionServices.HandleProcessCorruptedStateExceptions]
        [System.Security.SecurityCriticalAttribute]
        void SetReplicableReady(NetworkId networkId, IMyReplicable replicable, bool loaded)
        {
            try
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
                            Debug.Assert(ids.Count == m_tmpGroups.Count,
                                "Number of state groups on client and server for replicable does not match");
                            for (int i = 0; i < m_tmpGroups.Count; i++)
                            {
                                if (m_tmpGroups[i] != replicable && m_tmpGroups[i].GroupType != StateGroupEnum.Streaming)
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

                    SendStream.ResetWrite();
                    SendStream.WriteNetworkId(networkId);
                    SendStream.WriteBool(loaded);
                    SendStream.Terminate();
                    m_callback.SendReplicableReady(SendStream);
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
                            MyLog.Default.WriteLine("removing streaming group for not loaded replicable !");
                        }

                        replicable.GetStateGroups(m_tmpGroups);
                        foreach (var g in m_tmpGroups)
                        {
                            if (g != null)
                                // when terminal repblicable fails to attach to block its state group is null becouase its created inside hook method.
                            {
                                g.Destroy();
                            }
                        }
                    }
                    replicable.OnDestroy();
                }
            }
            catch (Exception ex)
            {
                MyLog.Default.WriteLine(ex);
                throw;
                //m_callback.DisconnectFromHost();
            }
        }

        public void ProcessReplicationCreateBegin(MyPacket packet)
        {
            ReceiveStream.ResetRead(packet);

            TypeId typeId = ReceiveStream.ReadTypeId();
            NetworkId networkID = ReceiveStream.ReadNetworkId();
            byte groupCount = ReceiveStream.ReadByte();

            var pendingReplicable = new MyPendingReplicable();
            for (int i = 0; i < groupCount; i++)
            {
                var id = ReceiveStream.ReadNetworkId();
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

            replicable.OnLoadBegin(ReceiveStream, (loaded) => SetReplicableReady(networkID, replicable, loaded));
        }

        public void ProcessReplicationCreate(MyPacket packet)
        {
            ReceiveStream.ResetRead(packet);

            TypeId typeId = ReceiveStream.ReadTypeId();
            NetworkId networkID = ReceiveStream.ReadNetworkId();
            byte groupCount = ReceiveStream.ReadByte();

            var pendingReplicable = new MyPendingReplicable();
            for (int i = 0; i < groupCount; i++)
            {
                var id = ReceiveStream.ReadNetworkId();
                pendingReplicable.StateGroupIds.Add(id);
            }

            Type type = GetTypeByTypeId(typeId);
            IMyReplicable replicable = (IMyReplicable)Activator.CreateInstance(type);
            pendingReplicable.DebugObject = replicable;
            pendingReplicable.IsStreaming = false;

            m_pendingReplicables.Add(networkID, pendingReplicable);

            replicable.OnLoad(ReceiveStream, (loaded) => SetReplicableReady(networkID, replicable, loaded));
        }

        public void ProcessReplicationDestroy(MyPacket packet)
        {
            ReceiveStream.ResetRead(packet);
            NetworkId networkID = ReceiveStream.ReadNetworkId();

            MyPendingReplicable pendingReplicable;

            if (!m_pendingReplicables.TryGetValue(networkID, out pendingReplicable)) // When it wasn't in pending replicables, it's already active and in scene, destroy it
            {
                IMyReplicable replicable = GetObjectByNetworkId(networkID) as IMyReplicable;
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
            ReceiveStream.ResetRead(packet);
            SerializeTypeTable(ReceiveStream);
            m_hasTypeTable = true;
        }

        const int MAX_TIMESTAMP_DIFF_LOW = 80;
        const int MAX_TIMESTAMP_DIFF_HIGH = 5000;
        private MyTimeSpan m_lastTime;

        private MyTimeSpan m_ping;
        private MyTimeSpan m_smoothPing;
        private MyTimeSpan m_lastPingTime;
        private MyTimeSpan m_correctionSmooth;
        public MyTimeSpan Ping { get { return UseSmoothPing ? m_smoothPing : m_ping; } }
        public static bool ApplyCorrectionsDebug = true;

        public override void UpdateBefore()
        {
        }

        public override void UpdateAfter()
        {
            if (!m_clientReady || !m_hasTypeTable || ClientState == null)
                return;

            NetProfiler.Begin("Replication client update", 0);

            var simulationTime = m_callback.GetUpdateTime();

            if (m_clientStartTimeStamp == MyTimeSpan.Zero)
                m_clientStartTimeStamp = simulationTime; // (uint)DateTime.Now.TimeOfDay.TotalMilliseconds;

           
            var timeStamp = simulationTime - m_clientStartTimeStamp;

            UpdatePingSmoothing();
            NetProfiler.CustomTime("Ping", (float)m_ping.Milliseconds, "{0} ms");
            NetProfiler.CustomTime("SmoothPing", (float)m_smoothPing.Milliseconds, "{0} ms");

            NetProfiler.Begin("Packet stats");
            NetProfiler.CustomTime("Server Drops", (float)m_serverStats.Drops, "{0}");
            NetProfiler.CustomTime("Server OutOfOrder", (float)m_serverStats.OutOfOrder, "{0}");
            NetProfiler.CustomTime("Server Duplicates", (float)m_serverStats.Duplicates, "{0}");
            NetProfiler.CustomTime("Client Drops", (float)m_clientStatsFromServer.Drops, "{0}");
            NetProfiler.CustomTime("Client OutOfOrder", (float)m_clientStatsFromServer.OutOfOrder, "{0}");
            NetProfiler.CustomTime("Client Duplicates", (float)m_clientStatsFromServer.Duplicates, "{0}");
            m_serverStats.Reset();
            m_clientStatsFromServer.Reset();
            NetProfiler.End();

            MyTimeSpan ping = UseSmoothPing ? m_smoothPing : m_ping;
            var diffCorrection = -ping.Milliseconds * m_callback.GetServerSimulationRatio();
            var diffMS = timeStamp.Milliseconds - m_lastServerTimeStamp.Milliseconds;
            var correctionCurrent = diffMS + diffCorrection;
            int correction = 0, nextFrameDelta = 0;
            MyTimeSpan currentTime = MyTimeSpan.FromTicks(Stopwatch.GetTimestamp());
            MyTimeSpan realtimeDelta = currentTime - m_lastTime;

            // try to be always one simulation step ahead
            correctionCurrent -= m_simulationTimeStep;
            //if (Math.Abs(correctionCurrent) > 200)
                //  m_correctionSmooth = MyTimeSpan.FromMilliseconds(correctionCurrent);
            correctionCurrent = Math.Min(correctionCurrent, (int)(m_simulationTimeStep * 2 / m_callback.GetServerSimulationRatio()));

            if (diffMS < -MAX_TIMESTAMP_DIFF_LOW || diffMS > MAX_TIMESTAMP_DIFF_HIGH)
            {
                m_clientStartTimeStamp = MyTimeSpan.FromMilliseconds(m_clientStartTimeStamp.Milliseconds + diffMS);

                timeStamp = simulationTime - m_clientStartTimeStamp;
                m_correctionSmooth = MyTimeSpan.Zero;
                TimestampReset();
                if (VRage.MyCompilationSymbols.EnableNetworkPositionTracking)
                {
                    Trace.MyTrace.Send(Trace.TraceWindow.MTiming, "---------------------------------------------------------------- DESYNC");
                }
            }
            else
            {
                var factor = Math.Min(realtimeDelta.Seconds / SmoothCorrectionAmplitude, 1.0);
                m_correctionSmooth = MyTimeSpan.FromMilliseconds(correctionCurrent * factor + m_correctionSmooth.Milliseconds * (1 - factor));
                // special case: we really dont want the client timestamp to fall behind
                if (diffMS < 0) 
                    correction = (int)correctionCurrent;
                else correction = UseSmoothCorrection ? (int) m_correctionSmooth.Milliseconds : (int) correctionCurrent;

                NetProfiler.CustomTime("Correction", (float)correctionCurrent, "{0} ms");
                NetProfiler.CustomTime("SmoothCorrection", (float)m_correctionSmooth.Milliseconds, "{0} ms");

                if (ApplyCorrectionsDebug && (LastMessageFromServer - DateTime.UtcNow).Seconds < 1.0f)
                {
                    if (diffMS < 0)
                    {
                        nextFrameDelta = correction;
                        m_callback.SetNextFrameDelayDelta(nextFrameDelta);
                    }
                    else if (Math.Abs(correction) > TimestampCorrectionMinimum)
                    {
                        nextFrameDelta = (Math.Abs(correction) - TimestampCorrectionMinimum) * Math.Sign(correction);
                        m_callback.SetNextFrameDelayDelta(nextFrameDelta);
                    }
                }
            }
            NetProfiler.CustomTime("GameTimeDelta", (float)(timeStamp - Timestamp).Milliseconds, "{0} ms");
            NetProfiler.CustomTime("RealTimeDelta", (float)realtimeDelta.Milliseconds, "{0} ms");
            Timestamp = timeStamp;

            //if (VRage.MyCompilationSymbols.EnableNetworkPositionTracking)
            {
                string trace = "realtime delta: " + realtimeDelta +
                                ", client: " + Timestamp +
                                ", server: " + m_lastServerTimeStamp +
                                ", diff: " + diffMS.ToString("##.#") + " => " + (Timestamp.Milliseconds - m_lastServerTimeStamp.Milliseconds).ToString("##.#") +
                                ", Ping: " + m_ping.Milliseconds.ToString("##.#") + " / " + m_smoothPing.Milliseconds.ToString("##.#") +
                                "ms, Correction " + correctionCurrent + " / " + m_correctionSmooth.Milliseconds + " / " + nextFrameDelta +
                                ", ratio " + m_callback.GetServerSimulationRatio();
                Trace.MyTrace.Send(Trace.TraceWindow.MTiming, trace);
                //Trace.MyTrace.Send(Trace.TraceWindow.MPositions2, trace);
            }
            m_lastTime = currentTime;

            NetProfiler.End();

            if (StressSleep.X > 0)
            {
                int sleep;
                if (StressSleep.Z == 0)
                    sleep = MyRandom.Instance.Next(StressSleep.X, StressSleep.Y);
                else sleep = (int) (Math.Sin(simulationTime.Milliseconds * Math.PI / StressSleep.Z) * StressSleep.Y + StressSleep.X);
                System.Threading.Thread.Sleep(sleep);
            }
        }

        public override void UpdateClientStateGroups()
        {
            ProfilerShort.Begin("StateGroup.ClientUpdate");
            // Update state groups on client
            foreach (var obj in NetworkObjects)
            {
                var stateGroup = obj as IMyStateGroup;
                if (stateGroup != null)
                {
                    stateGroup.ClientUpdate(Timestamp);
                }
            }
            ProfilerShort.End();
        }

        private void TimestampReset()
        {
            foreach (var obj in NetworkObjects)
            {
                var stateGroup = obj as IMyStateGroup;
                if (stateGroup != null)
                {
                    stateGroup.TimestampReset(Timestamp);
                }
            }
        }

        public override void SendUpdate()
        {
            ProfilerShort.Begin("ClientState.WriteAcks");

            // Client ACK Packet - reliable
            SendStream.ResetWrite();

            // ACK Header
            // Write last state sync packet id
            SendStream.WriteByte(m_lastStateSyncPacketId);
            
            // Write ACKs
            byte num = (byte)m_acks.Count;
            SendStream.WriteByte(num);
            for (int i = 0; i < num; i++)
            {
                SendStream.WriteByte(m_acks[i]);
            }
            SendStream.Terminate();
            m_acks.Clear();
            ProfilerShort.End();
            m_callback.SendClientAcks(SendStream);

            // Client Update Packet
            SendStream.ResetWrite();

            m_clientPacketId++;
            SendStream.WriteByte(m_clientPacketId);
            SendStream.WriteDouble(MyTimeSpan.FromTicks(Stopwatch.GetTimestamp()).Milliseconds);
            ProfilerShort.Begin("ClientState.Serialize");
            if (VRage.MyCompilationSymbols.EnableNetworkPacketTracking)
                Trace.MyTrace.Send(Trace.TraceWindow.MPackets, "Send client update: ");
            // Write Client state
            ClientState.Serialize(SendStream, false);
            ProfilerShort.End();
            SendStream.Terminate();

            ProfilerShort.Begin("SendClientUpdate");
            m_callback.SendClientUpdate(SendStream);
            ProfilerShort.End();

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
            LastMessageFromServer = DateTime.UtcNow;
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
            LastMessageFromServer = DateTime.UtcNow;
            // Client blindly invokes everything received from server (without validation)
            Invoke(site, stream, obj, source, null, false);
        }

        private MyPacketStatistics m_clientStatsFromServer = new MyPacketStatistics();

        private readonly MyPacketTracker m_serverTracker = new MyPacketTracker();
        private MyPacketStatistics m_serverStats = new MyPacketStatistics();

        //List<byte> m_alreadyReceivedPackets = new List<byte>();


        /// <summary>
        /// Processes state sync sent by server.
        /// </summary>
        public void ProcessStateSync(MyPacket packet)
        {
            LastMessageFromServer = DateTime.UtcNow;
            // Simulated packet loss
            // if (MyRandom.Instance.NextFloat() > 0.3f) return;

            ReceiveStream.ResetRead(packet);
            bool isStreaming = ReceiveStream.ReadBool();
            
            var packetId = ReceiveStream.ReadByte();

            //if (m_alreadyReceivedPackets.Contains(packetId))
            //{
            //}
            //if (m_alreadyReceivedPackets.Count > 128)
            //    m_alreadyReceivedPackets.RemoveAt(0);
            //m_alreadyReceivedPackets.Add(packetId);

            var serverOrder = m_serverTracker.Add(packetId);
            m_serverStats.Update(serverOrder);
            if (serverOrder == MyPacketTracker.OrderType.Duplicate)
                return;
            m_lastStateSyncPacketId = packetId;

            MyPacketStatistics stats = new MyPacketStatistics();
            stats.Read(ReceiveStream);
            m_clientStatsFromServer.Add(stats);

            var serverTimeStamp = MyTimeSpan.FromMilliseconds(ReceiveStream.ReadDouble());
            if (m_lastServerTimeStamp < serverTimeStamp)
                m_lastServerTimeStamp = serverTimeStamp;
            
            double lastClientRealtime = ReceiveStream.ReadDouble();
            if (lastClientRealtime > 0)
            {
                var ping = packet.ReceivedTime - MyTimeSpan.FromMilliseconds(lastClientRealtime);
                if (ping.Milliseconds < 1000)
                    SetPing(ping);
            }

            MyTimeSpan relevantTimeStamp = serverTimeStamp;
            m_callback.ReadCustomState(ReceiveStream);

            while (ReceiveStream.BytePosition < ReceiveStream.ByteLength)
            {
                NetworkId networkID = ReceiveStream.ReadNetworkId();
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

                if (isStreaming && obj.GroupType != StateGroupEnum.Streaming)
                {
                    Debug.Fail("group type mismatch !");
                    MyLog.Default.WriteLine("received streaming flag but group is not streaming !");
                    return;
                }

                if (!isStreaming && obj.GroupType == StateGroupEnum.Streaming)
                {
                    Debug.Fail("group type mismatch !");
                    MyLog.Default.WriteLine("received non streaming flag but group wants to stream !");
                    return;
                }

                if (VRage.MyCompilationSymbols.EnableNetworkPacketTracking)
                    Trace.MyTrace.Send(Trace.TraceWindow.MPackets, " ObjSync: " + obj.GetType().Name);

                var pos = ReceiveStream.BytePosition;
                NetProfiler.Begin(obj.GetType().Name);
                obj.Serialize(ReceiveStream, ClientState.EndpointId, relevantTimeStamp, m_lastStateSyncPacketId, 0);
                NetProfiler.End(ReceiveStream.ByteLength - pos);

                if (!m_acks.Contains(m_lastStateSyncPacketId))
                {
                    m_acks.Add(m_lastStateSyncPacketId);
                }
            }            
        }

        private void SetPing(MyTimeSpan ping)
        {
            m_ping = ping;
            m_callback.SetPing((long) ping.Milliseconds);
            UpdatePingSmoothing();
        }
        private void UpdatePingSmoothing()
        {
            var currentTime = MyTimeSpan.FromTicks(Stopwatch.GetTimestamp());
            var deltaTime = currentTime - m_lastPingTime;
            var factor = Math.Min(deltaTime.Seconds / PingSmoothFactor, 1.0);
            m_smoothPing = MyTimeSpan.FromMilliseconds(m_ping.Milliseconds * factor + m_smoothPing.Milliseconds * (1 - factor));
            m_lastPingTime = currentTime;
        }

        public JoinResultMsg OnJoinResult(MyPacket packet)
        {
            ReceiveStream.ResetRead(packet);
            JoinResultMsg msg = VRage.Serialization.MySerializer.CreateAndRead<JoinResultMsg>(ReceiveStream);
            return msg;
        }

        public ServerDataMsg OnWorldData(MyPacket packet)
        {
            ReceiveStream.ResetRead(packet);
            ServerDataMsg msg = VRage.Serialization.MySerializer.CreateAndRead<ServerDataMsg>(ReceiveStream);
            return msg;
        }

        public ChatMsg OnChatMessage(MyPacket packet)
        {
            ReceiveStream.ResetRead(packet);
            ChatMsg msg = VRage.Serialization.MySerializer.CreateAndRead<ChatMsg>(ReceiveStream);
            return msg;
        }

        public ConnectedClientDataMsg OnClientConnected(MyPacket packet)
        {
            ReceiveStream.ResetRead(packet);
            ConnectedClientDataMsg msg = VRage.Serialization.MySerializer.CreateAndRead<ConnectedClientDataMsg>(ReceiveStream);
            return msg;
        }

        public void SendClientConnected(ref ConnectedClientDataMsg msg)
        {
            SendStream.ResetWrite();
            VRage.Serialization.MySerializer.Write<ConnectedClientDataMsg>(SendStream, ref msg);
            m_callback.SendConnectRequest(SendStream);
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

        public void ResetClientTimes()
        {
            m_clientStartTimeStamp = Timestamp;

            foreach (var obj in NetworkObjects)
            {
                var stateGroup = obj as IMyStateGroup;
                if (stateGroup != null)
                {
                    stateGroup.Destroy();
                }
            }        
        }

        #endregion

    }
}
