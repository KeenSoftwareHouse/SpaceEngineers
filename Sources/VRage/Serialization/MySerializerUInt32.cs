using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VRage.Serialization
{
    public class MySerializerUInt32 : MySerializer<UInt32>
    {
        public override void Clone(ref UInt32 value)
        {
            // Primitive type, nothing to do
        }

        public override bool Equals(ref UInt32 a, ref UInt32 b)
        {
            return a == b;
        }

        public override void Read(Library.Collections.BitStream stream, out UInt32 value, MySerializeInfo info)
        {
            if (info.IsVariant || info.IsVariantSigned)
                value = stream.ReadUInt32Variant();
            else
                value = stream.ReadUInt32();
        }

        public override void Write(Library.Collections.BitStream stream, ref UInt32 value, MySerializeInfo info)
        {
            if (info.IsVariant || info.IsVariantSigned)
                stream.WriteVariant(value);
            else
                stream.WriteUInt32(value);
        }
    }
}
