#if !XB1
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

#if !UNSHARPER

namespace VRage.Library.Reflection
{
    public class GenericCallCache
    {
        struct Key
        {
            public MethodInfo Method;
            public Type[] Arguments;

            public Key(MethodInfo method, Type[] typeArgs)
            {
                Method = method;
                Arguments = typeArgs;
            }
        }

        class KeyComparer : IEqualityComparer<Key>
        {
            public bool Equals(Key x, Key y)
            {
                if (x.Method != y.Method || x.Arguments.Length != y.Arguments.Length)
                    return false;

                for (int i = 0; i < x.Arguments.Length; i++)
                {
                    if (x.Arguments[i] != y.Arguments[i])
                        return false;
                }
                return true;
            }

            public int GetHashCode(Key obj)
            {
                unchecked
                {
                    int hash = obj.Method.GetHashCode();
                    for (int i = 0; i < obj.Arguments.Length; i++)
                    {
                        hash = (hash * 397) ^ obj.Arguments[i].GetHashCode();
                    }
                    return hash;
                }
            }
        }

        Dictionary<Key, object> m_cache = new Dictionary<Key, object>(new KeyComparer());

        public TDelegate Get<TDelegate>(MethodInfo methodInfo, Type[] arguments)
            where TDelegate : class
        {
            var key = new Key(methodInfo, arguments);
            object deleg;
            if (!m_cache.TryGetValue(key, out deleg))
            {
                deleg = methodInfo.MakeGenericMethod(arguments).CreateDelegate<TDelegate>();
                m_cache[key] = deleg;
            }
            return (TDelegate)deleg;
        }
    }
}

#endif

#endif // !XB1
