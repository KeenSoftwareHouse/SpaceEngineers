using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SharpDX;

namespace VRage.Serialization
{
    public class MySerializerHalf : MySerializer<Half>
    {
        public override void Clone(ref Half value)
        {
            // Primitive type, nothing to do
        }

        public override bool Equals(ref Half a, ref Half b)
        {
            return a == b;
        }

        public override void Read(Library.Collections.BitStream stream, out Half value, MySerializeInfo info)
        {
            if (info.IsNormalized && info.IsFixed8)
            {
                value = stream.ReadByte() / 255.0f;
            }
            else
            {
                value = stream.ReadHalf();
            }
        }

        public override void Write(Library.Collections.BitStream stream, ref Half value, MySerializeInfo info)
        {
            if (info.IsNormalized && info.IsFixed8)
            {
                stream.WriteByte((byte)(value * 255.0f));
            }
            else
            {
                stream.WriteHalf(value);
            }
        }
    }
}
