using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using VRage.Collections;
using VRage.Library.Collections;
using VRage.Network;
using VRageMath;

namespace VRage.Replication
{
    internal class MyReplicablesAABB : MyReplicablesBase
    {
        #region Fields

        MyDynamicAABBTreeD m_rootsAABB = new MyDynamicAABBTreeD(Vector3D.One);
        HashSet<IMyReplicable> m_roots = new HashSet<IMyReplicable>();
        CacheList<IMyReplicable> m_tmp = new CacheList<IMyReplicable>();
        Dictionary<IMyReplicable, int> m_proxies = new Dictionary<IMyReplicable, int>();

        #endregion

        #region Public

        public override void IterateRange(Action<IMyReplicable> p)
        {
            foreach (var replicable in m_roots)
            {
                p(replicable);
            }
        }

        public override void IterateRoots(Action<IMyReplicable> p)
        {
            using (m_tmp)
            {
                m_rootsAABB.GetAll<IMyReplicable>(m_tmp, false);
                foreach (var replicable in m_tmp)
                {
                    p(replicable);
                }
            }
        }

        public override void GetReplicablesInBox(BoundingBoxD aabb, List<IMyReplicable> list)
        {
            m_rootsAABB.OverlapAllBoundingBox(ref aabb, list);
        }

        #endregion

        #region Implementation

        override protected void AddRoot(IMyReplicable replicable)
        {
            System.Diagnostics.Debug.Assert(!replicable.HasToBeChild, "Cannot add children replicables to root!");

            m_roots.Add(replicable);

            BoundingBoxD aabb = replicable.GetAABB();
            m_proxies.Add(replicable, m_rootsAABB.AddProxy(ref aabb, replicable, 0));

            replicable.OnAABBChanged += OnRootMoved;
        }

        void OnRootMoved(IMyReplicable replicable)
        {
            BoundingBoxD aabb = replicable.GetAABB();
            m_rootsAABB.MoveProxy(m_proxies[replicable], ref aabb, Vector3D.One);
        }

        override protected void RemoveRoot(IMyReplicable replicable)
        {
            m_roots.Remove(replicable);
            m_rootsAABB.RemoveProxy(m_proxies[replicable]);
        }

        override protected bool ContainsRoot(IMyReplicable replicable)
        {
            return m_roots.Contains(replicable);
        }


        #endregion
    }
}
