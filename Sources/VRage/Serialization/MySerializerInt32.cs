using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VRage.Serialization
{
    public class MySerializerInt32 : MySerializer<Int32>
    {
        public override void Clone(ref Int32 value)
        {
            // Primitive type, nothing to do
        }

        public override bool Equals(ref Int32 a, ref Int32 b)
        {
            return a == b;
        }

        public override void Read(Library.Collections.BitStream stream, out Int32 value, MySerializeInfo info)
        {
            if (info.IsVariant)
                value = (Int32)stream.ReadUInt32Variant();
            else if (info.IsVariantSigned)
                value = stream.ReadInt32Variant();
            else
                value = stream.ReadInt32();
        }

        public override void Write(Library.Collections.BitStream stream, ref Int32 value, MySerializeInfo info)
        {
            if (info.IsVariant)
                stream.WriteVariant((UInt32)value);
            else if (info.IsVariantSigned)
                stream.WriteVariantSigned(value);
            else
                stream.WriteInt32(value);
        }
    }
}
