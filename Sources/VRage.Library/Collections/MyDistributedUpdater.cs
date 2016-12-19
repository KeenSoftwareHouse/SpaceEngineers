using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace VRage.Collections
{
    /// <summary>
    /// Class distributing updates on large amount of objects in configurable intervals. 
    /// </summary>
    public class MyDistributedUpdater<V, T> where V : IReadOnlyList<T>, new()
    {
        V m_list = new V();
        int m_updateInterval;
        int m_updateIndex;

        public MyDistributedUpdater(int updateInterval)
        {
            m_updateInterval = updateInterval;
        }

        public void Iterate(Action<T> p)
        {
            for (int i = m_updateIndex; i < m_list.Count; i += m_updateInterval)
            {
                p(m_list[i]);
            }
        }

        public void Update()
        {
            ++m_updateIndex;
            m_updateIndex %= m_updateInterval;
        }

        public V List
        {
            get { return m_list; }
        }
    }
}
