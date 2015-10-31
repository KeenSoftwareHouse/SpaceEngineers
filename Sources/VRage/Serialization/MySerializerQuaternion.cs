using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using VRageMath;

namespace VRage.Serialization
{
    public class MySerializerQuaternion : MySerializer<Quaternion>
    {
        public override void Clone(ref Quaternion value)
        {
            // Primitive type, nothing to do
        }

        public override bool Equals(ref Quaternion a, ref Quaternion b)
        {
            return a == b;
        }

        public override void Read(Library.Collections.BitStream stream, out Quaternion value, MySerializeInfo info)
        {
            if (info.IsNormalized)
            {
                bool cwNeg = stream.ReadBool();
                bool cxNeg = stream.ReadBool();
                bool cyNeg = stream.ReadBool();
                bool czNeg = stream.ReadBool();
                ushort cx = stream.ReadUInt16();
                ushort cy = stream.ReadUInt16();
                ushort cz = stream.ReadUInt16();

                // Calculate w from x,y,z
                value.X = (float)(cx / 65535.0);
                value.Y = (float)(cy / 65535.0);
                value.Z = (float)(cz / 65535.0);
                if (cxNeg) value.X = -value.X;
                if (cyNeg) value.Y = -value.Y;
                if (czNeg) value.Z = -value.Z;
                float difference = 1.0f - value.X * value.X - value.Y * value.Y - value.Z * value.Z;
                if (difference < 0.0f)
                    difference = 0.0f;
                value.W = (float)(Math.Sqrt(difference));
                if (cwNeg)
                    value.W = -value.W;
            }
            else
            {
                Debug.Fail("Not normalized quaternion?");
                value.X = stream.ReadFloat();
                value.Y = stream.ReadFloat();
                value.Z = stream.ReadFloat();
                value.W = stream.ReadFloat();
            }
        }

        public override void Write(Library.Collections.BitStream stream, ref Quaternion value, MySerializeInfo info)
        {
            if (info.IsNormalized)
            {
                stream.WriteBool(value.W < 0.0f);
                stream.WriteBool(value.X < 0.0f);
                stream.WriteBool(value.Y < 0.0f);
                stream.WriteBool(value.Z < 0.0f);
                stream.WriteUInt16((ushort)(Math.Abs(value.X) * 65535.0));
                stream.WriteUInt16((ushort)(Math.Abs(value.Y) * 65535.0));
                stream.WriteUInt16((ushort)(Math.Abs(value.Z) * 65535.0));
            }
            else
            {
                Debug.Fail("Not normalized quaternion?");
                stream.WriteFloat(value.X);
                stream.WriteFloat(value.Y);
                stream.WriteFloat(value.Z);
                stream.WriteFloat(value.W);
            }
        }
    }
}
