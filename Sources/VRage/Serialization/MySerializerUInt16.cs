using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VRage.Serialization
{
    public class MySerializerUInt16 : MySerializer<UInt16>
    {
        public override void Clone(ref UInt16 value)
        {
            // Primitive type, nothing to do
        }

        public override bool Equals(ref UInt16 a, ref UInt16 b)
        {
            return a == b;
        }

        public override void Read(Library.Collections.BitStream stream, out UInt16 value, MySerializeInfo info)
        {
            if (info.IsVariant || info.IsVariantSigned)
                value = (UInt16)stream.ReadUInt32Variant();
            else
                value = stream.ReadUInt16();
        }

        public override void Write(Library.Collections.BitStream stream, ref UInt16 value, MySerializeInfo info)
        {
            if (info.IsVariant || info.IsVariantSigned)
                stream.WriteVariant(value);
            else
                stream.WriteUInt16(value);
        }
    }
}
