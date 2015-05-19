using Sandbox.Common.ObjectBuilders.Definitions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using VRage.Plugins;
using VRage.Reflection;

namespace Sandbox.Common.ObjectBuilders
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
            return m_type.Name;
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

        static MyObjectBuilderType()
        {
            m_typeByName = new Dictionary<string,MyObjectBuilderType>(200);
            m_typeByLegacyName = new Dictionary<string, MyObjectBuilderType>(200);
            m_typeById = new Dictionary<MyRuntimeObjectBuilderId,MyObjectBuilderType>(200, MyRuntimeObjectBuilderId.Comparer);
            m_idByType = new Dictionary<MyObjectBuilderType,MyRuntimeObjectBuilderId>(200, MyObjectBuilderType.Comparer);

            MyObjectBuilderType.RegisterFromAssembly(Assembly.GetExecutingAssembly(), registerLegacyNames: true);
            MyObjectBuilderType.RegisterLegacyName(typeof(MyObjectBuilder_GlobalEventDefinition), "EventDefinition");
            MyObjectBuilderType.RegisterLegacyName(typeof(MyObjectBuilder_FactionCollection), "Factions");

            MyObjectBuilderType.RegisterFromAssembly(MyPlugins.GameAssembly, true);
            MyObjectBuilderType.RegisterFromAssembly(MyPlugins.UserAssembly, true);
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
                if (baseType.IsAssignableFrom(type))
                {
                    var myType = new MyObjectBuilderType(type);
                    var myId = new MyRuntimeObjectBuilderId(++m_idCounter);

                    m_typeById.Add(myId, myType);
                    m_idByType.Add(myType, myId);
                    m_typeByName.Add(type.Name, myType);

                    const string PREFIX = "MyObjectBuilder_";
                    if (registerLegacyNames && type.Name.StartsWith(PREFIX))
                        m_typeByLegacyName.Add(type.Name.Substring(PREFIX.Length), myType);
                }
            }
        }

        internal static void RegisterLegacyName(MyObjectBuilderType type, string legacyName)
        {
            m_typeByLegacyName.Add(legacyName, type);
        }

    }

    [ProtoBuf.ProtoContract]
    public struct MyRuntimeObjectBuilderId
    {
        [ProtoBuf.ProtoMember]
        internal readonly ushort Value;

        public MyRuntimeObjectBuilderId(ushort value)
        {
            Value = value;
        }

        public override string ToString()
        {
            return string.Format("{0}: {1}", Value, (MyObjectBuilderType)this);
        }

        #region Comparer
        public class IdComparerType : IComparer<MyRuntimeObjectBuilderId>, IEqualityComparer<MyRuntimeObjectBuilderId>
        {
            public int Compare(MyRuntimeObjectBuilderId x, MyRuntimeObjectBuilderId y)
            {
                return CompareInternal(ref x, ref y);
            }

            public bool Equals(MyRuntimeObjectBuilderId x, MyRuntimeObjectBuilderId y)
            {
                return CompareInternal(ref x, ref y) == 0;
            }

            public int GetHashCode(MyRuntimeObjectBuilderId obj)
            {
                return obj.Value.GetHashCode();
            }

            private static int CompareInternal(ref MyRuntimeObjectBuilderId x, ref MyRuntimeObjectBuilderId y)
            {
                return x.Value - y.Value;
            }
        }

        public static readonly IdComparerType Comparer = new IdComparerType();
        #endregion

    }
}
