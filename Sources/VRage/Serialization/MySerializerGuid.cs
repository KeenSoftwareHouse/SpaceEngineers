using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRageMath;

namespace VRage.Serialization
{
    public class MySerializerGuid : MySerializer<Guid>
    {
        public override void Clone(ref Guid value)
        {
            // Primitive type, nothing to do
        }

        public override bool Equals(ref Guid a, ref Guid b)
        {
            return a == b;
        }

        public override void Read(Library.Collections.BitStream stream, out Guid value, MySerializeInfo info)
        {
            string s = stream.ReadPrefixLengthString(info.Encoding);
            value = new Guid(s);
        }

        public override void Write(Library.Collections.BitStream stream, ref Guid value, MySerializeInfo info)
        {
            string s = value.ToString();
            stream.WritePrefixLengthString(s, 0, s.Length, info.Encoding);
        }
    }
}
