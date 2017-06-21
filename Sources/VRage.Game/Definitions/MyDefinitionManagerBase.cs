using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using Sandbox.Common;
using VRage.Game.Definitions;
using VRage.Game.ObjectBuilder;
using VRage.ObjectBuilders;
using VRage.Plugins;
using VRage.Utils;

namespace VRage.Game
{
    public abstract class MyDefinitionManagerBase
    {
        protected MyDefinitionSet m_definitions = new MyDefinitionSet();

        private static MyObjectFactory<MyDefinitionTypeAttribute, MyDefinitionBase> m_definitionFactory = new MyObjectFactory<MyDefinitionTypeAttribute, MyDefinitionBase>();

        public static MyObjectFactory<MyDefinitionTypeAttribute, MyDefinitionBase> GetObjectFactory()
        {
            return m_definitionFactory;
        }

        protected static Dictionary<Type, MyDefinitionPostprocessor> m_postprocessorsByType = new Dictionary<Type, MyDefinitionPostprocessor>();
        protected static List<MyDefinitionPostprocessor> m_postProcessors = new List<MyDefinitionPostprocessor>();

#if !XB1 // XB1_ALLINONEASSEMBLY
        protected static HashSet<Assembly> m_registeredAssemblies = new HashSet<Assembly>();
#endif // !XB1

        // This field is set in static constructor of MyDefinitionManager.
        public static MyDefinitionManagerBase Static;

        // TODO: Should not be static
        private static readonly Dictionary<Type, HashSet<Type>> m_childDefinitionMap = new Dictionary<Type, HashSet<Type>>();

#if !XB1 // XB1_ALLINONEASSEMBLY
        private static HashSet<Assembly> m_registered = new HashSet<Assembly>();
#else // XB1
        private static bool m_registered = false;
#endif // XB1

        public static void RegisterTypesFromAssembly(Assembly assembly)
        {
            if (assembly == null) return;
#if !XB1 // XB1_ALLINONEASSEMBLY
            if (m_registeredAssemblies.Contains(assembly)) return;
            m_registeredAssemblies.Add(assembly);

            if (m_registered.Contains(assembly)) return;
            m_registered.Add(assembly);
#endif // !XB1

#if XB1 // XB1_ALLINONEASSEMBLY
            if (m_registered == true)
                return;
            m_registered = true;
            foreach (Type type in MyAssembly.GetTypes())
#else // !XB1
            foreach (Type type in assembly.GetTypes())
#endif // !XB1
            {
                var descriptorArray = type.GetCustomAttributes(typeof(MyDefinitionTypeAttribute), false);

                if (descriptorArray.Length > 0)
                {
                    if (!type.IsSubclassOf(typeof(MyDefinitionBase)) && type != typeof(MyDefinitionBase))
                    {
                        MyLog.Default.Error("Type {0} is not a definition.", type.Name);
                        continue;
                    }

                    foreach (MyDefinitionTypeAttribute descriptor in descriptorArray)
                    {
                        m_definitionFactory.RegisterDescriptor(descriptor, type);
                        var pp = (MyDefinitionPostprocessor)Activator.CreateInstance(descriptor.PostProcessor);
                        pp.DefinitionType = descriptor.ObjectBuilderType;
                        m_postProcessors.Add(pp);
                        m_postprocessorsByType.Add(descriptor.ObjectBuilderType, pp);
                        MyXmlSerializerManager.RegisterSerializer(descriptor.ObjectBuilderType);
                    }

                    var tp = type;
                    while (tp != typeof(MyDefinitionBase))
                    {
                        tp = tp.BaseType;

                        HashSet<Type> children;
                        if (!m_childDefinitionMap.TryGetValue(tp, out children))
                        {
                            children = new HashSet<Type>();
                            m_childDefinitionMap[tp] = children;
                            children.Add(tp); // make sure it contains itself
                        }

                        children.Add(type);
                    }
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

        public IEnumerable<T> GetDefinitions<T>() where T : MyDefinitionBase
        {
            return m_definitions.GetDefinitionsOfType<T>();
        }

        public IEnumerable<T> GetAllDefinitions<T>() where T : MyDefinitionBase
        {
            return m_definitions.GetDefinitionsOfTypeAndSubtypes<T>();
        }

        public bool TryGetDefinition<T>(MyStringHash subtypeId, out T def) where T : MyDefinitionBase
        {
            if ((def = m_definitions.GetDefinition<T>(subtypeId)) != null) return true;
            return false;
        }

        public abstract MyDefinitionSet GetLoadingSet();

        public MyDefinitionSet Definitions
        {
            get { return m_definitions; }
        }

        public HashSet<Type> GetSubtypes<T>()
        {
            HashSet<Type> subtypes;
            m_childDefinitionMap.TryGetValue(typeof(T), out subtypes);
            return subtypes;
        }
    }
}
