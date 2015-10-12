using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using VRage.Utils;

namespace VRage.Network
{
    /// <summary>
    /// Type descriptor for synchronized type.
    /// </summary>
    public class MySynchronizedTypeInfo
    {
        public readonly Type Type;
        public readonly TypeId TypeId;
        public readonly int TypeHash;
        public readonly string TypeName;
        public readonly string FullTypeName;
        public readonly bool IsReplicated;

        public readonly MySynchronizedTypeInfo BaseType;

        public readonly MyEventTable EventTable;

        public MySynchronizedTypeInfo(Type type, TypeId id, MySynchronizedTypeInfo baseType, bool isReplicated)
        {
            Type = type;
            TypeId = id;
            TypeHash = GetHashFromType(type);
            TypeName = type.Name;
            FullTypeName = type.FullName;
            BaseType = baseType;
            IsReplicated = isReplicated;

            EventTable = new MyEventTable(this);
        }

        public static int GetHashFromType(Type type)
        {
            return MyStringHash.GetOrCompute(type.FullName).GetHashCode();
        }
    }
}
