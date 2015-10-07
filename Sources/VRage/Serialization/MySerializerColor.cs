using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRageMath;

namespace VRage.Serialization
{
    public class MySerializerColor : MySerializer<Color>
    {
        public override void Clone(ref Color value)
        {
            // Primitive type, nothing to do
        }

        public override bool Equals(ref Color a, ref Color b)
        {
            return a.PackedValue == b.PackedValue;
        }

        public override void Read(Library.Collections.BitStream stream, out Color value, MySerializeInfo info)
        {
            value.PackedValue = stream.ReadUInt32();
        }

        public override void Write(Library.Collections.BitStream stream, ref Color value, MySerializeInfo info)
        {
            stream.WriteUInt32(value.PackedValue);
        }
    }
}
