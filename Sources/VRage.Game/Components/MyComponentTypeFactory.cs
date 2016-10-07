using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using VRage.Plugins;
using VRage.Utils;

namespace VRage.Game.Components
{
    [AttributeUsage(AttributeTargets.Class, Inherited = true, AllowMultiple = true)]
    public class MyComponentTypeAttribute : System.Attribute
    {
        public readonly Type ComponentType;

        public MyComponentTypeAttribute(Type componentType)
        {
            ComponentType = componentType;
        }
    }


    [PreloadRequired]
    public static class MyComponentTypeFactory
    {
#if XB1 // XB1_ALLINONEASSEMBLY
        private static bool m_registered = false;
#endif // XB1

        private static Dictionary<MyStringId, Type> m_idToType;
        private static Dictionary<Type, MyStringId> m_typeToId;

        // Dictionary from component type to type which the component is added to container.
        private static Dictionary<Type, Type> m_typeToContainerComponentType;

        static MyComponentTypeFactory()
        {
            m_idToType = new Dictionary<MyStringId, Type>(MyStringId.Comparer);
            m_typeToId = new Dictionary<Type, MyStringId>();
            m_typeToContainerComponentType = new Dictionary<Type, Type>();

#if XB1 // XB1_ALLINONEASSEMBLY
            RegisterFromAssembly(MyAssembly.AllInOneAssembly);
#else // !XB1
            RegisterFromAssembly(Assembly.GetCallingAssembly());
            RegisterFromAssembly(MyPlugins.SandboxAssembly);
            RegisterFromAssembly(MyPlugins.GameAssembly);
            RegisterFromAssembly(MyPlugins.SandboxGameAssembly);            
            RegisterFromAssembly(MyPlugins.UserAssembly);            
#endif // !XB1
        }

        private static void RegisterFromAssembly(Assembly assembly)
        {
            if (assembly == null)
                return;

            var baseType = typeof(MyComponentBase);
#if XB1 // XB1_ALLINONEASSEMBLY
            System.Diagnostics.Debug.Assert(m_registered == false);
            if (m_registered == true)
                return;
            m_registered = true;
            var types = MyAssembly.GetTypes();
#else // !XB1
            var types = assembly.GetTypes();
#endif // !XB1
            foreach (var type in types)
            {
                if (baseType.IsAssignableFrom(type))
                {
                    AddId(type, MyStringId.GetOrCompute(type.Name));
                    RegisterComponentTypeAttribute(type);
                }
            }
        }

        private static void RegisterComponentTypeAttribute(Type type)
        {
            var descriptorArray = type.GetCustomAttributes(typeof(MyComponentTypeAttribute), true);
            Type containerComponentType = null;
            foreach (MyComponentTypeAttribute descriptor in descriptorArray)
            {
                if (descriptor.ComponentType != null)
                {
                    if (containerComponentType == null)
                    {
                        // First attribute is for most derived type.
                        containerComponentType = descriptor.ComponentType;
                        break;
                    }
                }
            }

            if (containerComponentType != null)
                m_typeToContainerComponentType.Add(type, containerComponentType);
        }

        private static void AddId(Type type, MyStringId id)
        {
            Debug.Assert(!m_idToType.ContainsKey(id));
            Debug.Assert(!m_typeToId.ContainsKey(type));
            m_idToType[id] = type;
            m_typeToId[type] = id;
        }

        public static MyStringId GetId(Type type)
        {
            return m_typeToId[type];
        }

        public static Type GetType(MyStringId id)
        {
            return m_idToType[id];
        }

        public static Type GetType(string typeId)
        {
            MyStringId typeIdInt;
            if (MyStringId.TryGet(typeId, out typeIdInt))
            {
                return m_idToType[typeIdInt];
            }

            throw new Exception("Unregistered component typeId! : " + typeId);
        }

        public static Type GetComponentType(Type type) 
        {
            Type componentType;
            if (m_typeToContainerComponentType.TryGetValue(type, out componentType))
                return componentType;
            return null;
        }
    }
}
