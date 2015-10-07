using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VRage.Serialization
{
    public class MySerializerUInt8 : MySerializer<Byte>
    {
        public override void Clone(ref Byte value)
        {
            // Primitive type, nothing to do
        }

        public override bool Equals(ref Byte a, ref Byte b)
        {
            return a == b;
        }

        public override void Read(Library.Collections.BitStream stream, out Byte value, MySerializeInfo info)
        {
            value = stream.ReadByte();
        }

        public override void Write(Library.Collections.BitStream stream, ref Byte value, MySerializeInfo info)
        {
            stream.WriteByte(value);
        }
    }
}
