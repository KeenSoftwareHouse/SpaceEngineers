#if !XB1
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace VRage.Rpc
{
    public class CallSiteCache
    {
        private Dictionary<MethodInfo, CallSite> m_cache = new Dictionary<MethodInfo, CallSite>();
        private Dictionary<object, CallSite> m_associateCache = new Dictionary<object, CallSite>();
        private Dictionary<int, CallSite> m_idToCallsite = new Dictionary<int, CallSite>();
        private Dictionary<Type, object> m_serializers = new Dictionary<Type, object>();

        public CallSite this[MethodInfo info]
        {
            get 
            {
                return m_cache[info]; 
            }
        }

        public CallSite this[int id]
        {
            get { return m_idToCallsite[id]; }
        }

        public CallSite Get<T>(object associatedObject, Func<T, Delegate> getter, T arg)
        {
            CallSite result;
            if (!m_associateCache.TryGetValue(associatedObject, out result))
            {
                result = this[getter(arg).Method];
                m_associateCache[associatedObject] = result;
            }
            return result;
        }

        public CallSite Get(object associatedObject, Func<Delegate> getter)
        {
            CallSite result;
            if (!m_associateCache.TryGetValue(associatedObject, out result))
            {
                result = this[getter().Method];
                m_associateCache[associatedObject] = result;
            }
            return result;
        }

        public Serializer<T> GetSerializer<T>()
        {
            return (Serializer<T>)m_serializers[typeof(T)];
        }

        public Serializer<object> GetSerializer(Type t)
        {
            return (Serializer<object>)m_serializers[t];
        }

        public void Register(CallSite site)
        {
            m_cache.Add(site.MethodInfo, site);
            m_idToCallsite.Add(site.Id, site);
        }

        public void Register(MethodInfo info, ushort id)
        {
            Register(CallSite.Create(info, id, this));
        }

        public void RegisterSerializer<T>(Serializer<T> serializer)
        {
            m_serializers.Add(typeof(T), serializer);
        }

        public void RegisterSerializer(Type serializedType, object serializer)
        {
            m_serializers.Add(serializedType, serializer);
        }

        public void RegisterFromAssembly(Assembly assembly, ref ushort startId)
        {
            foreach (var t in assembly.GetTypes())
            {
                if (t.BaseType != null && t.BaseType.IsGenericType && t.BaseType.GetGenericTypeDefinition() == typeof(Serializer<>))
                {
                    RegisterSerializer(t.BaseType.GetGenericArguments().Single(), Activator.CreateInstance(t));
                }
            }

            foreach (var t in assembly.GetTypes())
            {
                foreach (var m in t.GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
                {
                    if (Attribute.IsDefined(m, typeof(RpcAttribute)))
                    {
                        Register(m, startId++);
                    }
                }
            }
        }
    }
}
#endif // !XB1
