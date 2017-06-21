using System.Collections.Generic;
using VRage;
using VRage.Collections;
using VRage.Generics;
using VRage.Library.Utils;
using VRage.Utils;

namespace VRageRender.Animations
{
    /// <summary>
    /// Key-value storage of float values, other types are implicitly converted.
    /// </summary>
    public class MyAnimationVariableStorage : IMyVariableStorage<float>
    {
        readonly Dictionary<MyStringId, float> m_storage = new Dictionary<MyStringId, float>(MyStringId.Comparer);
        readonly MyRandom m_random = new MyRandom();
        readonly FastResourceLock m_lock = new FastResourceLock();

        // use only for debug, please, resource lock is not present on this one
        // todo: concurent dictionaryreader?
        public DictionaryReader<MyStringId, float> AllVariables
        {
            get { return m_storage; }
        }

        public void SetValue(MyStringId key, float newValue)
        {
            using (m_lock.AcquireExclusiveUsing())
            {
                m_storage[key] = newValue;
            }
        }

        public bool GetValue(MyStringId key, out float value)
        {
            // todo: bigger count of special variables => jump table
            if (key == MyAnimationVariableStorageHints.StrIdRandom)
            {
                value = m_random.NextFloat();
                return true;
            }
            else
            {
                using (m_lock.AcquireSharedUsing())
                {
                    return m_storage.TryGetValue(key, out value);
                }
            }
        }

        public void Clear()
        {
            m_storage.Clear();
        }
    }
}
