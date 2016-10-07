using System;
using System.Collections.Generic;

namespace VRageMath
{
    //  Integer version of Vector2, not yet fully implemented
    [ProtoBuf.ProtoContract]
    public struct Vector2I
    {
        public static readonly ComparerClass Comparer = new ComparerClass();
        public static Vector2I Zero = new Vector2I();
        public static Vector2I One = new Vector2I(1, 1);
        public static Vector2I UnitX = new Vector2I(1, 0);
        public static Vector2I UnitY = new Vector2I(0, 1);

        [ProtoBuf.ProtoMember]
        public int X;
        [ProtoBuf.ProtoMember]
        public int Y;

        public Vector2I(int x, int y)
        {
            X = x;
            Y = y;
        }

        public Vector2I(int width)
        {
            X = width;
            Y = width;
        }

        public Vector2I(Vector2 vec)
        {
            X = (int)vec.X;
            Y = (int)vec.Y;
        }

        public Vector2I(Vector2D vec)
        {
            X = (int)vec.X;
            Y = (int)vec.Y;
        }

        public override string ToString()
        {
            return X + ", " + Y;
        }

        public int Size()
        {
            return Math.Abs(X * Y);
        }

        public static implicit operator Vector2(Vector2I intVector)
        {
            return new Vector2(intVector.X, intVector.Y);
        }

        public static Vector2I operator +(Vector2I left, Vector2I right)
        {
            return new Vector2I(left.X + right.X,
                                left.Y + right.Y);
        }

        public static Vector2I operator +(Vector2I left, int right)
        {
            return new Vector2I(left.X + right,
                                left.Y + right);
        }

        public static Vector2I operator -(Vector2I left, Vector2I right)
        {
            return new Vector2I(left.X - right.X,
                                left.Y - right.Y);
        }

        public static Vector2I operator -(Vector2I left, int value)
        {
            return new Vector2I(left.X - value,
                                left.Y - value);
        }

        public static Vector2I operator -(Vector2I left)
        {
            return new Vector2I(-left.X, -left.Y);
        }

        public static Vector2I operator *(Vector2I value1, int multiplier)
        {
            return new Vector2I(value1.X * multiplier,
                                value1.Y * multiplier);
        }

        public static Vector2I operator /(Vector2I value1, int divider)
        {
            return new Vector2I(value1.X / divider,
                                value1.Y / divider);
        }

        public static bool operator ==(Vector2I left, Vector2I right)
        {
            return left.X == right.X && left.Y == right.Y;
        }

        public static bool operator !=(Vector2I left, Vector2I right)
        {
            return left.X != right.X || left.Y != right.Y;
        }

        public static Vector2I operator <<(Vector2I left, int bits)
        {
            return new Vector2I(left.X << bits, left.Y << bits);
        }

        public static Vector2I operator >>(Vector2I left, int bits)
        {
            return new Vector2I(left.X >> bits, left.Y >> bits);
        }

        public static Vector2I Floor(Vector2 value)
        {
            return new Vector2I((int)Math.Floor(value.X), (int)Math.Floor(value.Y));
        }

        public static Vector2I Round(Vector2 value)
        {
            return new Vector2I((int)Math.Round(value.X), (int)Math.Round(value.Y));
        }

        public bool Between(ref Vector2I start, ref Vector2I end)
        {
            return X >= start.X && X <= end.X || Y >= start.Y && Y <= end.Y;
        }

        public override bool Equals(object obj)
        {
            if (!(obj is Vector2I))
            {
                return false;
            }
            else
            {
                return this == (Vector2I)obj;
            }
        }

        public override int GetHashCode()
        {
            return (X * 397) ^ Y;
        }

        #region Comparer

        public class ComparerClass : IEqualityComparer<Vector2I>
        {
            public bool Equals(Vector2I x, Vector2I y)
            {
                return x.X == y.X && x.Y == y.Y;
            }

            public int GetHashCode(Vector2I obj)
            {
                return (obj.X * 397) ^ obj.Y;
            }
        }

        #endregion

        public static void Min(ref Vector2I v1, ref Vector2I v2, out Vector2I min)
        {
            min.X = Math.Min(v1.X, v2.X);
            min.Y = Math.Min(v1.Y, v2.Y);
        }

        public static void Max(ref Vector2I v1, ref Vector2I v2, out Vector2I max)
        {
            max.X = Math.Max(v1.X, v2.X);
            max.Y = Math.Max(v1.Y, v2.Y);
        }

        public static Vector2I Min(Vector2I v1, Vector2I v2)
        {
            return new Vector2I(Math.Min(v1.X, v2.X), Math.Min(v1.Y, v2.Y));
        }

        public static Vector2I Max(Vector2I v1, Vector2I v2)
        {
            return new Vector2I(Math.Max(v1.X, v2.X), Math.Max(v1.Y, v2.Y));
        }
    }
}
