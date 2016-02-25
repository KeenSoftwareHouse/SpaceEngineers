using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using VRage.Plugins;
using VRage.Reflection;

namespace VRage.ObjectBuilders
{
    public struct MyObjectBuilderType
    {
        public static readonly MyObjectBuilderType Invalid = new MyObjectBuilderType(null);

        private readonly Type m_type;

        public MyObjectBuilderType(Type type)
        {
            Debug.Assert(type == null || typeof(MyObjectBuilder_Base).IsAssignableFrom(type));
            m_type = type;
        }

        public bool IsNull
        {
            get { return m_type == null; }
        }

        public static implicit operator MyObjectBuilderType(Type t)
        {
            return new MyObjectBuilderType(t);
        }

        public static implicit operator Type(MyObjectBuilderType t)
        {
            return t.m_type;
        }

        public static explicit operator MyRuntimeObjectBuilderId(MyObjectBuilderType t)
        {
            return m_idByType[t];
        }

        public static explicit operator MyObjectBuilderType(MyRuntimeObjectBuilderId id)
        {
            return m_typeById[id];
        }

        public static bool operator ==(MyObjectBuilderType lhs, MyObjectBuilderType rhs) { return lhs.m_type == rhs.m_type; }
        public static bool operator !=(MyObjectBuilderType lhs, MyObjectBuilderType rhs) { return lhs.m_type != rhs.m_type; }
        public static bool operator ==(Type lhs, MyObjectBuilderType rhs) { return lhs == rhs.m_type; }
        public static bool operator !=(Type lhs, MyObjectBuilderType rhs) { return lhs != rhs.m_type; }
        public static bool operator ==(MyObjectBuilderType lhs, Type rhs) { return lhs.m_type == rhs; }
        public static bool operator !=(MyObjectBuilderType lhs, Type rhs) { return lhs.m_type != rhs; }

        public override bool Equals(object obj)
        {
            return (obj != null) && (obj is MyObjectBuilderType) &&
                this.Equals((MyObjectBuilderType)obj);
        }

        public bool Equals(MyObjectBuilderType type)
        {
            return type.m_type == this.m_type;
        }

        public override int GetHashCode()
        {
            return m_type.GetHashCode();
        }

        public override string ToString()
        {
            Debug.Assert(m_type != null, "m_type should not be null");
            return m_type != null ? m_type.Name : null;
        }

        public static MyObjectBuilderType Parse(string value)
        {
            return m_typeByName[value];
        }

        /// <summary>
        /// Can handle old values as well.
        /// </summary>
        public static MyObjectBuilderType ParseBackwardsCompatible(string value)
        {
            MyObjectBuilderType result;
            if (m_typeByName.TryGetValue(value, out result))
                return result;
            else
                return m_typeByLegacyName[value];
        }

        public static bool TryParse(string value, out MyObjectBuilderType result)
        {
            if (value == null)
            {
                result = MyObjectBuilderType.Invalid;
                return false;
            }
            return m_typeByName.TryGetValue(value, out result);
        }

        #region Comparer
        public class ComparerType : IEqualityComparer<MyObjectBuilderType>
        {
            public bool Equals(MyObjectBuilderType x, MyObjectBuilderType y)
            {
                return x == y;
            }

            public int GetHashCode(MyObjectBuilderType obj)
            {
                return obj.GetHashCode();
            }
        }

        public static readonly ComparerType Comparer = new ComparerType();
        #endregion

        private static Dictionary<string, MyObjectBuilderType> m_typeByName;
        private static Dictionary<string, MyObjectBuilderType> m_typeByLegacyName;
        private static Dictionary<MyRuntimeObjectBuilderId, MyObjectBuilderType> m_typeById;
        private static Dictionary<MyObjectBuilderType, MyRuntimeObjectBuilderId> m_idByType;
        private static ushort m_idCounter;
        private const int EXPECTED_TYPE_COUNT = 500;

        static MyObjectBuilderType()
        {
            m_typeByName = new Dictionary<string, MyObjectBuilderType>(EXPECTED_TYPE_COUNT);
            m_typeByLegacyName = new Dictionary<string, MyObjectBuilderType>(EXPECTED_TYPE_COUNT);
            m_typeById = new Dictionary<MyRuntimeObjectBuilderId, MyObjectBuilderType>(EXPECTED_TYPE_COUNT, MyRuntimeObjectBuilderId.Comparer);
            m_idByType = new Dictionary<MyObjectBuilderType, MyRuntimeObjectBuilderId>(EXPECTED_TYPE_COUNT, MyObjectBuilderType.Comparer);
        }

        /// <summary>
        /// Register all object builders types from game assemblies. This function must be called after links to assemblies in MyPlugins are set!
        /// Returns false if assembly links are not set. Only MyPlugins.UserAssembly can be null.
        /// </summary>
        public static bool RegisterAssemblies()
        {
            if (m_typeById.Count > 0)
                UnregisterAssemblies();

            MyObjectBuilderType.RegisterFromAssembly(Assembly.GetExecutingAssembly(), registerLegacyNames: true);
            //MyObjectBuilderType.RegisterLegacyName(typeof(MyObjectBuilder_GlobalEventDefinition), "EventDefinition");
            //MyObjectBuilderType.RegisterLegacyName(typeof(MyObjectBuilder_FactionCollection), "Factions");
            if (MyPlugins.SandboxAssemblyReady)
                MyObjectBuilderType.RegisterFromAssembly(MyPlugins.SandboxAssembly, registerLegacyNames: true); //TODO: Will be removed 
            if (MyPlugins.GameAssemblyReady)
                MyObjectBuilderType.RegisterFromAssembly(MyPlugins.GameAssembly, registerLegacyNames: true);
            if (MyPlugins.GameObjectBuildersAssemblyReady)
                MyObjectBuilderType.RegisterFromAssembly(MyPlugins.GameObjectBuildersAssembly, registerLegacyNames: true);
            if (MyPlugins.UserAssemblyReady)
                MyObjectBuilderType.RegisterFromAssembly(MyPlugins.UserAssembly, registerLegacyNames: true);

            return Assembly.GetExecutingAssembly() != null && MyPlugins.SandboxAssembly != null
                && MyPlugins.GameAssembly != null && MyPlugins.GameObjectBuildersAssembly != null;
        }

        // Are the types already registered?
        public static bool IsReady()
        {
            return m_typeByName.Count > 0;
        }

        internal static void RegisterFromAssembly(Assembly assembly, bool registerLegacyNames = false)
        {
            if (assembly == null)
                return;

            var baseType = typeof(MyObjectBuilder_Base);
            var types = assembly.GetTypes();
            Array.Sort(types, FullyQualifiedNameComparer.Default);
            foreach (var type in types)
            {
                if (baseType.IsAssignableFrom(type) && !m_typeByName.ContainsKey(type.Name))
                {
                    var myType = new MyObjectBuilderType(type);
                    var myId = new MyRuntimeObjectBuilderId(++m_idCounter);

                    m_typeById.Add(myId, myType);
                    m_idByType.Add(myType, myId);
                    m_typeByName.Add(type.Name, myType);

                    const string PREFIX = "MyObjectBuilder_";
                    if (registerLegacyNames && type.Name.StartsWith(PREFIX))
                        RegisterLegacyName(myType, type.Name.Substring(PREFIX.Length));

                    var attrs = type.GetCustomAttributes(typeof(MyObjectBuilderDefinitionAttribute), true);
                    if (attrs.Length > 0)
                    {
                        MyObjectBuilderDefinitionAttribute att = (MyObjectBuilderDefinitionAttribute)attrs[0];
                        if (!string.IsNullOrEmpty(att.LegacyName))
                        {
                            RegisterLegacyName(myType, att.LegacyName);
                        }
                    }
                }
            }
        }

        internal static void RegisterLegacyName(MyObjectBuilderType type, string legacyName)
        {
            m_typeByLegacyName.Add(legacyName, type);
        }

        public static void UnregisterAssemblies()
        {
            if (m_typeByLegacyName != null)
                m_typeByLegacyName.Clear();
            if (m_typeById != null)
                m_typeById.Clear();
            if (m_idByType != null)
                m_idByType.Clear();
            if (m_typeByName != null)
                m_typeByName.Clear();
            m_idCounter = default(ushort);
        }
    }
}
