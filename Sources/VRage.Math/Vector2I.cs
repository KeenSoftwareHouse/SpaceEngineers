using System;
using System.Collections.Generic;

namespace VRageMath
{
    //  Integer version of Vector2, not yet fully implemented
    [ProtoBuf.ProtoContract]
    public struct Vector2I
    {
        public static readonly ComparerClass Comparer = new ComparerClass();
        public static Vector2I Zero  = new Vector2I();
        public static Vector2I One   = new Vector2I(1, 1);
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

        public Vector2I(Vector2 vec)
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

        public static Vector2I operator -(Vector2I left, Vector2I right)
        {
            return new Vector2I(left.X - right.X,
                                left.Y - right.Y);
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

        #region Comparer

        public class ComparerClass : IEqualityComparer<Vector2I>
        {
            public bool Equals(Vector2I x, Vector2I y)
            {
                return x.X == y.X & x.Y == y.Y;
            }

            public int GetHashCode(Vector2I obj)
            {
                return (obj.X * 397) ^ obj.Y;
            }
        }

        #endregion
    }
}
