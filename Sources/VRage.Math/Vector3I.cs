using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace VRageMath
{
    /**
     * Face in a cube.
     */
    public enum CubeFace
    {
        Left, Right, Up, Down, Forward, Backward
    }


    /// <summary>
    /// A class for simpler traversal of ranges of integer vectors
    /// </summary>
	public struct Vector3I_RangeIterator
    {
        Vector3I m_start;
        Vector3I m_end;

        /// <summary>
        /// Do not modify, public only for optimization!
        /// </summary>
        public Vector3I Current;

        /// <summary>
        /// Note: both start and end are inclusive
        /// </summary>
	public Vector3I_RangeIterator(ref Vector3I start, ref Vector3I end)
        {
            Debug.Assert(start.X <= end.X);
            Debug.Assert(start.Y <= end.Y);
            Debug.Assert(start.Z <= end.Z);

            m_start = start;
            m_end = end;
            Current = m_start;
        }

        public bool IsValid()
        {
            // MZ: assert from the past, i will leave it here
            Debug.Assert(Current.X <= m_end.X && Current.Y <= m_end.Y, "Invalid X and Y values in the Vector3I range iterator!");
            // MZ: changed validation to be safer and take in account all values (resolves crashes in voxel hands)
            return Current.X >= m_start.X && Current.Y >= m_start.Y && Current.Z >= m_start.Z &&
                Current.X <= m_end.X && Current.Y <= m_end.Y && Current.Z <= m_end.Z;
        }

        public void GetNext(out Vector3I next)
        {
            MoveNext();
            next = Current;
        }

        public void MoveNext()
        {
            Current.X++;
            if (Current.X > m_end.X)
            {
                Current.X = m_start.X;
                Current.Y++;
                if (Current.Y > m_end.Y)
                {
                    Current.Y = m_start.Y;
                    Current.Z++;
                }
            }
        }
    }


    [ProtoBuf.ProtoContract, Serializable]
    public struct Vector3I : IEquatable<Vector3I>, IComparable<Vector3I>
    {

        public class EqualityComparer: IEqualityComparer<Vector3I>, IComparer<Vector3I>
        {
            public bool Equals(Vector3I x, Vector3I y)
            {
                return x.X == y.X & x.Y == y.Y & x.Z == y.Z;
            }

            public int GetHashCode(Vector3I obj)
            {
                return (((obj.X * 397) ^ obj.Y) * 397) ^ obj.Z;
            }

            public int Compare(Vector3I x, Vector3I y)
            {
                return x.CompareTo(y);
            }
        }

        public static readonly EqualityComparer Comparer = new EqualityComparer();
        public static Vector3I UnitX = new Vector3I(1, 0, 0);
        public static Vector3I UnitY = new Vector3I(0, 1, 0);
        public static Vector3I UnitZ = new Vector3I(0, 0, 1);
        public static Vector3I Zero = new Vector3I(0, 0, 0);
        public static Vector3I MaxValue = new Vector3I(int.MaxValue, int.MaxValue, int.MaxValue);
        public static Vector3I MinValue = new Vector3I(int.MinValue, int.MinValue, int.MinValue);
        public static Vector3I Up = new Vector3I(0, 1, 0);
        public static Vector3I Down = new Vector3I(0, -1, 0);
        public static Vector3I Right = new Vector3I(1, 0, 0);
        public static Vector3I Left = new Vector3I(-1, 0, 0);
        public static Vector3I Forward = new Vector3I(0, 0, -1);
        public static Vector3I Backward = new Vector3I(0, 0, 1);

        [ProtoBuf.ProtoMember]
        public int X;
        [ProtoBuf.ProtoMember]
        public int Y;
        [ProtoBuf.ProtoMember]
        public int Z;

        public Vector3I(int xyz)
        {
            X = xyz;
            Y = xyz;
            Z = xyz;
        }

        public Vector3I(int x, int y, int z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public Vector3I(Vector2I xy, int z)
        {
            X = xy.X;
            Y = xy.Y;
            Z = z;
        }

        public Vector3I(Vector3 xyz)
        {
            X = (int)xyz.X;
            Y = (int)xyz.Y;
            Z = (int)xyz.Z;
        }

        public Vector3I(Vector3D xyz)
        {
            X = (int)xyz.X;
            Y = (int)xyz.Y;
            Z = (int)xyz.Z;
        }

        public Vector3I(Vector3S xyz)
        {
            X = (int)xyz.X;
            Y = (int)xyz.Y;
            Z = (int)xyz.Z;
        }

        public Vector3I(float x, float y, float z)
        {
            X = (int)x;
            Y = (int)y;
            Z = (int)z;
        }

        public static Vector3I One = new Vector3I(1, 1, 1);

        public override string ToString()
        {
            return string.Format("[X:{0}, Y:{1}, Z:{2}]", X, Y, Z);
        }

        public bool Equals(Vector3I other)
        {
            return other.X == X && other.Y == Y && other.Z == Z;
        }

        public bool IsPowerOfTwo
        {
            get
            {
                return MathHelper.IsPowerOfTwo(X) && MathHelper.IsPowerOfTwo(Y) && MathHelper.IsPowerOfTwo(Z);
            }
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (obj.GetType() != typeof(Vector3I)) return false;
            return Equals((Vector3I)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int result = X;
                result = (result * 397) ^ Y;
                result = (result * 397) ^ Z;
                return result;
            }
        }

        public bool IsInsideInclusive(ref Vector3I min, ref Vector3I max)
        {
            return
                min.X <= this.X && this.X <= max.X &&
                min.Y <= this.Y && this.Y <= max.Y &&
                min.Z <= this.Z && this.Z <= max.Z;
        }

        public bool IsInsideInclusive(Vector3I min, Vector3I max)
        {
            return IsInsideInclusive(ref min, ref max);
        }

        public bool IsInsideExclusive(ref Vector3I min, ref Vector3I max)
        {
            return
                min.X < this.X && this.X < max.X &&
                min.Y < this.Y && this.Y < max.Y &&
                min.Z < this.Z && this.Z < max.Z;
        }

        public bool IsInsideExclusive(Vector3I min, Vector3I max)
        {
            return IsInsideExclusive(ref min, ref max);
        }

        /// <summary>
        /// Calculates rectangular distance.
        /// It's how many sectors you have to travel to get to other sector from current sector.
        /// </summary>
        public int RectangularDistance(Vector3I otherVector)
        {
            return Math.Abs(X - otherVector.X) + Math.Abs(Y - otherVector.Y) + Math.Abs(Z - otherVector.Z);
        }

        /// <summary>
        /// Calculates rectangular distance of this vector, interpreted as a point, from the origin.
        /// </summary>
        /// <returns></returns>
        public int RectangularLength()
        {
            return Math.Abs(X) + Math.Abs(Y) + Math.Abs(Z);
        }

        public int Length()
        {
            return (int)Math.Sqrt(Vector3I.Dot(this, this));
        }

        public static bool BoxIntersects(Vector3I minA, Vector3I maxA, Vector3I minB, Vector3I maxB)
        {
            return BoxIntersects(ref minA, ref maxA, ref minB, ref maxB);
        }

        public static bool BoxIntersects(ref Vector3I minA, ref Vector3I maxA, ref Vector3I minB, ref Vector3I maxB)
        {
            return (maxA.X >= minB.X && minA.X <= maxB.X) && (maxA.Y >= minB.Y && minA.Y <= maxB.Y) && (maxA.Z >= minB.Z && minA.Z <= maxB.Z);
        }

        public static bool BoxContains(Vector3I boxMin, Vector3I boxMax, Vector3I pt)
        {
            return (boxMax.X >= pt.X && boxMin.X <= pt.X) && (boxMax.Y >= pt.Y && boxMin.Y <= pt.Y) && (boxMax.Z >= pt.Z && boxMin.Z <= pt.Z);
        }

        public static bool BoxContains(ref Vector3I boxMin, ref Vector3I boxMax, ref Vector3I pt)
        {
            return (boxMax.X >= pt.X && boxMin.X <= pt.X) && (boxMax.Y >= pt.Y && boxMin.Y <= pt.Y) && (boxMax.Z >= pt.Z && boxMin.Z <= pt.Z);
        }

        public static Vector3I operator *(Vector3I a, Vector3I b)
        {
            return new Vector3I(a.X * b.X, a.Y * b.Y, a.Z * b.Z);
        }

        public static bool operator ==(Vector3I a, Vector3I b)
        {
            return a.X == b.X && a.Y == b.Y && a.Z == b.Z;
        }

        public static bool operator !=(Vector3I a, Vector3I b)
        {
            return !(a == b);
        }

        public static Vector3 operator +(Vector3I a, float b)
        {
            return new Vector3(a.X + b, a.Y + b, a.Z + b);
        }

        public static Vector3 operator *(Vector3I a, Vector3 b)
        {
            return new Vector3(a.X * b.X, a.Y * b.Y, a.Z * b.Z);
        }

        public static Vector3 operator *(Vector3 a, Vector3I b)
        {
            return new Vector3(a.X * b.X, a.Y * b.Y, a.Z * b.Z);
        }

        public static Vector3 operator *(float num, Vector3I b)
        {
            return new Vector3(num * b.X, num * b.Y, num * b.Z);
        }

        public static Vector3 operator *(Vector3I a, float num)
        {
            return new Vector3(num * a.X, num * a.Y, num * a.Z);
        }

        public static Vector3D operator *(double num, Vector3I b)
        {
            return new Vector3D(num * b.X, num * b.Y, num * b.Z);
        }

        public static Vector3D operator *(Vector3I a, double num)
        {
            return new Vector3D(num * a.X, num * a.Y, num * a.Z);
        }

        public static Vector3 operator /(Vector3I a, float num)
        {
            return new Vector3(a.X / num, a.Y / num, a.Z / num);
        }

        public static Vector3 operator /(float num, Vector3I a)
        {
            return new Vector3(num / a.X, num / a.Y, num / a.Z);
        }

        public static Vector3I operator /(Vector3I a, int num)
        {
            return new Vector3I(a.X / num, a.Y / num, a.Z / num);
        }

        public static Vector3I operator /(Vector3I a, Vector3I b)
        {
            return new Vector3I(a.X / b.X, a.Y / b.Y, a.Z / b.Z);
        }

        public static Vector3I operator %(Vector3I a, int num)
        {
            return new Vector3I(a.X % num, a.Y % num, a.Z % num);
        }

        public static Vector3I operator >>(Vector3I v, int shift)
        {
            return new Vector3I(v.X >> shift, v.Y >> shift, v.Z >> shift);
        }

        public static Vector3I operator <<(Vector3I v, int shift)
        {
            return new Vector3I(v.X << shift, v.Y << shift, v.Z << shift);
        }

        public static Vector3I operator &(Vector3I v, int mask)
        {
            return new Vector3I(v.X & mask, v.Y & mask, v.Z & mask);
        }

        public static Vector3I operator |(Vector3I v, int mask)
        {
            return new Vector3I(v.X | mask, v.Y | mask, v.Z | mask);
        }

        public static Vector3I operator ^(Vector3I v, int mask)
        {
            return new Vector3I(v.X ^ mask, v.Y ^ mask, v.Z ^ mask);
        }

        public static Vector3I operator ~(Vector3I v)
        {
            return new Vector3I(~v.X, ~v.Y, ~v.Z);
        }

        public static Vector3I operator *(int num, Vector3I b)
        {
            return new Vector3I(num * b.X, num * b.Y, num * b.Z);
        }

        public static Vector3I operator *(Vector3I a, int num)
        {
            return new Vector3I(num * a.X, num * a.Y, num * a.Z);
        }

        public static Vector3I operator +(Vector3I a, Vector3I b)
        {
            return new Vector3I(a.X + b.X, a.Y + b.Y, a.Z + b.Z);
        }

        public static Vector3I operator +(Vector3I a, int b)
        {
            return new Vector3I(a.X + b, a.Y + b, a.Z + b);
        }

        public static Vector3I operator -(Vector3I a, Vector3I b)
        {
            return new Vector3I(a.X - b.X, a.Y - b.Y, a.Z - b.Z);
        }

        public static Vector3I operator -(Vector3I a, int b)
        {
            return new Vector3I(a.X - b, a.Y - b, a.Z - b);
        }

        public static Vector3I operator -(Vector3I a)
        {
            return new Vector3I(-a.X, -a.Y, -a.Z);
        }

        public static Vector3I Min(Vector3I value1, Vector3I value2)
        {
            Vector3I vector3;
            vector3.X = value1.X < value2.X ? value1.X : value2.X;
            vector3.Y = value1.Y < value2.Y ? value1.Y : value2.Y;
            vector3.Z = value1.Z < value2.Z ? value1.Z : value2.Z;
            return vector3;
        }

        public static void Min(ref Vector3I value1, ref Vector3I value2, out Vector3I result)
        {
            result.X = value1.X < value2.X ? value1.X : value2.X;
            result.Y = value1.Y < value2.Y ? value1.Y : value2.Y;
            result.Z = value1.Z < value2.Z ? value1.Z : value2.Z;
        }

        /// <summary>
        /// Returns the component of the vector, whose absolute value is smallest of all the three components.
        /// </summary>
        public int AbsMin()
        {
            if (Math.Abs(X) < Math.Abs(Y))
            {
                if (Math.Abs(X) < Math.Abs(Z)) return Math.Abs(X);
                else return Math.Abs(Z);
            }
            else
            {
                if (Math.Abs(Y) < Math.Abs(Z)) return Math.Abs(Y);
                else return Math.Abs(Z);
            }
        }

        public static Vector3I Max(Vector3I value1, Vector3I value2)
        {
            Vector3I vector3;
            vector3.X = value1.X > value2.X ? value1.X : value2.X;
            vector3.Y = value1.Y > value2.Y ? value1.Y : value2.Y;
            vector3.Z = value1.Z > value2.Z ? value1.Z : value2.Z;
            return vector3;
        }

        public static void Max(ref Vector3I value1, ref Vector3I value2, out Vector3I result)
        {
            result.X = value1.X > value2.X ? value1.X : value2.X;
            result.Y = value1.Y > value2.Y ? value1.Y : value2.Y;
            result.Z = value1.Z > value2.Z ? value1.Z : value2.Z;
        }

        /// <summary>
        /// Returns the component of the vector, whose absolute value is largest of all the three components.
        /// </summary>
        public int AbsMax()
        {
            if (Math.Abs(X) > Math.Abs(Y))
            {
                if (Math.Abs(X) > Math.Abs(Z)) return Math.Abs(X);
                else return Math.Abs(Z);
            }
            else
            {
                if (Math.Abs(Y) > Math.Abs(Z)) return Math.Abs(Y);
                else return Math.Abs(Z);
            }
        }

        public int AxisValue(Base6Directions.Axis axis)
        {
            if (axis == Base6Directions.Axis.ForwardBackward) return Z;
            else if (axis == Base6Directions.Axis.LeftRight) return X;
            Debug.Assert(axis == Base6Directions.Axis.UpDown, "Invalid axis in Vector3I.AxisProjection!");
            return Y;
        }

        public static CubeFace GetDominantDirection(Vector3I val)
        {
            if (Math.Abs(val.X) > Math.Abs(val.Y))
            {
                if (Math.Abs(val.X) > Math.Abs(val.Z))
                {
                    if (val.X > 0) return CubeFace.Right;
                    else return CubeFace.Left;
                }
                else
                {
                    if (val.Z > 0) return CubeFace.Backward;
                    else return CubeFace.Forward;
                }
            }
            else
            {
                if (Math.Abs(val.Y) > Math.Abs(val.Z))
                {
                    if (val.Y > 0) return CubeFace.Up;
                    else return CubeFace.Down;
                }
                else
                {
                    if (val.Z > 0) return CubeFace.Backward;
                    else return CubeFace.Forward;
                }
            }
        }

        public static Vector3I GetDominantDirectionVector(Vector3I val)
        {
            if (Math.Abs(val.X) > Math.Abs(val.Y))
            {
                val.Y = 0;
                if (Math.Abs(val.X) > Math.Abs(val.Z))
                {
                    val.Z = 0;
                    if (val.X > 0) val.X = 1;
                    else val.X = -1;
                }
                else
                {
                    val.X = 0;
                    if (val.Z > 0) val.Z = 1;
                    else val.Z = -1;
                }
            }
            else
            {
                val.X = 0;
                if (Math.Abs(val.Y) > Math.Abs(val.Z))
                {
                    val.Z = 0;
                    if (val.Y > 0) val.Y = 1;
                    else val.Y = -1;
                }
                else
                {
                    val.Y = 0;
                    if (val.Z > 0) val.Z = 1;
                    else val.Z = -1;
                }
            }
            return val;
        }

        /// <summary>
        /// Returns a vector that is equal to the projection of the input vector to the coordinate axis that corresponds
        /// to the original vector's largest value.
        /// </summary>
        /// <param name="value1">Source vector.</param>
        public static Vector3I DominantAxisProjection(Vector3I value1)
        {
            if (Math.Abs(value1.X) > Math.Abs(value1.Y))
            {
                value1.Y = 0;
                if (Math.Abs(value1.X) > Math.Abs(value1.Z))
                    value1.Z = 0;
                else
                    value1.X = 0;
            }
            else
            {
                value1.X = 0;
                if (Math.Abs(value1.Y) > Math.Abs(value1.Z))
                    value1.Z = 0;
                else
                    value1.Y = 0;
            }
            return value1;
        }

        /// <summary>
        /// Calculates a vector that is equal to the projection of the input vector to the coordinate axis that corresponds
        /// to the original vector's largest value. The result is saved into a user-specified variable.
        /// </summary>
        /// <param name="value1">Source vector.</param><param name="result">[OutAttribute] The projected vector.</param>
        public static void DominantAxisProjection(ref Vector3I value1, out Vector3I result)
        {
            if (Math.Abs(value1.X) > Math.Abs(value1.Y))
            {
                if (Math.Abs(value1.X) > Math.Abs(value1.Z))
                    result = new Vector3I(value1.X, 0, 0);
                else
                    result = new Vector3I(0, 0, value1.Z);
            }
            else
            {
                if (Math.Abs(value1.Y) > Math.Abs(value1.Z))
                    result = new Vector3I(0, value1.Y, 0);
                else
                    result = new Vector3I(0, 0, value1.Z);
            }
        }

        public static Vector3I Sign(Vector3 value)
        {
            return new Vector3I(Math.Sign(value.X), Math.Sign(value.Y), Math.Sign(value.Z));
        }

        public static Vector3I Sign(Vector3I value)
        {
            return new Vector3I(Math.Sign(value.X), Math.Sign(value.Y), Math.Sign(value.Z));
        }

        public static Vector3I Round(Vector3 value)
        {
            Vector3I result;
            Round(ref value, out result);
            return result;
        }

        public static Vector3I Round(Vector3D value)
        {
            Vector3I result;
            Round(ref value, out result);
            return result;
        }

        public static void Round(ref Vector3 v, out Vector3I r)
        {
            r.X = (int)Math.Round(v.X, MidpointRounding.AwayFromZero);
            r.Y = (int)Math.Round(v.Y, MidpointRounding.AwayFromZero);
            r.Z = (int)Math.Round(v.Z, MidpointRounding.AwayFromZero);
        }

        public static void Round(ref Vector3D v, out Vector3I r)
        {
            r.X = (int)Math.Round(v.X, MidpointRounding.AwayFromZero);
            r.Y = (int)Math.Round(v.Y, MidpointRounding.AwayFromZero);
            r.Z = (int)Math.Round(v.Z, MidpointRounding.AwayFromZero);
        }
        public static Vector3I Floor(Vector3 value)
        {
            return new Vector3I((int)Math.Floor(value.X), (int)Math.Floor(value.Y), (int)Math.Floor(value.Z));
        }

        public static Vector3I Floor(Vector3D value)
        {
            return new Vector3I((int)Math.Floor(value.X), (int)Math.Floor(value.Y), (int)Math.Floor(value.Z));
        }

        public static void Floor(ref Vector3 v, out Vector3I r)
        {
            r.X = (int)Math.Floor(v.X);
            r.Y = (int)Math.Floor(v.Y);
            r.Z = (int)Math.Floor(v.Z);
        }

        public static void Floor(ref Vector3D v, out Vector3I r)
        {
            r.X = (int)Math.Floor(v.X);
            r.Y = (int)Math.Floor(v.Y);
            r.Z = (int)Math.Floor(v.Z);
        }

        public static Vector3I Ceiling(Vector3 value)
        {
            return new Vector3I((int)Math.Ceiling(value.X), (int)Math.Ceiling(value.Y), (int)Math.Ceiling(value.Z));
        }

        public static Vector3I Trunc(Vector3 value)
        {
            return new Vector3I((int)value.X, (int)value.Y, (int)value.Z);
        }

        // X->Y, Y->Z, Z->X
        public static Vector3I Shift(Vector3I value)
        {
            return new Vector3I(value.Z, value.X, value.Y);
        }

        public static implicit operator Vector3(Vector3I value)
        {
            return new Vector3(value.X, value.Y, value.Z);
        }

        public static implicit operator Vector3D(Vector3I value)
        {
            return new Vector3D(value.X, value.Y, value.Z);
        }

        /// <summary>
        /// Transforms a Vector3I by the given Matrix.
        /// </summary>
        /// <param name="position">The source Vector3I.</param><param name="matrix">The transformation Matrix.</param><param name="result">[OutAttribute] The transformed vector.</param>
        public static void Transform(ref Vector3I position, ref Matrix matrix, out Vector3I result)
        {
            int num1 = (int)(position.X * (int)Math.Round(matrix.M11) + position.Y * (int)Math.Round(matrix.M21) + position.Z * (int)Math.Round(matrix.M31)) + (int)Math.Round(matrix.M41);
            int num2 = (int)(position.X * (int)Math.Round(matrix.M12) + position.Y * (int)Math.Round(matrix.M22) + position.Z * (int)Math.Round(matrix.M32)) + (int)Math.Round(matrix.M42);
            int num3 = (int)(position.X * (int)Math.Round(matrix.M13) + position.Y * (int)Math.Round(matrix.M23) + position.Z * (int)Math.Round(matrix.M33)) + (int)Math.Round(matrix.M43);
            result.X = num1;
            result.Y = num2;
            result.Z = num3;
        }

        public static void Transform(ref Vector3I value, ref Quaternion rotation, out Vector3I result)
        {
            float num1 = rotation.X + rotation.X;
            float num2 = rotation.Y + rotation.Y;
            float num3 = rotation.Z + rotation.Z;
            float num4 = rotation.W * num1;
            float num5 = rotation.W * num2;
            float num6 = rotation.W * num3;
            float num7 = rotation.X * num1;
            float num8 = rotation.X * num2;
            float num9 = rotation.X * num3;
            float num10 = rotation.Y * num2;
            float num11 = rotation.Y * num3;
            float num12 = rotation.Z * num3;
            float num13 = (float)((double)value.X * (1.0 - (double)num10 - (double)num12) + (double)value.Y * ((double)num8 - (double)num6) + (double)value.Z * ((double)num9 + (double)num5));
            float num14 = (float)((double)value.X * ((double)num8 + (double)num6) + (double)value.Y * (1.0 - (double)num7 - (double)num12) + (double)value.Z * ((double)num11 - (double)num4));
            float num15 = (float)((double)value.X * ((double)num9 - (double)num5) + (double)value.Y * ((double)num11 + (double)num4) + (double)value.Z * (1.0 - (double)num7 - (double)num10));
            result.X = (int)Math.Round(num13);
            result.Y = (int)Math.Round(num14);
            result.Z = (int)Math.Round(num15);
        }

        public static Vector3I Transform(Vector3I value, Quaternion rotation)
        {
            Vector3I result;
            Transform(ref value, ref rotation, out result);
            return result;
        }

        public static void Transform(ref Vector3I value, ref MatrixI matrix, out Vector3I result)
        {
            result = value.X * Base6Directions.GetIntVector(matrix.Right) +
                     value.Y * Base6Directions.GetIntVector(matrix.Up) +
                     value.Z * Base6Directions.GetIntVector(matrix.Backward) +
                     matrix.Translation;
        }

        public static Vector3I Transform(Vector3I value, MatrixI transformation)
        {
            Vector3I result;
            Transform(ref value, ref transformation, out result);
            return result;
        }

        public static Vector3I Transform(Vector3I value, ref MatrixI transformation)
        {
            Vector3I result;
            Transform(ref value, ref transformation, out result);
            return result;
        }

        public static Vector3I TransformNormal(Vector3I value, ref MatrixI transformation)
        {
            Vector3I result;
            TransformNormal(ref value, ref transformation, out result);
            return result;
        }

        /// <summary>
        /// Transforms a vector normal by a matrix.
        /// </summary>
        /// <param name="normal">The source vector.</param><param name="matrix">The transformation Matrix.</param><param name="result">[OutAttribute] The Vector3 resulting from the transformation.</param>
        public static void TransformNormal(ref Vector3I normal, ref Matrix matrix, out Vector3I result)
        {
            int num1 = (int)(normal.X * (int)Math.Round(matrix.M11) + normal.Y * (int)Math.Round(matrix.M21) + normal.Z * (int)Math.Round(matrix.M31));
            int num2 = (int)(normal.X * (int)Math.Round(matrix.M12) + normal.Y * (int)Math.Round(matrix.M22) + normal.Z * (int)Math.Round(matrix.M32));
            int num3 = (int)(normal.X * (int)Math.Round(matrix.M13) + normal.Y * (int)Math.Round(matrix.M23) + normal.Z * (int)Math.Round(matrix.M33));
            result.X = num1;
            result.Y = num2;
            result.Z = num3;
        }

        public static void TransformNormal(ref Vector3I normal, ref MatrixI matrix, out Vector3I result)
        {
            result = normal.X * Base6Directions.GetIntVector(matrix.Right) +
                     normal.Y * Base6Directions.GetIntVector(matrix.Up) +
                     normal.Z * Base6Directions.GetIntVector(matrix.Backward);
        }

        /// <summary>
        /// Calculates the cross product of two vectors.
        /// </summary>
        /// <param name="vector1">Source vector.</param><param name="vector2">Source vector.</param><param name="result">[OutAttribute] The cross product of the vectors.</param>
        public static void Cross(ref Vector3I vector1, ref Vector3I vector2, out Vector3I result)
        {
            int num1 = (vector1.Y * vector2.Z - vector1.Z * vector2.Y);
            int num2 = (vector1.Z * vector2.X - vector1.X * vector2.Z);
            int num3 = (vector1.X * vector2.Y - vector1.Y * vector2.X);
            result.X = num1;
            result.Y = num2;
            result.Z = num3;
        }

        /// <summary>
        /// How many cubes are in block with this size
        /// </summary>
        /// <returns></returns>
        public int Size
        {
            get
            {
                return Math.Abs(X * Y * Z);
            }
        }

        public int CompareTo(Vector3I other)
        {
            int x = X - other.X;
            int y = Y - other.Y;
            int z = Z - other.Z;
            return x != 0 ? x : (y != 0 ? y : z);
        }

        public static Vector3I Abs(Vector3I value)
        {
            return new Vector3I(Math.Abs(value.X), Math.Abs(value.Y), Math.Abs(value.Z));
        }

        public static void Abs(ref Vector3I value, out Vector3I result)
        {
            result.X = Math.Abs(value.X);
            result.Y = Math.Abs(value.Y);
            result.Z = Math.Abs(value.Z);
        }

        public static Vector3I Clamp(Vector3I value1, Vector3I min, Vector3I max)
        {
            Vector3I result;
            Clamp(ref value1, ref min, ref max, out result);
            return result;
        }

        public static void Clamp(ref Vector3I value1, ref Vector3I min, ref Vector3I max, out Vector3I result)
        {
            int num1 = value1.X;
            int num2 = num1 > max.X ? max.X : num1;
            result.X = num2 < min.X ? min.X : num2;

            int num4 = value1.Y;
            int num5 = num4 > max.Y ? max.Y : num4;
            result.Y = num5 < min.Y ? min.Y : num5;

            int num7 = value1.Z;
            int num8 = num7 > max.Z ? max.Z : num7;
            result.Z = num8 < min.Z ? min.Z : num8;
        }

        /// <summary>
        /// Manhattan distance (cube distance)
        /// X + Y + Z of Abs(first - second)
        /// </summary>
        public static int DistanceManhattan(Vector3I first, Vector3I second)
        {
            var dist = Vector3I.Abs(first - second);
            return dist.X + dist.Y + dist.Z;
        }

        public int Dot(ref Vector3I v)
        {
            return (X * v.X + Y * v.Y + Z * v.Z);
        }

        public static int Dot(Vector3I vector1, Vector3I vector2)
        {
            return Dot(ref vector1, ref vector2);
        }

        public static int Dot(ref Vector3I vector1, ref Vector3I vector2)
        {
            return (vector1.X * vector2.X + vector1.Y * vector2.Y + vector1.Z * vector2.Z);
        }

        public static void Dot(ref Vector3I vector1, ref Vector3I vector2, out int dot)
        {
            dot = (vector1.X * vector2.X + vector1.Y * vector2.Y + vector1.Z * vector2.Z);
        }

        public static bool TryParseFromString(string p, out Vector3I vec)
        {
            var vals = p.Split(';');

            if (vals.Length != 3)
            {
                Debug.Fail("Bad serialized vector");
                vec = Vector3I.Zero;
                return false;
            }

            try{
                vec.X = Int32.Parse(vals[0]);
                vec.Y = Int32.Parse(vals[1]);
                vec.Z = Int32.Parse(vals[2]);
            }
            catch (FormatException e)
            {
                Debug.Fail(e.Message);
                vec = Vector3I.Zero;
                return false;
            }

            return true;
        }

        public int Volume()
        {
            return X*Y*Z;
        }
    }
}
