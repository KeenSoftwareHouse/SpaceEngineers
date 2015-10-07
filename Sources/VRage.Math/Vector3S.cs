using System;
using System.Collections.Generic;

namespace VRageMath
{
    //  Short version of Vector3, not yet fully implemented
    [ProtoBuf.ProtoContract]
    public struct Vector3S
    {
        [ProtoBuf.ProtoMember]
        public short X;
        [ProtoBuf.ProtoMember]
        public short Y;
        [ProtoBuf.ProtoMember]
        public short Z;

        public static Vector3S Up = new Vector3S(0, 1, 0);
        public static Vector3S Down = new Vector3S(0, -1, 0);
        public static Vector3S Right = new Vector3S(1, 0, 0);
        public static Vector3S Left = new Vector3S(-1, 0, 0);
        public static Vector3S Forward = new Vector3S(0, 0, -1);
        public static Vector3S Backward = new Vector3S(0, 0, 1);

        public Vector3S(Vector3I vec)
            : this(ref vec)
        {
        }

        public Vector3S(ref Vector3I vec)
        {
            X = (short)vec.X;
            Y = (short)vec.Y;
            Z = (short)vec.Z;
        }

        public Vector3S(short x, short y, short z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public Vector3S(float x, float y, float z)
        {
            X = (short) x;
            Y = (short) y;
            Z = (short) z;
        }

        public override string ToString()
        {
            return X + ", " + Y + ", " + Z;
        }

        public override int GetHashCode()
        {
            return (((X * 397) ^ Y) * 397) ^ Z;
        }

        public override bool Equals(object obj)
        {
            if (obj != null)
            {
                var rhs = obj as Vector3S?;
                if (rhs.HasValue)
                    return this == rhs.Value;
            }

            return false;
        }

        public static Vector3S operator *(Vector3S v, short t)
        {
            return new Vector3S((short)(t * v.X), (short)(t * v.Y), (short)(t * v.Z));
        }

        public static Vector3 operator *(Vector3S v, float t)
        {
            return new Vector3(t * v.X, t * v.Y, t * v.Z);
        }

        public static VRageMath.Vector3 operator *(VRageMath.Vector3 vector, Vector3S shortVector)
        {
            return shortVector * vector;
        }

        public static VRageMath.Vector3 operator *(Vector3S shortVector, VRageMath.Vector3 vector)
        {
            return new VRageMath.Vector3(shortVector.X * vector.X, shortVector.Y * vector.Y, shortVector.Z * vector.Z);
        }

        public static bool operator ==(Vector3S v1, Vector3S v2)
        {
            return v1.X == v2.X && v1.Y == v2.Y && v1.Z == v2.Z;
        }
        public static bool operator !=(Vector3S v1, Vector3S v2)
        {
            return v1.X != v2.X || v1.Y != v2.Y || v1.Z != v2.Z;
        }

        public static Vector3S Round(Vector3 v)
        {
            return new Vector3S((short)Math.Round(v.X), (short)Math.Round(v.Y), (short)Math.Round(v.Z));
        }

        public static implicit operator Vector3I(Vector3S me)
        {
            return new Vector3I(me.X, me.Y, me.Z);
        }

        public static Vector3I operator-(Vector3S op1, Vector3B op2) {
            return new Vector3I(op1.X - op2.X, op1.Y - op2.Y, op1.Z - op2.Z);
        }
    }
}
