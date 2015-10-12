using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VRage.Serialization
{
    public class MySerializerDouble : MySerializer<double>
    {
        public override void Clone(ref double value)
        {
            // Primitive type, nothing to do
        }

        public override bool Equals(ref double a, ref double b)
        {
            return a == b;
        }

        public override void Read(Library.Collections.BitStream stream, out double value, MySerializeInfo info)
        {
            value = stream.ReadDouble();
        }

        public override void Write(Library.Collections.BitStream stream, ref double value, MySerializeInfo info)
        {
            stream.WriteDouble(value);
        }
    }
}
