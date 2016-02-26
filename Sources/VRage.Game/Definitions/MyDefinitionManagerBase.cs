using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using VRage.Game.Definitions;
using VRage.ObjectBuilders;
using VRage.Plugins;
using VRage.Utils;

namespace VRage.Game
{
    public class MyDefinitionManagerBase
    {
        protected MyDefinitionSet m_definitions = new MyDefinitionSet();

        private static MyObjectFactory<MyDefinitionTypeAttribute, MyDefinitionBase> m_definitionFactory;

        public static MyObjectFactory<MyDefinitionTypeAttribute, MyDefinitionBase> GetObjectFactory()
        {
            return m_definitionFactory;
        }

        protected static Dictionary<Type, MyDefinitionPostprocessor> m_postprocessorsByType = new Dictionary<Type, MyDefinitionPostprocessor>();
        protected static List<MyDefinitionPostprocessor> m_postProcessors = new List<MyDefinitionPostprocessor>();

        // This field is set in static constructor of MyDefinitionManager.
        public static MyDefinitionManagerBase Static;

        static MyDefinitionManagerBase()
        {
                m_definitionFactory = new MyObjectFactory<MyDefinitionTypeAttribute, MyDefinitionBase>();
                RegisterTypesFromAssembly(m_definitionFactory.GetType().Assembly);
                RegisterTypesFromAssembly(MyPlugins.GameAssembly);
                RegisterTypesFromAssembly(MyPlugins.SandboxAssembly);
                RegisterTypesFromAssembly(MyPlugins.UserAssembly);

                foreach (var plugin in MyPlugins.Plugins)
                {
                    RegisterTypesFromAssembly(plugin.GetType().Assembly);
                }
        }

        private static HashSet<Assembly> m_registered = new HashSet<Assembly>();

        public static void RegisterTypesFromAssembly(Assembly assembly)
        {
            if (assembly == null)
                return;

            if (m_registered.Contains(assembly)) return;
            m_registered.Add(assembly);

            foreach (Type type in assembly.GetTypes())
            {
                var descriptorArray = type.GetCustomAttributes(typeof(MyDefinitionTypeAttribute), false);
                foreach (MyDefinitionTypeAttribute descriptor in descriptorArray)
                {
                    m_definitionFactory.RegisterDescriptor(descriptor, type);
                    var pp = (MyDefinitionPostprocessor)Activator.CreateInstance(descriptor.PostProcessor);
                    pp.DefinitionType = descriptor.ObjectBuilderType;
                    m_postProcessors.Add(pp);
                    m_postprocessorsByType.Add(descriptor.ObjectBuilderType, pp);
                }
            }

            m_postProcessors.Sort(MyDefinitionPostprocessor.Comparer);
        }

        public static MyDefinitionPostprocessor GetPostProcessor(Type obType)
        {
            MyDefinitionPostprocessor dpp;
            m_postprocessorsByType.TryGetValue(obType, out dpp);

            return dpp;
        }

        public static Type GetObjectBuilderType(Type defType)
        {
            foreach (var attr in defType.GetCustomAttributes(typeof(MyDefinitionTypeAttribute), false))
            {
                return ((MyDefinitionTypeAttribute)attr).ObjectBuilderType;
            }

            Debug.Assert(false, "Object does not have an object builder.");

            return null;
        }

        public T GetDefinition<T>(MyStringHash subtypeId) where T : MyDefinitionBase
        {
            return m_definitions.GetDefinition<T>(subtypeId);
        }

        public T GetDefinition<T>(MyDefinitionId subtypeId) where T : MyDefinitionBase
        {
            return m_definitions.GetDefinition<T>(subtypeId);
        }

        public bool TryGetDefinition<T>(MyStringHash subtypeId, out T def) where T : MyDefinitionBase
        {
            if ((def = m_definitions.GetDefinition<T>(subtypeId)) != null) return true;
            return false;
        }
    }
}
