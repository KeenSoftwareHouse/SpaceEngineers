using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using VRage.Library.Utils;
using VRageMath;

[assembly: InternalsVisibleTo("Sandbox.Game.UnitTests")]
namespace Sandbox.Game.Replication.History
{
    public class MySnapshotHistory
    {
        // number of simulations in history to simulate the server
        public static readonly MyTimeSpan DELAY = MyTimeSpan.FromMilliseconds(100);
        static readonly MyTimeSpan MAX_EXTRAPOLATION_DELAY = MyTimeSpan.FromMilliseconds(1000);
        
        public enum SnapshotType
        {
            Exact,
            TooNew,
            Interpolation,
            Extrapolation,
            TooOld,
            Reset
        }

        public struct MyItem
        {
            public bool Valid;
            public SnapshotType Type;
            public MyTimeSpan Timestamp;

            public MySnapshot Snapshot;

            public override string ToString()
            {
                return "Item timestamp: " + Timestamp;
            }
        }

        readonly List<MyItem> m_history = new List<MyItem>();

        public bool Empty()
        {
            return m_history.Count == 0;
        }

        public MyItem GetItem(MyTimeSpan clientTimestamp)
        {
            if (m_history.Count > 0)
            {
                int i = FindIndex(clientTimestamp);
                i--;
                if (i >= 0  && i < m_history.Count)
                    return m_history[i];
            }
            return new MyItem();
        }

        public void Add(MySnapshot snapshot, MyTimeSpan timestamp)
        {
            int exact = FindExact(timestamp);
          //  System.Diagnostics.Debug.Assert(exact == -1, "Timestamp " + timestamp + " already exist in history.");
            if (exact != -1)
                return;

            var item = new MyItem()
            {
                Valid = true,
                Type = SnapshotType.Exact,
                Timestamp = timestamp,
                Snapshot = snapshot
            };
            int idx = FindIndex(timestamp);
            m_history.Insert(idx, item);
        }

        public MyItem GetSimilar(MyTimeSpan clientTimestamp, Vector3 linearVelocity)
        {
            int i = 0;
            int minDeltaIndex = -1;
            float minDeltaSqr = float.MaxValue, deltaSqr;
            while (i < m_history.Count)
            {
                deltaSqr = (linearVelocity - m_history[i].Snapshot.LinearVelocity).LengthSquared();
                if (deltaSqr < minDeltaSqr)
                {
                    minDeltaSqr = deltaSqr;
                    minDeltaIndex = i;
                }
                i++;
            }
            if (minDeltaIndex != -1)
                return m_history[minDeltaIndex];
            else return new MyItem();
        }

        public MyItem Get(MyTimeSpan clientTimestamp, MyTimeSpan delay)
        {
            if (m_history.Count == 0)
                return new MyItem();

            MyTimeSpan delayedTimestamp = clientTimestamp - delay;

            int i = FindIndex(delayedTimestamp);

            MyItem snapshot;
            // exact timestamp
            if (i < m_history.Count && delayedTimestamp == m_history[i].Timestamp)
            {
                snapshot = m_history[i];
                snapshot.Type = SnapshotType.Exact;
            }
            // all timestamps are waaay newer then client timestamp (something wrong!!!)
            else if (i == 0)
            {
                snapshot = m_history[0];
                if (delayedTimestamp == m_history[0].Timestamp)
                    snapshot.Type = SnapshotType.Exact;
                else
                    if (delayedTimestamp < m_history[0].Timestamp)
                        snapshot.Type = SnapshotType.TooNew;
                    else
                        snapshot.Type = SnapshotType.TooOld;
            }
            // interpolation?
            else if (i < m_history.Count && m_history.Count > 1)
            {
                float factor = Factor(delayedTimestamp, i - 1);
                snapshot = Lerp(delayedTimestamp, i - 1, factor);
                snapshot.Type = SnapshotType.Interpolation;
            }
            // extrapolation?
            else if (m_history.Count > 1 && (delayedTimestamp - m_history[m_history.Count - 1].Timestamp) < MAX_EXTRAPOLATION_DELAY)
            {
                int idx = m_history.Count - 2;
                float factor = Factor(delayedTimestamp, idx);
                snapshot = Lerp(delayedTimestamp, idx, factor);

                snapshot.Type = SnapshotType.Extrapolation;
            }
            // we are seriously lagging, wait for new packets
            else
            {
                snapshot = m_history[m_history.Count - 1];
                snapshot.Type = SnapshotType.TooOld;
            }

            return snapshot;
        }

        public void Prune(MyTimeSpan clientTimestamp, MyTimeSpan delay, int leaveCount = 2)
        {
            MyTimeSpan delayedTimestamp = clientTimestamp - delay;

            int i = FindIndex(delayedTimestamp);
            
            // remove unnecessary history, leave at least two old items for possible future extrapolation
            m_history.RemoveRange(0, Math.Max(0, i - leaveCount));
        }

        public void PruneTooOld(MyTimeSpan clientTimestamp)
        {
            Prune(clientTimestamp, MAX_EXTRAPOLATION_DELAY);
        }

        private int FindIndex(MyTimeSpan timestamp)
        {
            int i = 0;
            while (i < m_history.Count && timestamp > m_history[i].Timestamp)
                i++;
            return i;
        }

        private int FindExact(MyTimeSpan timestamp)
        {
            int i = 0;
            while (i < m_history.Count && timestamp != m_history[i].Timestamp)
                i++;
            if (i < m_history.Count)
                return i;
            return -1;
        }

        float Factor(MyTimeSpan timestamp, int index)
        {
            return (float)(timestamp - m_history[index].Timestamp).Ticks / (m_history[index + 1].Timestamp - m_history[index].Timestamp).Ticks;
        }

        MyItem Lerp(MyTimeSpan timestamp, int index, float factor)
        {
            return new MyItem()
            {
                Valid = true,
                Timestamp = timestamp,
                Snapshot = m_history[index].Snapshot.Lerp(m_history[index + 1].Snapshot, factor)
            };
        }

        public void ApplyDeltaPosition(MyTimeSpan timestamp, Vector3D positionDelta)
        {
            int i = 0;
            while (i < m_history.Count)
            {
                if (timestamp <= m_history[i].Timestamp)
                {
                    var item = m_history[i];
                    item.Snapshot.Position += positionDelta;
                    m_history[i] = item;
                }
                i++;
            }
        }

        public void ApplyDeltaLinearVelocity(MyTimeSpan timestamp, Vector3 linearVelocityDelta)
        {
            int i = 0;
            while (i < m_history.Count)
            {
                if (timestamp <= m_history[i].Timestamp)
                {
                    var item = m_history[i];
                    item.Snapshot.LinearVelocity += linearVelocityDelta;
                    m_history[i] = item;
                }
                i++;
            }
        }

        public void ApplyDeltaAngularVelocity(MyTimeSpan timestamp, Vector3 angularVelocityDelta)
        {
            int i = 0;
            while (i < m_history.Count)
            {
                if (timestamp <= m_history[i].Timestamp)
                {
                    var item = m_history[i];
                    item.Snapshot.AngularVelocity += angularVelocityDelta;
                    m_history[i] = item;
                }
                i++;
            }
        }

        public void ApplyDeltaRotation(MyTimeSpan timestamp, Quaternion rotationDelta)
        {
            int i = 0;
            while (i < m_history.Count)
            {
                if (timestamp <= m_history[i].Timestamp)
                {
                    var item = m_history[i];
                    item.Snapshot.Rotation = item.Snapshot.Rotation * Quaternion.Inverse(rotationDelta); item.Snapshot.Rotation.Normalize();
                    m_history[i] = item;
                }
                i++;
            }
        }
        
        public void ApplyDelta(MyTimeSpan timestamp, MySnapshot delta)
        {
            int i = 0;
            while (i < m_history.Count)
            {
                if (timestamp <= m_history[i].Timestamp)
                {
                    var item = m_history[i];
                    item.Snapshot.Add(delta);
                    m_history[i] = item;
                }
                i++;
            }
        }

        public void Reset()
        {
            m_history.Clear();
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            for (int i = 0; i < m_history.Count; i++)
                sb.Append(m_history[i].Timestamp + " (" + m_history[i].Snapshot.Position.ToString("N3") + ") ");
            return sb.ToString();
        }
        public string ToStringRotation()
        {
            var sb = new StringBuilder();
            for (int i = 0; i < m_history.Count; i++)
                sb.Append(m_history[i].Timestamp + " (" + m_history[i].Snapshot.Rotation.ToStringAxisAngle("N3") + ") ");
            return sb.ToString();
        }
        public string ToStringTimestamps()
        {
            var sb = new StringBuilder();
            for (int i = 0; i < m_history.Count; i++)
                sb.Append(m_history[i].Timestamp + " ");
            return sb.ToString();
        }

        public void OverwriteLinearVelocityUntil(MyTimeSpan timestamp, Vector3 linearVelocity, float maxLinVel)
        {
            int i = 0;
            while (i < m_history.Count)
            {
                if (timestamp <= m_history[i].Timestamp)
                {
                    var item = m_history[i];
                    var deltaLinVel = item.Snapshot.LinearVelocity - linearVelocity;
                    if (deltaLinVel.LengthSquared() > maxLinVel)
                        item.Snapshot.LinearVelocity = linearVelocity;
                    else break;//!!
                    m_history[i] = item;
                }
                i++;
            }
        }

        public void OverwriteRotation(MyTimeSpan timestamp, Quaternion rotation)
        {
            int i = 0;
            while (i < m_history.Count)
            {
                if (timestamp <= m_history[i].Timestamp)
                {
                    var item = m_history[i];
                    item.Snapshot.Rotation = rotation;
                    m_history[i] = item;
                }
                i++;
            }
        }

        public MyItem GetLast()
        {
            if (m_history.Count == 0)
                return new MyItem();

            return m_history[m_history.Count - 1];
        }
    }
}
