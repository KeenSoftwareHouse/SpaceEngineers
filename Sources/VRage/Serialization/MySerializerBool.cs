using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VRage.Serialization
{
    public class MySerializerBool : MySerializer<bool>
    {
        public override void Clone(ref bool value)
        {
            // Primitive type, nothing to do
        }

        public override bool Equals(ref bool a, ref bool b)
        {
            return a == b;
        }

        public override void Read(Library.Collections.BitStream stream, out bool value, MySerializeInfo info)
        {
            value = stream.ReadBool();
        }

        public override void Write(Library.Collections.BitStream stream, ref bool value, MySerializeInfo info)
        {
            stream.WriteBool(value);
        }
    }
}
