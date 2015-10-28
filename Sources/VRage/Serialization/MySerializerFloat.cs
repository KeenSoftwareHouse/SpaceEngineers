using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VRage.Serialization
{
    public class MySerializerFloat : MySerializer<float>
    {
        public override void Clone(ref float value)
        {
            // Primitive type, nothing to do
        }

        public override bool Equals(ref float a, ref float b)
        {
            return a == b;
        }

        public override void Read(Library.Collections.BitStream stream, out float value, MySerializeInfo info)
        {
            if (info.IsNormalized && info.IsFixed8)
            {
                value = stream.ReadByte() / 255.0f;
            }
            else if(info.IsNormalized && info.IsFixed16)
            {
                value = stream.ReadUInt16() / 65535.0f;
            }
            else
            {
                value = stream.ReadFloat();
            }
        }

        public override void Write(Library.Collections.BitStream stream, ref float value, MySerializeInfo info)
        {
            if (info.IsNormalized && info.IsFixed8)
            {
                stream.WriteByte((byte)(value * 255.0f));
            }
            else if (info.IsNormalized && info.IsFixed16)
            {
                stream.WriteUInt16((ushort)(value * 65535.0f));
            }
            else
            {
                stream.WriteFloat(value);
            }
        }
    }
}
