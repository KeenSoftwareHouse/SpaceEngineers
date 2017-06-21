using System;
using System.Collections.Generic;
using VRage.Collections;
using VRage.Serialization;

namespace VRage.Sync
{
    public class SyncType
    {
        //static int m_TotalCount = 0;

        List<SyncBase> m_properties;
        Action<SyncBase> m_registeredHandlers;

        public ListReader<SyncBase> Properties { get { return new ListReader<SyncBase>(m_properties); } }

        public event Action<SyncBase> PropertyChanged
        {
            add
            {
                m_registeredHandlers += value;
                foreach (var p in m_properties)
                {
                    p.ValueChanged += value;
                }
            }
            remove
            {
                foreach (var p in m_properties)
                {
                    p.ValueChanged -= value;
                }
                m_registeredHandlers -= value;
            }
        }

        public SyncType(List<SyncBase> properties)
        {
            m_properties = properties;

            //m_TotalCount += m_properties.Count;
            //System.Diagnostics.Debug.Print(m_TotalCount.ToString());
        }

#if !XB1 // This used to be #if !UNSHAPER_TMP
        public Sync<T> Add<T>(MySerializeInfo info = null)
        {
            var sync = new Sync<T>(m_properties.Count, info ?? MySerializeInfo.Default);
            sync.ValueChanged += m_registeredHandlers;
            m_properties.Add(sync);
            return sync;
        }
#endif // !XB1

#if !XB1 // XB1_SYNC_NOREFLECTION
        public void Append(object obj)
        {
#if !UNSHAPER_TMP
            //++m_TotalCount;
            //System.Diagnostics.Debug.Print(m_TotalCount.ToString());

            var num = m_properties.Count;
            SyncHelpers.Compose(obj, m_properties.Count, m_properties);
            for (int i = num; i < m_properties.Count; i++)
            {
                m_properties[i].ValueChanged += m_registeredHandlers;
            }
#endif
        }

#else // XB1

        //This is here just for XB1 (to make Sync work without dependency on reflection)
        public Sync<T> CreateAndAddProp<T>()
        {
            //++m_TotalCount;
            //System.Diagnostics.Debug.Print(m_TotalCount.ToString());

            var nextId = m_properties.Count;
            var prop = new Sync<T>(nextId);
            m_properties.Add(prop);
            m_properties[m_properties.Count - 1].ValueChanged += m_registeredHandlers;
            return prop;
        }

#endif // XB1
    }
}
