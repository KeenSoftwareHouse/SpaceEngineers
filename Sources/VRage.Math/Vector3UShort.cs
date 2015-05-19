using System;
using System.Collections.Generic;

namespace VRageMath
{
    //  ushort version of Vector3, not yet fully implemented
    [ProtoBuf.ProtoContract]
    public struct Vector3Ushort
    {
        [ProtoBuf.ProtoMember]
        public ushort X;
        [ProtoBuf.ProtoMember]
        public ushort Y;
        [ProtoBuf.ProtoMember]
        public ushort Z;

        public Vector3Ushort(ushort x, ushort y, ushort z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public override string ToString()
        {
            return X + ", " + Y + ", " + Z;
        }

        public static Vector3Ushort operator *(Vector3Ushort v, ushort t)
        {
            return new Vector3Ushort((ushort)(t * v.X), (ushort)(t * v.Y), (ushort)(t * v.Z));
        }

        public static VRageMath.Vector3 operator *(VRageMath.Vector3 vector, Vector3Ushort ushortVector)
        {
            return ushortVector * vector;
        }

        public static VRageMath.Vector3 operator *(Vector3Ushort ushortVector, VRageMath.Vector3 vector)
        {
            return new VRageMath.Vector3(ushortVector.X * vector.X, ushortVector.Y * vector.Y, ushortVector.Z * vector.Z);
        }

        public static explicit operator Vector3(Vector3Ushort v)
        {
            return new Vector3(v.X, v.Y, v.Z);
        }
    }
}
