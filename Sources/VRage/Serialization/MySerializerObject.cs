using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using VRage.Library.Collections;

namespace VRage.Serialization
{
    public class MySerializerObject<T> : MySerializer<T>
    {
        MyMemberSerializer<T>[] m_memberSerializers;

        public MySerializerObject()
        {
            //var members = typeof(T).GetDataMembers(true, false, true, true, false, true);
            var members = typeof(T).GetDataMembers(true, true, true, true, false, true, true, true);
            var filter = members
                .Where(s => !Attribute.IsDefined(s, typeof(NoSerializeAttribute)))
                .Where(s => Attribute.IsDefined(s, typeof(SerializeAttribute)) || s.IsMemberPublic())
                .Where(Filter);

            m_memberSerializers = filter.Select(s => MyFactory.CreateMemberSerializer<T>(s)).ToArray();
        }

        bool Filter(MemberInfo info)
        {
            if (info.MemberType == MemberTypes.Field)
            {
                return true;
            }
            else if (info.MemberType == MemberTypes.Property)
            {
                var p = (PropertyInfo)info;
                return p.CanRead && p.CanWrite && p.GetIndexParameters().Length == 0;
            }
            return false;
        }

        public override void Clone(ref T value)
        {
            var copy = Activator.CreateInstance<T>();
            foreach (var s in m_memberSerializers)
            {
                s.Clone(ref value, ref copy);
            }
            value = copy;
        }

        public override bool Equals(ref T a, ref T b)
        {
            if (ReferenceEquals(a, b))
                return true;
            else if (AnyNull(a, b))
                return false;
            else
            {
                foreach (var s in m_memberSerializers)
                {
                    if (!s.Equals(ref a, ref b))
                        return false;
                }
                return true;
            }
        }

        public override void Read(BitStream stream, out T value, MySerializeInfo info)
        {
            value = Activator.CreateInstance<T>();
            foreach (var s in m_memberSerializers)
            {
                s.Read(stream, ref value, info.ItemInfo);
            }
        }

        public override void Write(BitStream stream, ref T value, MySerializeInfo info)
        {
            foreach (var s in m_memberSerializers)
            {
                s.Write(stream, ref value, info.ItemInfo);
            }
        }
    }
}
