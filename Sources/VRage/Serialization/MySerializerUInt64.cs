using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VRage.Serialization
{
    public class MySerializerUInt64 : MySerializer<UInt64>
    {
        public override void Clone(ref UInt64 value)
        {
            // Primitive type, nothing to do
        }

        public override bool Equals(ref UInt64 a, ref UInt64 b)
        {
            return a == b;
        }

        public override void Read(Library.Collections.BitStream stream, out UInt64 value, MySerializeInfo info)
        {
            if (info.IsVariant || info.IsVariantSigned)
                value = stream.ReadUInt64Variant();
            else
                value = stream.ReadUInt64();
        }

        public override void Write(Library.Collections.BitStream stream, ref UInt64 value, MySerializeInfo info)
        {
            if (info.IsVariant || info.IsVariantSigned)
                stream.WriteVariant(value);
            else
                stream.WriteUInt64(value);
        }
    }
}
