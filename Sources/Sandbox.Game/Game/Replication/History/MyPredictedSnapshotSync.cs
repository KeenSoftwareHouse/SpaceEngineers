using System;
using VRage;
using VRage.Game.Entity;
using VRage.Library.Collections;
using VRage.Library.Utils;
using VRage.Trace;
using VRageMath;

namespace Sandbox.Game.Replication.History
{
    public class MyPredictedSnapshotSyncSetup : MySnapshotSyncSetup
    {
        public float MaxPositionFactor;
        public float MaxLinearFactor;
        public float MaxRotationFactor;
        public float MaxAngularFactor;
        public float IterationsFactor;
    }

    public class MyPredictedSnapshotSync : IMySnapshotSync
    {
        // debug data
        public static bool SetTransformCorrections = true;
        public static bool SetPhysicsCorrections = true;

        public static float DeltaFactor = 0.7f;
        public static int SmoothTimesteps = 30;

        public static bool SmoothPositionCorrection = true;
        public static float MinPositionDelta = 0.05f;
        public static float MaxPositionDelta = 0.5f;
        public static float ReferenceLinearVelocity = 10.0f;

        public static bool SmoothRotationCorrection = true;
        public static float MinRotationAngle = 2.0f / 180.0f * MathHelper.Pi;
        public static float MaxRotationAngle = 10.0f / 180.0f * MathHelper.Pi;
        public static float ReferenceAngularVelocity = 0.5f;

        public static bool SmoothLinearVelocityCorrection = true;
        public static float MinLinearVelocityDelta = 0.01f;
        public static float MaxLinearVelocityDelta = 4.0f;

        public static bool SmoothAngularVelocityCorrection = true;
        public static float MinAngularVelocityDelta = 0.01f;

        public static float MinVelocityChangeToReset = 10;

        private readonly MySnapshotHistory m_clientHistory = new MySnapshotHistory();
        private readonly MySnapshotHistory m_receivedQueue = new MySnapshotHistory();

        private int m_animDeltaLinearVelocityIterations;
        private MyTimeSpan m_animDeltaLinearVelocityTimestamp;
        private Vector3 m_animDeltaLinearVelocity;
        private int m_animDeltaPositionIterations;
        private MyTimeSpan m_animDeltaPositionTimestamp;
        private Vector3D m_animDeltaPosition;
        private int m_animDeltaRotationIterations;
        private MyTimeSpan m_animDeltaRotationTimestamp;
        private Quaternion m_animDeltaRotation;
        private int m_animDeltaAngularVelocityIterations;
        private MyTimeSpan m_animDeltaAngularVelocityTimestamp;
        private Vector3 m_animDeltaAngularVelocity;

        private readonly MyEntity m_entity;

        private MySnapshot m_lastServerSnapshot;
        private MyTimeSpan m_lastServerTimestamp;
        private MySnapshot m_lastClientSnapshot;
        private MyTimeSpan m_lastClientTimestamp;

        private MySnapshot m_lastSnapshot;
        private MyTimeSpan m_lastTimestamp;

        private readonly string m_trackName = "cenda";
        private bool m_wasReset = true;
        private Vector3 m_lastServerVelocity;

        private int m_stopSuspected;

        public MyPredictedSnapshotSync(MyEntity entity)
        {
            m_entity = entity;
        }

        public void Update(MyTimeSpan clientTimestamp, MySnapshotSyncSetup setup)
        {
            // skip entities with parent
            if (m_entity.Parent != null)
                return;
            if (m_entity.Physics == null) //trees
                return;
            if (m_entity.Physics.RigidBody != null && !m_entity.Physics.RigidBody.IsActive)
                return;

            VRage.Profiler.ProfilerShort.Begin("Sync Predicted" + m_entity.DisplayName);
            UpdatePrediction(clientTimestamp, setup);
            VRage.Profiler.ProfilerShort.End();
        }

        public void UpdatePrediction(MyTimeSpan clientTimestamp, MySnapshotSyncSetup setup)
        {
            var currentSnapshot = new MySnapshot(m_entity);
            var tmpSnapshot = currentSnapshot;
            m_clientHistory.Add(currentSnapshot, clientTimestamp);

            var serverTmpSnapshot = m_receivedQueue.GetItem(clientTimestamp);
            var timeDelta = (float)(m_lastServerTimestamp - serverTmpSnapshot.Timestamp).Seconds;
            var predictedSetup = setup as MyPredictedSnapshotSyncSetup;
            var serverDeltaItem = UpdateFromServerQueue(clientTimestamp, predictedSetup);
            bool animated = m_animDeltaPositionIterations > 0 || m_animDeltaLinearVelocityIterations > 0 || m_animDeltaRotationIterations > 0 ||
                m_animDeltaAngularVelocityIterations > 0;
            bool applySnapshot = false;
            if (serverDeltaItem.Valid)
            {
                currentSnapshot.Add(serverDeltaItem.Snapshot);
                m_clientHistory.ApplyDelta(serverDeltaItem.Timestamp, serverDeltaItem.Snapshot);
                applySnapshot = true;
            }

            if (animated)
            {
                if (m_animDeltaPositionIterations > 0)
                {
                    m_clientHistory.ApplyDeltaPosition(m_animDeltaPositionTimestamp, m_animDeltaPosition);
                    currentSnapshot.Position += m_animDeltaPosition;
                    m_animDeltaPositionIterations--;
                }
                if (m_animDeltaLinearVelocityIterations > 0)
                {
                    m_clientHistory.ApplyDeltaLinearVelocity(m_animDeltaLinearVelocityTimestamp, m_animDeltaLinearVelocity);
                    currentSnapshot.LinearVelocity += m_animDeltaLinearVelocity;
                    m_animDeltaLinearVelocityIterations--;
                }
                if (m_animDeltaAngularVelocityIterations > 0)
                {
                    m_clientHistory.ApplyDeltaAngularVelocity(m_animDeltaAngularVelocityTimestamp, m_animDeltaAngularVelocity);
                    currentSnapshot.AngularVelocity += m_animDeltaAngularVelocity;
                    m_animDeltaAngularVelocityIterations--;
                }
                if (m_animDeltaRotationIterations > 0)
                {
                    m_clientHistory.ApplyDeltaRotation(m_animDeltaRotationTimestamp, m_animDeltaRotation);
                    currentSnapshot.Rotation = currentSnapshot.Rotation * Quaternion.Inverse(m_animDeltaRotation);
                    currentSnapshot.Rotation.Normalize();
                    m_animDeltaRotationIterations--;
                }

                applySnapshot = true;
            }
            if (applySnapshot)
            {
                currentSnapshot.Apply(m_entity, setup.ApplyRotation, setup.ApplyPhysics, serverDeltaItem.Type == MySnapshotHistory.SnapshotType.Reset);
            }
            
            //if (MyCompilationSymbols.EnableNetworkClientUpdateTracking)
            {
                if (m_entity.DisplayName.Contains("dicykal") && (serverDeltaItem.Valid || animated))
                {
                    var velocity = (serverTmpSnapshot.Snapshot.Position - m_lastServerSnapshot.Position) / timeDelta;
                    m_lastServerSnapshot = serverTmpSnapshot.Snapshot;

                    var clientTimeDelta = (float)(m_lastClientTimestamp - clientTimestamp).Seconds;
                    var clientVelocity = (currentSnapshot.Position - m_lastClientSnapshot.Position) / clientTimeDelta;
                    m_lastClientSnapshot = currentSnapshot;
                    m_lastClientTimestamp = clientTimestamp;

                    VRage.Trace.MyTrace.Send(VRage.Trace.TraceWindow.MPositions3, m_entity.DisplayName + ": " +
                        tmpSnapshot + " / " + currentSnapshot + " / " + serverTmpSnapshot.Snapshot + "; cvel " + velocity + " / " + clientVelocity + "; " +
                        serverDeltaItem.Valid + ", " + animated + "; " + m_animDeltaPosition * 60);

                    /*var delta = tmpSnapshot.Diff(currentSnapshot);
                    var posLen = delta.Position.Length();
                    var lenLV = delta.LinearVelocity.Length();
                    var sdPosLen = serverDeltaItem.Snapshot.Position.Length();
                    var aPosLen = m_animDeltaPosition.Length();

                    VRage.Trace.MyTrace.Send(VRage.Trace.TraceWindow.MPositions3, m_entity.DisplayName + 
                        ": pos " + delta.Position + " / " + posLen + " lv " + tmpSnapshot.LinearVelocity + " / " + lenLV + 
                        "; sdelta " + serverDeltaItem.Valid + " pos " + serverDeltaItem.Snapshot.Position + " / " + sdPosLen +
                        " anim " + m_animDeltaPositionIterations + " pos " + m_animDeltaPosition + " / " + aPosLen);*/
                }
            }
        }

        public void Write(BitStream stream)
        {
            var snapshot = new MySnapshot(m_entity);
            snapshot.Write(stream);
        }

        public void Read(BitStream stream, MyTimeSpan timeStamp)
        {
            var snapshot = new MySnapshot(stream);

            if (m_entity.Parent == null && m_entity.Physics != null)
            {
                if (m_entity.Physics.IsInWorld && m_entity.Physics.RigidBody != null && !m_entity.Physics.RigidBody.IsActive && snapshot.Active)
                    m_entity.Physics.RigidBody.Activate();
                if (m_entity.Physics.RigidBody == null || m_entity.Physics.RigidBody.IsActive)
                    m_receivedQueue.Add(snapshot, timeStamp);
            }
        }

        public void Reset()
        {
            m_clientHistory.Reset();
            m_animDeltaRotationIterations = m_animDeltaLinearVelocityIterations = 
                m_animDeltaPositionIterations = m_animDeltaAngularVelocityIterations = 0;
            m_lastServerVelocity = Vector3D.PositiveInfinity;
            m_wasReset = true;
        }

        private MySnapshotHistory.MyItem UpdateFromServerQueue(MyTimeSpan clientTimestamp, MyPredictedSnapshotSyncSetup setup)
        {
            bool recalc = false;
            var serverItem = m_receivedQueue.GetItem(clientTimestamp);
            if (serverItem.Valid)
            {
                if (serverItem.Timestamp != m_lastServerTimestamp)
                {
                    var item = m_clientHistory.Get(serverItem.Timestamp, MyTimeSpan.Zero);
                    if (item.Valid && (item.Type == MySnapshotHistory.SnapshotType.Exact || item.Type == MySnapshotHistory.SnapshotType.Interpolation))
                    {
                        m_lastServerTimestamp = serverItem.Timestamp;
                        m_receivedQueue.Prune(clientTimestamp, MyTimeSpan.Zero, 3);
                        m_clientHistory.Prune(serverItem.Timestamp, MyTimeSpan.Zero, 10);

                        MySnapshot delta;
                        if (!serverItem.Snapshot.Active && !setup.IsControlled)
                        {
                            var currentSnapshot = new MySnapshot(m_entity);
                            delta = serverItem.Snapshot.Diff(currentSnapshot);
                            Reset();
                        }
                        else delta = serverItem.Snapshot.Diff(item.Snapshot);
                        if (m_lastServerVelocity.IsValid())
                        {
                            var deltaVelocity = serverItem.Snapshot.LinearVelocity - m_lastServerVelocity;
                            m_lastServerVelocity = serverItem.Snapshot.LinearVelocity;

                            var deltaVelocityLengthSqr = deltaVelocity.LengthSquared();
                            if (m_stopSuspected > 0)
                            {
                                var currentSnapshot = new MySnapshot(m_entity);
                                var maxVelocityDeltaSqr = (MinVelocityChangeToReset / 2) * (MinVelocityChangeToReset / 2);
                                if ((serverItem.Snapshot.LinearVelocity - currentSnapshot.LinearVelocity).LengthSquared() > maxVelocityDeltaSqr)
                                {
                                    Reset();
                                    delta = serverItem.Snapshot.Diff(currentSnapshot);
                                    m_stopSuspected = 0;
                                }
                            }
                            if (deltaVelocityLengthSqr > (MinVelocityChangeToReset * MinVelocityChangeToReset))
                            {
                                m_stopSuspected = 10;
                                if (MyCompilationSymbols.EnableNetworkPositionTracking)
                                    VRage.Trace.MyTrace.Send(VRage.Trace.TraceWindow.MPositions3, "!!!!!!!!!!!!!!!!!!! sudden server velocity change (" + m_entity.DisplayName + "): " + Math.Sqrt(deltaVelocityLengthSqr));
                            }
                            else if (m_stopSuspected > 0) m_stopSuspected--;
                        }
                        else m_lastServerVelocity = serverItem.Snapshot.LinearVelocity;
                        if (m_wasReset)
                        {
                            delta.Position += serverItem.Snapshot.LinearVelocity * (float)(clientTimestamp - serverItem.Timestamp).Seconds;
                            m_wasReset = false;
                            serverItem.Snapshot = delta;
                            serverItem.Type = MySnapshotHistory.SnapshotType.Reset;
                            return serverItem;
                        }

                        /*{
                            var lvsqr = delta.LinearVelocity.LengthSquared();
                            var maxLinearFactorSqr = setup.MaxLinearFactor * setup.MaxLinearFactor;
                            var maxLinVel = MaxLinearVelocityDelta * MaxLinearVelocityDelta * maxLinearFactorSqr;
                            if (lvsqr > maxLinVel)
                            {
                                var similarItem = m_clientHistory.GetSimilar(serverItem.Timestamp, serverItem.Snapshot.LinearVelocity);
                                if (similarItem.Valid)
                                {
                                    var newDelta = serverItem.Snapshot.LinearVelocity - similarItem.Snapshot.LinearVelocity;
                                    if (newDelta.LengthSquared() < maxLinVel)
                                    {
                                        if (MyCompilationSymbols.EnableNetworkClientControlledTracking)
                                            if (m_entity.DisplayName.Contains(m_trackName))
                                                VRage.Trace.MyTrace.Send(VRage.Trace.TraceWindow.MPositions3,
                                                    m_entity.DisplayName + ": old " + newDelta + " new " + delta.LinearVelocity +
                                                    " ------------------------------------------" +
                                                    serverItem.Snapshot + " >> " + item.Snapshot + " >> " + similarItem.Snapshot);
                                        delta.LinearVelocity = Vector3.Zero;
                                    }
                                }
                            }
                        }*/
                        var serverAngVelSqr = serverItem.Snapshot.AngularVelocity.LengthSquared();
                        bool anyAngVel = serverAngVelSqr > 0.001f;
                        var minLinearVelocityFactor = Math.Max(Math.Min(serverItem.Snapshot.LinearVelocity.LengthSquared() /
                                                                        (ReferenceLinearVelocity * ReferenceLinearVelocity), 1.0f), 0.01f);
                        var minAngularVelocityFactor = Math.Max(Math.Min(serverAngVelSqr /
                                                                         (ReferenceAngularVelocity * ReferenceAngularVelocity), 1.0f), 0.01f);
                        int iterations = (int)(SmoothTimesteps * setup.IterationsFactor);
                        var maxFactorSqr = setup.MaxPositionFactor * setup.MaxPositionFactor;

                        var anyDelta = setup.ApplyPhysics && setup.ApplyRotation && delta.AngularVelocity.LengthSquared() > 0.00001f;
                        // position
                        {
                            var psqr = delta.Position.LengthSquared();
                            if (psqr > MaxPositionDelta * MaxPositionDelta * maxFactorSqr)
                            {
                                var dir = delta.Position;
                                var length = dir.Normalize();
                                var newLength = length - MaxPositionDelta * (1.0f - DeltaFactor);
                                delta.Position = dir * newLength;
                                anyDelta = true;
                                m_animDeltaPositionIterations = 0;
                            }
                            else if (!SmoothPositionCorrection)
                            {
                                delta.Position *= DeltaFactor;
                                anyDelta = true;
                                m_animDeltaPositionIterations = 0;
                            }
                            else
                            {
                                if (psqr > MinPositionDelta * MinPositionDelta * minLinearVelocityFactor)
                                    m_animDeltaPositionIterations = iterations;
                                if (m_animDeltaPositionIterations > 0)
                                {
                                    m_animDeltaPosition = delta.Position / m_animDeltaPositionIterations;
                                    m_animDeltaPositionTimestamp = serverItem.Timestamp;
                                }
                                delta.Position = Vector3D.Zero;
                            }
                        }
                        // rotation
                        if (setup.ApplyRotation)
                        {
                            Vector3 axis;
                            float angle;
                            delta.Rotation.GetAxisAngle(out axis, out angle);

                            if (angle > MathHelper.Pi)
                            {
                                axis = -axis;
                                angle = 2 * MathHelper.Pi - angle;
                            }
                            if (angle > MaxRotationAngle * setup.MaxRotationFactor)
                            {
                                delta.Rotation = Quaternion.CreateFromAxisAngle(axis, angle - MaxRotationAngle * (1.0f - DeltaFactor));
                                anyDelta = true;
                                m_animDeltaRotationIterations = 0;
                            }
                            else if (!SmoothRotationCorrection)
                            {
                                delta.Rotation = Quaternion.CreateFromAxisAngle(axis, angle * DeltaFactor);
                                anyDelta = true;
                                m_animDeltaRotationIterations = 0;
                            }
                            else
                            {
                                if (angle > MinRotationAngle * minAngularVelocityFactor)
                                    m_animDeltaRotationIterations = iterations;
                                if (m_animDeltaRotationIterations > 0)
                                {
                                    m_animDeltaRotation = Quaternion.CreateFromAxisAngle(axis, angle / m_animDeltaRotationIterations);
                                    m_animDeltaRotationTimestamp = serverItem.Timestamp;
                                }
                                delta.Rotation = Quaternion.Identity;
                            }
                        }
                        if (setup.ApplyPhysics)
                        {
                            // linear velocity
                            {
                                var lvsqr = delta.LinearVelocity.LengthSquared();
                                if (!SmoothLinearVelocityCorrection)
                                {
                                    delta.LinearVelocity *= DeltaFactor;
                                    anyDelta = true;
                                    m_animDeltaLinearVelocityIterations = 0;
                                }
                                else
                                {
                                    if (lvsqr > MinLinearVelocityDelta * MinLinearVelocityDelta)
                                        m_animDeltaLinearVelocityIterations = iterations;
                                    if (m_animDeltaLinearVelocityIterations > 0)
                                    {
                                        m_animDeltaLinearVelocity = delta.LinearVelocity * DeltaFactor / m_animDeltaLinearVelocityIterations;
                                        m_animDeltaLinearVelocityTimestamp = serverItem.Timestamp;
                                    }
                                    delta.LinearVelocity = Vector3.Zero;
                                }
                            }
                            // angular velocity
                            {
                                var lvsqr = delta.AngularVelocity.LengthSquared();
                                if (!SmoothAngularVelocityCorrection)
                                {
                                    delta.AngularVelocity *= DeltaFactor;
                                    anyDelta = true;
                                    m_animDeltaAngularVelocityIterations = 0;
                                }
                                else
                                {
                                    if (lvsqr > MinAngularVelocityDelta * MinAngularVelocityDelta)
                                        m_animDeltaAngularVelocityIterations = iterations;
                                    if (m_animDeltaAngularVelocityIterations > 0)
                                    {
                                        m_animDeltaAngularVelocity = delta.AngularVelocity * DeltaFactor / m_animDeltaAngularVelocityIterations;
                                        m_animDeltaAngularVelocityTimestamp = serverItem.Timestamp;
                                    }
                                    delta.AngularVelocity = Vector3.Zero;
                                }
                            }
                        }

                        if (!SetTransformCorrections)
                        {
                            delta.Position = Vector3D.Zero;
                            delta.Rotation = Quaternion.Identity;
                            m_animDeltaPositionIterations = m_animDeltaRotationIterations = 0;
                        }
                        if (!SetPhysicsCorrections)
                        {
                            delta.LinearVelocity = Vector3.Zero;
                            delta.AngularVelocity = Vector3.Zero;
                            m_animDeltaLinearVelocityIterations = m_animDeltaAngularVelocityIterations = 0;
                        }

                        if (anyDelta)
                        {
                            if (MyCompilationSymbols.EnableNetworkPositionTracking)
                                if (m_entity.DisplayName.Contains(m_trackName))
                                    VRage.Trace.MyTrace.Send(VRage.Trace.TraceWindow.MPositions3,
                                        m_entity.DisplayName + ": " + serverItem.Snapshot + " >> " + item.Snapshot + " >> " + delta);

                        }
                        serverItem.Snapshot = delta;
                        serverItem.Valid = anyDelta;
                    }
                    else
                    {
                        serverItem.Valid = false;
                        recalc = m_wasReset;
                        if (!m_wasReset && MyCompilationSymbols.EnableNetworkPositionTracking)
                            MyTrace.Send(TraceWindow.MPositions3,
                                "!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!! " + m_entity.DisplayName + ": " + item.Type);
                    }
                }
                else
                {
                    serverItem.Valid = false;
                    recalc = m_wasReset;
                }
            }
            else
            {
                if (!m_receivedQueue.Empty())
                    recalc = true;
                m_clientHistory.Prune(clientTimestamp, MyTimeSpan.FromMilliseconds(1500));
            }
            if (recalc)
            {
                serverItem = m_receivedQueue.Get(clientTimestamp, MyTimeSpan.Zero);
                if (serverItem.Valid && serverItem.Type == MySnapshotHistory.SnapshotType.Exact ||
                    serverItem.Type == MySnapshotHistory.SnapshotType.Interpolation ||
                    serverItem.Type == MySnapshotHistory.SnapshotType.Extrapolation)
                {
                    var currentSnapshot = new MySnapshot(m_entity);
                    var delta = serverItem.Snapshot.Diff(currentSnapshot);
                    serverItem.Valid = true;
                    serverItem.Snapshot = delta;
                    serverItem.Type = MySnapshotHistory.SnapshotType.Reset;
                    return serverItem;
                }
                else
                {
                    serverItem.Valid = false;
                    if (MyCompilationSymbols.EnableNetworkPositionTracking)
                        MyTrace.Send(TraceWindow.MPositions3,
                            "------------------------------------------- " + m_entity.DisplayName + ": " +
                            m_receivedQueue.ToStringTimestamps() + " / " + serverItem.Type);
                }
           } 
            return serverItem;
        }
    }
}