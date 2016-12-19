using VRage.Game.Entity;
using VRage.Library.Collections;
using VRage.Library.Utils;
using VRageMath;
using VRage.Trace;

namespace Sandbox.Game.Replication.History
{
    public class MyAnimatedSnapshotSync : IMySnapshotSync
    {
        readonly MySnapshotHistory m_history = new MySnapshotHistory();

        MyTimeSpan m_safeMovementCounter;
        Vector3 m_lastVelocity;
        private readonly MyEntity m_entity;

        public MyAnimatedSnapshotSync(MyEntity entity)
        {
            m_entity = entity;
        }

        void IMySnapshotSync.Update(MyTimeSpan clientTimestamp, MySnapshotSyncSetup setup)
        {
            VRage.Profiler.NetProfiler.Begin("PhHistory:Update: " + m_entity.DisplayName);
            VRage.Profiler.ProfilerShort.Begin("MyPhysicsHistory.Update");
            // synchronization of timestamps from server and on client is missing (history won't work correctly now)
            var item = m_history.Get(clientTimestamp, MySnapshotHistory.DELAY);
            m_history.PruneTooOld(clientTimestamp);
            VRage.Profiler.ProfilerShort.End();

            if (item.Valid)
            {
                var targetSnapshot = item.Snapshot;
                
                float difpos = (float)Vector3D.Distance(m_entity.PositionComp.GetPosition(), targetSnapshot.Position);
                VRage.Profiler.NetProfiler.Begin("Diff position", 0);
                VRage.Profiler.NetProfiler.End(difpos, 0, "m", "{0} m");

                targetSnapshot.Apply(m_entity, setup.ApplyRotation, setup.ApplyPhysics);
            }

            VRage.Profiler.NetProfiler.End();
        }

        void IMySnapshotSync.Write(BitStream stream)
        {
            var item = new MySnapshot(m_entity);
            item.Write(stream);
        }

        void IMySnapshotSync.Read(BitStream stream, MyTimeSpan timeStamp)
        {
            var item = new MySnapshot(stream);
            m_history.Add(item, timeStamp);
        }

        void IMySnapshotSync.Reset()
        {
            m_history.Reset();
        }
    }
}
