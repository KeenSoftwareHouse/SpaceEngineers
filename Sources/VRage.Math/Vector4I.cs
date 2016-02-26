using System;
using System.Collections.Generic;

namespace VRageMath
{
    //  Integer version of Vector4, not yet fully implemented
    [ProtoBuf.ProtoContract]
    public struct Vector4I : IComparable<Vector4I>
    {
        [ProtoBuf.ProtoMember]
        public int X;
        [ProtoBuf.ProtoMember]
        public int Y;
        [ProtoBuf.ProtoMember]
        public int Z;
        [ProtoBuf.ProtoMember]
        public int W;

        public Vector4I(int x, int y, int z, int w)
        {
            X = x;
            Y = y;
            Z = z;
            W = w;
        }

        public Vector4I(Vector3I xyz, int w)
        {
            X = xyz.X;
            Y = xyz.Y;
            Z = xyz.Z;
            W = w;
        }

        public class EqualityComparer : IEqualityComparer<Vector4I>, IComparer<Vector4I>
        {
            public bool Equals(Vector4I x, Vector4I y)
            {
                return x.X == y.X && x.Y == y.Y && x.Z == y.Z && x.W == y.W;
            }

            public int GetHashCode(Vector4I obj)
            {
                return (((((obj.X * 397) ^ obj.Y) * 397) ^ obj.Z) * 397) ^ obj.W;
            }

            public int Compare(Vector4I x, Vector4I y)
            {
                return x.CompareTo(y);
            }
        }

        public static readonly EqualityComparer Comparer = new EqualityComparer();

        public int CompareTo(Vector4I other)
        {
            int x = X - other.X;
            int y = Y - other.Y;
            int z = Z - other.Z;
            int w = W - other.W;
            return x != 0 ? x : (y != 0 ? y : (z != 0 ? z : w));
        }

        public override string ToString()
        {
            return X + ", " + Y + ", " + Z + ", " + W;
        }
    }
}
