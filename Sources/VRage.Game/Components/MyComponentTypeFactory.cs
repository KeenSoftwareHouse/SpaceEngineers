using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using VRage.ObjectBuilders;
using VRage.Plugins;
using VRage.Utils;

namespace VRage.Components
{
    [PreloadRequired]
    public static class MyComponentTypeFactory
    {
        private static Dictionary<MyStringId, Type> m_idToType;
        private static Dictionary<Type, MyStringId> m_typeToId;

        static MyComponentTypeFactory()
        {
            m_idToType = new Dictionary<MyStringId, Type>(MyStringId.Comparer);
            m_typeToId = new Dictionary<Type, MyStringId>();

            RegisterFromAssembly(Assembly.GetCallingAssembly());
            RegisterFromAssembly(MyPlugins.SandboxAssembly);
            RegisterFromAssembly(MyPlugins.GameAssembly);
            RegisterFromAssembly(MyPlugins.SandboxGameAssembly);            
            RegisterFromAssembly(MyPlugins.UserAssembly);            
        }

        private static void RegisterFromAssembly(Assembly assembly)
        {
            if (assembly == null)
                return;

            var baseType = typeof(MyComponentBase);
            var types = assembly.GetTypes();
            foreach (var type in types)
            {
                if (baseType.IsAssignableFrom(type))
                {
                    AddId(type, MyStringId.GetOrCompute(type.Name));
                }
            }
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
    }
}
