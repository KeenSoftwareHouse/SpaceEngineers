using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VRage.Serialization
{
    public class MySerializerInt8 : MySerializer<SByte>
    {
        public override void Clone(ref SByte value)
        {
            // Primitive type, nothing to do
        }

        public override bool Equals(ref SByte a, ref SByte b)
        {
            return a == b;
        }

        public override void Read(Library.Collections.BitStream stream, out SByte value, MySerializeInfo info)
        {
            value = stream.ReadSByte();
        }

        public override void Write(Library.Collections.BitStream stream, ref SByte value, MySerializeInfo info)
        {
            stream.WriteSByte(value);
        }
    }
}
