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
            // CH: Don't use type.FullName, because for generic types you'll get something like this:
            // Namespace.MyGenericType`1[[OtherNamespace.MyWhatever, AssemblyName, Version=1.0.2.0, Cuntulre=neutral, PublicKeyToken=null]]
            // The problem is that "Version" will be different when building at different times (e.g. when testing on 2 computers from SVN)
            // type.ToString() should be safe for that purpose
            return MyStringHash.GetOrCompute(type.ToString()).GetHashCode();
        }
    }
}
