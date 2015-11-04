using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VRageRender
{
    class MyIDTracker<T> where T : class
    {
        uint m_ID;
        T m_value;

        internal uint ID { get { return m_ID; } }
        internal T Value { get { return m_value; } }

        static Dictionary<uint, MyIDTracker<T>> m_dict = new Dictionary<uint, MyIDTracker<T>>();

        static internal T FindByID(uint id)
        {
            MyIDTracker<T> result;
            if (m_dict.TryGetValue(id, out result))
            {
                return result.m_value;
            }
            return null;
        }

        internal void Register(uint id, T val)
        {
            m_ID = id;
            m_value = val;
            m_dict[id] = this;
        }

        internal void Deregister()
        {
            m_dict.Remove(m_ID);
            m_value = null;
        }
    }
}
