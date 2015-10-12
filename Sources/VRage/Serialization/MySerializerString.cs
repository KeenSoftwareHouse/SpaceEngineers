using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VRage.Serialization
{
    public class MySerializerString : MySerializer<string>
    {
        public override void Clone(ref string value)
        {
            // Immutable type, nothing to do
        }

        public override bool Equals(ref string a, ref string b)
        {
            return a == b;
        }

        public override void Read(Library.Collections.BitStream stream, out string value, MySerializeInfo info)
        {
            value = stream.ReadPrefixLengthString(info.Encoding);
        }

        public override void Write(Library.Collections.BitStream stream, ref string value, MySerializeInfo info)
        {
            stream.WritePrefixLengthString(value, 0, value.Length, info.Encoding);
        }
    }
}
