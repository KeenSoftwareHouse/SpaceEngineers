using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using VRage.ObjectBuilders;
using VRage.Plugins;
#if XB1 // XB1_ALLINONEASSEMBLY
using VRage.Utils;
#endif // XB1

namespace VRage.Game.ObjectBuilder
{
    // TODO: Unify factory management and loading here
    // Also abstract serialization, MP and whatever
    public class MyGlobalTypeMetadata
    {
        /// <summary>
        /// Default type metadata manager.
        /// </summary>
        public static MyGlobalTypeMetadata Static = new MyGlobalTypeMetadata();

        #region Private members

#if !XB1 // XB1_ALLINONEASSEMBLY
        private HashSet<Assembly> m_assemblies = new HashSet<Assembly>();
#endif // !XB1

        private bool m_ready;

        #endregion

        /// <summary>
        /// Register an assembly and it's types metadata.
        /// </summary>
        /// <param name="assembly">Assembly to register.</param>
        public void RegisterAssembly(Assembly assembly)
        {
            if (assembly == null) return;

#if !XB1 // XB1_ALLINONEASSEMBLY
            m_assemblies.Add(assembly);
#endif // !XB1

            MyObjectBuilderSerializer.RegisterFromAssembly(assembly);
            MyObjectBuilderType.RegisterFromAssembly(assembly, true);
            MyXmlSerializerManager.RegisterFromAssembly(assembly);
            MyDefinitionManagerBase.RegisterTypesFromAssembly(assembly);
        }

        public Type GetType(string fullName, bool throwOnError)
        {
#if XB1 // XB1_ALLINONEASSEMBLY
            Type type;
            if ((type = MyAssembly.GetType(fullName, false)) != null)
                return type;
#else // !XB1
            foreach (var assembly in m_assemblies)
            {
                Type type;
                if ((type = assembly.GetType(fullName, false)) != null)
                    return type;
            }
#endif // !XB1

            if (throwOnError)
                throw new TypeLoadException(string.Format("Type {0} was not found in any registered assembly!", fullName));

            return null;
        }

        /// <summary>
        /// Initalize the registry with all the defautl game assemblies.
        /// </summary>
        public void Init()
        {
            if (m_ready)
                return;

#if XB1 // XB1_ALLINONEASSEMBLY
            RegisterAssembly(MyAssembly.AllInOneAssembly);
#else // !XB1
            MyXmlSerializerManager.RegisterSerializableBaseType(typeof(MyObjectBuilder_Base));

            RegisterAssembly(GetType().Assembly); // VRage.Game

            RegisterAssembly(MyPlugins.GameAssembly);
            RegisterAssembly(MyPlugins.SandboxGameAssembly);
            RegisterAssembly(MyPlugins.SandboxAssembly);
            RegisterAssembly(MyPlugins.UserAssembly);
            RegisterAssembly(MyPlugins.GameBaseObjectBuildersAssembly);
            RegisterAssembly(MyPlugins.GameObjectBuildersAssembly);

            foreach (var plugin in MyPlugins.Plugins)
            {
                RegisterAssembly(plugin.GetType().Assembly);
            }
#endif // !XB1

            MyObjectBuilderSerializer.LoadSerializers();

            m_ready = true;
        }
    }
}
