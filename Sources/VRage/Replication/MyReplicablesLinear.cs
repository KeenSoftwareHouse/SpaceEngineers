using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using VRage.Collections;
using VRage.Library.Collections;
using VRage.Network;

namespace VRage.Replication
{
    internal class MyReplicablesLinear : MyReplicablesBase
    {
        #region Fields

        const int UPDATE_INTERVAL = 60;

        HashSet<IMyReplicable> m_roots = new HashSet<IMyReplicable>();
        MyDistributedUpdater<List<IMyReplicable>, IMyReplicable> m_updateList = new MyDistributedUpdater<List<IMyReplicable>, IMyReplicable>(UPDATE_INTERVAL);

        #endregion

        #region Public

        public override void IterateRange(Action<IMyReplicable> p)
        {
            m_updateList.Update();
            m_updateList.Iterate(p);
        }

        public override void IterateRoots(Action<IMyReplicable> p)
        {
            foreach (var replicable in m_roots)
            {
                p(replicable);
            }            
        }

        #endregion

        #region Implementation

        override protected void AddRoot(IMyReplicable replicable)
        {
            System.Diagnostics.Debug.Assert(!replicable.HasToBeChild, "Cannot add children replicables to root!");

            m_roots.Add(replicable);
            m_updateList.List.Add(replicable);
        }

        override protected void RemoveRoot(IMyReplicable replicable)
        {
            m_roots.Remove(replicable);
            m_updateList.List.Remove(replicable);
        }

        override protected bool ContainsRoot(IMyReplicable replicable)
        {
            return m_roots.Contains(replicable);
        }


        #endregion
    }
}
