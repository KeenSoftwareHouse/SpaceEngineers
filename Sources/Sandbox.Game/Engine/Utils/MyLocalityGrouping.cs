using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRageMath;

namespace Sandbox.Engine.Utils
{
    /// <summary>
    /// Use this class to prevent multiple instances close to each other at the same time.
    /// Call add instance to test whether instance can be added.
    /// </summary>
    public class MyLocalityGrouping
    {
        public enum GroupingMode
        {
            ContainsCenter,
            Overlaps,
        }

        struct InstanceInfo
        {
            public Vector3 Position;
            public float Radius;
            public int EndTimeMs;
        }

        class InstanceInfoComparer: IComparer<InstanceInfo>
        {
            public int Compare(InstanceInfo x, InstanceInfo y)
            {
                return (x.EndTimeMs - y.EndTimeMs);
            }
        }

        public GroupingMode Mode;

        SortedSet<InstanceInfo> m_instances = new SortedSet<InstanceInfo>(new InstanceInfoComparer());

        private int TimeMs
        {
            get { return MySandboxGame.TotalGamePlayTimeInMilliseconds; }
        }

        public MyLocalityGrouping(GroupingMode mode)
        {
            Mode = mode;
        }

        /// <summary>
        /// This is currently O(n), when it's not enough, bounding volume tree or KD-tree will be used.
        /// </summary>
        public bool AddInstance(TimeSpan lifeTime, Vector3 position, float radius, bool removeOld = true)
        {
            if (removeOld)
                RemoveOld();

            foreach (var item in m_instances)
            {
                float testDistance = Mode == GroupingMode.ContainsCenter ? Math.Max(radius, item.Radius) : radius + item.Radius;
                if (Vector3.DistanceSquared(position, item.Position) < testDistance * testDistance)
                {
                    return false;
                }
            }

            m_instances.Add(new InstanceInfo() { EndTimeMs = TimeMs + (int)lifeTime.TotalMilliseconds, Position = position, Radius = radius });
            return true;
        }

        /// <summary>
        /// This is O(r) where r is number of removed elements
        /// </summary>
        public void RemoveOld()
        {
            int currentTimeMs = TimeMs;
            while (m_instances.Count > 0 && m_instances.Min.EndTimeMs < currentTimeMs)
            {
                // This should be O(1)
                m_instances.Remove(m_instances.Min);
            }
        }

        public void Clear()
        {
            m_instances.Clear();
        }
    }
}
