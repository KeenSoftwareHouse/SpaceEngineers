using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VRage.Serialization
{
    public class MySerializerNullable<T> : MySerializer<T?>
        where T : struct
    {
        MySerializer<T> m_serializer = MyFactory.GetSerializer<T>();

        public override void Clone(ref T? value)
        {
            // Values types inside can have references, must clone properly
            if(value.HasValue)
            {
                T val = value.Value;
                m_serializer.Clone(ref val);
                value = val;
            }
        }

        public override bool Equals(ref T? a, ref T? b)
        {
            if (a.HasValue != b.HasValue) // One is null and the other is not, return false
                return false;
            else if (!a.HasValue) // Here both have value or both are null, if null, return true
                return true;
            else
            {
                T aa = a.Value;
                T bb = b.Value;
                return m_serializer.Equals(ref aa, ref bb);
            }
        }

        public override void Read(Library.Collections.BitStream stream, out T? value, MySerializeInfo info)
        {
            if (stream.ReadBool())
            {
                T val;
                m_serializer.Read(stream, out val, info);
                value = val;
            }
            else
            {
                value = null;
            }
        }

        public override void Write(Library.Collections.BitStream stream, ref T? value, MySerializeInfo info)
        {
            if (value.HasValue)
            {
                T val = value.Value;
                stream.WriteBool(true);
                m_serializer.Write(stream, ref val, info);
            }
            else
            {
                stream.WriteBool(false);
            }
        }
    }
}
