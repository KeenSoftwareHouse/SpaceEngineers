using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VRage.Serialization
{
    public class MySerializerInt64 : MySerializer<Int64>
    {
        public override void Clone(ref Int64 value)
        {
            // Primitive type, nothing to do
        }

        public override bool Equals(ref Int64 a, ref Int64 b)
        {
            return a == b;
        }

        public override void Read(Library.Collections.BitStream stream, out Int64 value, MySerializeInfo info)
        {
            if (info.IsVariant)
                value = (Int64)stream.ReadUInt64Variant();
            else if (info.IsVariantSigned)
                value = stream.ReadInt64Variant();
            else
                value = stream.ReadInt64();
        }

        public override void Write(Library.Collections.BitStream stream, ref Int64 value, MySerializeInfo info)
        {
            if (info.IsVariant)
                stream.WriteVariant((UInt64)value);
            else if (info.IsVariantSigned)
                stream.WriteVariantSigned(value);
            else
                stream.WriteInt64(value);
        }
    }
}
