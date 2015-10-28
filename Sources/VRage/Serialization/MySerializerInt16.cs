using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VRage.Serialization
{
    public class MySerializerInt16 : MySerializer<Int16>
    {
        public override void Clone(ref Int16 value)
        {
            // Primitive type, nothing to do
        }

        public override bool Equals(ref Int16 a, ref Int16 b)
        {
            return a == b;
        }

        public override void Read(Library.Collections.BitStream stream, out Int16 value, MySerializeInfo info)
        {
            if (info.IsVariant)
                value = (Int16)stream.ReadUInt32Variant();
            else if (info.IsVariantSigned)
                value = (Int16)stream.ReadInt32Variant();
            else
                value = stream.ReadInt16();
        }

        public override void Write(Library.Collections.BitStream stream, ref Int16 value, MySerializeInfo info)
        {
            if (info.IsVariant)
                stream.WriteVariant((UInt16)value);
            else if (info.IsVariantSigned)
                stream.WriteVariantSigned(value);
            else
                stream.WriteInt16(value);
        }
    }
}
