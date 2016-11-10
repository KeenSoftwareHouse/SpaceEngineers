using System;
using System.Collections.Generic;
using VRageMath.PackedVector;

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

        public static explicit operator Byte4(Vector4I xyzw)
        {
            Byte4 b4;
            b4 = new Byte4(xyzw.X, xyzw.Y, xyzw.Z, xyzw.W);
            return b4;
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

        public int this[int index]
        {
            get
            {
                switch (index)
                {
                    case 0:
                        return X;
                    case 1:
                        return Y;
                    case 2:
                        return Z;
                    case 3:
                        return W;
                    default:
                        throw new Exception("Index out of bounds");
                }
            }
            set
            {
                switch (index)
                {
                    case 0:
                        X = value;
                        break;
                    case 1:
                        Y = value;
                        break;
                    case 2:
                        Z = value;
                        break;
                    case 3:
                        W = value;
                        break;
                    default:
                        throw new Exception("Index out of bounds");
                }
            }
        }
    }
}
