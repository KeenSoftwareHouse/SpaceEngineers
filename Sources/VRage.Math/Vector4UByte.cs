using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VRageMath
{
    [ProtoBuf.ProtoContract]
    public struct Vector4UByte
    {
        [ProtoBuf.ProtoMember]
        public byte X;
        [ProtoBuf.ProtoMember]
        public byte Y;
        [ProtoBuf.ProtoMember]
        public byte Z;
        [ProtoBuf.ProtoMember]
        public byte W;

        public Vector4UByte(byte x, byte y, byte z, byte w)
        {
            X = x;
            Y = y;
            Z = z;
            W = w;
        }

        public override string ToString()
        {
            return X + ", " + Y + ", " + Z + ", " + W;
        }

        public static Vector4UByte Round(Vector3 vec)
        {
            return Round(new Vector4(vec.X, vec.Y, vec.Z, 0));
        }

        public static Vector4UByte Round(Vector4 vec)
        {
            return new Vector4UByte((byte)Math.Round(vec.X), (byte)Math.Round(vec.Y), (byte)Math.Round(vec.Z), 0);
        }

        /// <summary>
        /// Normalizes Vector3 into Vector4UByte, scales vector from (-range, range) to (0, 255)
        /// </summary>
        public static Vector4UByte Normalize(Vector3 vec, float range)
        {
            // Scale from (-range, range) to (-1,1):  vec / range 
            // Scale to (-0.5f, 0.5f): vec / range / 2
            // Scale to (0, 1): (vec / range / 2 + new Vector3(0.5f)
            // Finally scale to (0, 255)
            return Round((vec / range / 2 + new Vector3(0.5f)) * 255);
        }
    }
}
