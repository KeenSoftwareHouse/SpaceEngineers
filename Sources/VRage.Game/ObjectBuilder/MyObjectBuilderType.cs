using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using VRage.Plugins;
using VRage.Reflection;
#if XB1 // XB1_ALLINONEASSEMBLY
using VRage.Utils;
#endif // XB1

namespace VRage.ObjectBuilders
{
    public struct MyObjectBuilderType
    {
#if XB1 // XB1_ALLINONEASSEMBLY
        private static bool m_registered = false;
#endif // XB1
        const string LEGACY_TYPE_PREFIX = "MyObjectBuilder_";

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
            return m_type != null ? m_type.GetHashCode() : 0;
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

        // Are the types already registered?
        public static bool IsReady()
        {
            return m_typeByName.Count > 0;
        }

        public static void RegisterFromAssembly(Assembly assembly, bool registerLegacyNames = false)
        {
            if (assembly == null)
                return;

            var baseType = typeof(MyObjectBuilder_Base);
#if XB1 // XB1_ALLINONEASSEMBLY
            System.Diagnostics.Debug.Assert(m_registered == false);
            if (m_registered == true)
                return;
            m_registered = true;
            var types = MyAssembly.GetTypes();
#else // !XB1
            var types = assembly.GetTypes();
#endif // !XB1
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

                    if (registerLegacyNames && type.Name.StartsWith(LEGACY_TYPE_PREFIX))
                        RegisterLegacyName(myType, type.Name.Substring(LEGACY_TYPE_PREFIX.Length));

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

        /// <summary>
        /// Used for type remapping when overriding definition types
        /// </summary>
        internal static void RemapType(ref SerializableDefinitionId id, Dictionary<string, string> typeOverrideMap)
        {
            string overrideType;
            bool found = typeOverrideMap.TryGetValue(id.TypeIdString, out overrideType);
            if (!found)
            {
                if (id.TypeIdString.StartsWith(LEGACY_TYPE_PREFIX))
                    found = typeOverrideMap.TryGetValue(id.TypeIdString.Substring(LEGACY_TYPE_PREFIX.Length), out overrideType);
            }

            if (!found)
                return;

            id.TypeIdString = overrideType;
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
