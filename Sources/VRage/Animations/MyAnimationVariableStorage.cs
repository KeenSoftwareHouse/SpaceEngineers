using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using VRage.Generics;
using VRage.Utils;

namespace VRage.Animations
{
    /// <summary>
    /// Key-value storage of float values, other types are implicitly converted.
    /// </summary>
    public class MyAnimationVariableStorage : IMyVariableStorage<float>
    {
        Dictionary<MyStringId, float> m_storage = new Dictionary<MyStringId, float>();

        public void SetValue(MyStringId key, float newValue)
        {
            m_storage[key] = newValue;
        }

        public bool GetValue(MyStringId key, out float value)
        {
            return m_storage.TryGetValue(key, out value);
        }

        public void Clear()
        {
            m_storage.Clear();
        }
    }
}
