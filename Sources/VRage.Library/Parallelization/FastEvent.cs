using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VRage
{
    public delegate void MyNoArgsDelegate();

    public class FastNoArgsEvent
    {
        FastResourceLock m_lock = new FastResourceLock();
        List<MyNoArgsDelegate> m_delegates = new List<MyNoArgsDelegate>(2);
        List<MyNoArgsDelegate> m_delegatesIterator = new List<MyNoArgsDelegate>(2);

        public event MyNoArgsDelegate Event
        {
            add
            {
                using (m_lock.AcquireExclusiveUsing())
                {
                    m_delegates.Add(value);
                }
            }
            remove
            {
                using (m_lock.AcquireExclusiveUsing())
                {
                    m_delegates.Remove(value);
                }
            }
        }
        public void Raise()
        {
            using (m_lock.AcquireSharedUsing())
            {
                m_delegatesIterator.Clear();
                foreach (MyNoArgsDelegate _delegate in m_delegates)
                {
                    m_delegatesIterator.Add(_delegate);
                }
            }

            foreach (MyNoArgsDelegate _delegate in m_delegatesIterator)
            {
                _delegate();
            }
            m_delegatesIterator.Clear();
        }

    }
}
