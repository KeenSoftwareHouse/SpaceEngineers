using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using VRage;
using VRage.Plugins;

namespace Sandbox.Game.Replicables
{
    public class MyReplicableFactory
    {
        Dictionary<Type, Type> m_objTypeToExternalReplicableType = new Dictionary<Type, Type>(32);

        public MyReplicableFactory()
        {
            var assemblies = new Assembly[] { typeof(MySandboxGame).Assembly, MyPlugins.GameAssembly, MyPlugins.SandboxAssembly, MyPlugins.SandboxGameAssembly, MyPlugins.UserAssembly };
            RegisterFromAssemblies(assemblies);
        }

        public void RegisterFromAssemblies(IEnumerable<Assembly> assemblies)
        {
            foreach (var a in assemblies.Where(x => x != null))
                RegisterFromAssembly(a);
        }

        public void RegisterFromAssembly(Assembly assembly)
        {
            foreach (var type in assembly.GetTypes().Where(t => typeof(MyExternalReplicable).IsAssignableFrom(t) && !t.IsAbstract))
            {
                var objType = type.FindGenericBaseTypeArgument(typeof(MyExternalReplicable<>));
                if (objType != null && !m_objTypeToExternalReplicableType.ContainsKey(objType))
                {
                    Debug.Assert(type.HasDefaultConstructor(), string.Format("Type '{0}' should have public constructor", type.Name));
                    m_objTypeToExternalReplicableType.Add(objType, type);
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
                m_objTypeToExternalReplicableType.Add(originalType, resultType); // Faster lookup next time
            }
            return resultType;
        }
    }
}
