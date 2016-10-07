using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using VRage;
using VRage.Plugins;
using VRage.Collections;
#if XB1 // XB1_ALLINONEASSEMBLY
using VRage.Utils;
#endif // XB1

namespace Sandbox.Game.Replication
{
    public class MyReplicableFactory
    {
#if XB1 // XB1_ALLINONEASSEMBLY
        private bool m_registered = false;
#endif // !XB1

        MyConcurrentDictionary<Type, Type> m_objTypeToExternalReplicableType = new MyConcurrentDictionary<Type, Type>(32);

        public MyReplicableFactory()
        {
#if XB1 // XB1_ALLINONEASSEMBLY
            RegisterFromAssembly(MyAssembly.AllInOneAssembly);
#else // !XB1
            var assemblies = new Assembly[] { typeof(MySandboxGame).Assembly, MyPlugins.GameAssembly, MyPlugins.SandboxAssembly, MyPlugins.SandboxGameAssembly, MyPlugins.UserAssembly };
            RegisterFromAssemblies(assemblies);
#endif // !XB1
        }

        public void RegisterFromAssemblies(IEnumerable<Assembly> assemblies)
        {
            foreach (var a in assemblies.Where(x => x != null))
                RegisterFromAssembly(a);
        }

        public void RegisterFromAssembly(Assembly assembly)
        {
#if XB1 // XB1_ALLINONEASSEMBLY
            System.Diagnostics.Debug.Assert(m_registered == false);
            if (m_registered == true)
                return;
            m_registered = true;
            foreach (var type in MyAssembly.GetTypes().Where(t => typeof(MyExternalReplicable).IsAssignableFrom(t) && !t.IsAbstract))
#else // !XB1
            foreach (var type in assembly.GetTypes().Where(t => typeof(MyExternalReplicable).IsAssignableFrom(t) && !t.IsAbstract))
#endif // !XB1
            {
                var objType = type.FindGenericBaseTypeArgument(typeof(MyExternalReplicable<>));
                if (objType != null && !m_objTypeToExternalReplicableType.ContainsKey(objType))
                {
                    Debug.Assert(type.HasDefaultConstructor(), string.Format("Type '{0}' should have public constructor", type.Name));
                    m_objTypeToExternalReplicableType.TryAdd(objType, type);
                }
            }
        }

        public Type FindTypeFor(object obj)
        {
            Type originalType = obj.GetType();
            if (originalType.IsValueType)
                throw new InvalidOperationException("obj cannot be value type");

            Type resultType = null;
            Type lookupType = originalType;
            while (lookupType != typeof(Object) && !m_objTypeToExternalReplicableType.TryGetValue(lookupType, out resultType))
            {
                lookupType = lookupType.BaseType;
            }
            if (originalType != lookupType)
            {
                m_objTypeToExternalReplicableType.TryAdd(originalType, resultType); // Faster lookup next time
            }
            return resultType;
        }
    }
}
