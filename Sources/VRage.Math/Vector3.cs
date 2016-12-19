// Disabled, some people have issues with this, but latest redist installed and SharpDX works
//#define NATIVE_SUPPORT

using System;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Security;
#if XB1 // XB1_SYNC_SERIALIZER_NOEMIT
using System.Reflection;
using VRage.Reflection;
#endif // XB1

namespace VRageMath
{
    /// <summary>
    /// Defines a vector with three components.
    /// </summary>
    [ProtoBuf.ProtoContract, Serializable]
    [Unsharper.UnsharperDisableReflection()]
#if !XB1 // XB1_SYNC_SERIALIZER_NOEMIT
    public struct Vector3 : IEquatable<Vector3>
#else // XB1
    public struct Vector3 : IEquatable<Vector3>, IMySetGetMemberDataHelper
#endif // XB1
    {
        public static Vector3 Zero = new Vector3();
        public static Vector3 One = new Vector3(1f, 1f, 1f);
        public static Vector3 MinusOne = new Vector3(-1f, -1f, -1f);
        public static Vector3 Half = new Vector3(0.5f, 0.5f, 0.5f);
        public static Vector3 PositiveInfinity = new Vector3(float.PositiveInfinity);
        public static Vector3 NegativeInfinity = new Vector3(float.NegativeInfinity);
        public static Vector3 UnitX = new Vector3(1f, 0.0f, 0.0f);
        public static Vector3 UnitY = new Vector3(0.0f, 1f, 0.0f);
        public static Vector3 UnitZ = new Vector3(0.0f, 0.0f, 1f);
        public static Vector3 Up = new Vector3(0.0f, 1f, 0.0f);
        public static Vector3 Down = new Vector3(0.0f, -1f, 0.0f);
        public static Vector3 Right = new Vector3(1f, 0.0f, 0.0f);
        public static Vector3 Left = new Vector3(-1f, 0.0f, 0.0f);
        public static Vector3 Forward = new Vector3(0.0f, 0.0f, -1f);
        public static Vector3 Backward = new Vector3(0.0f, 0.0f, 1f);
        public static Vector3 MaxValue = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
        public static Vector3 MinValue = new Vector3(float.MinValue, float.MinValue, float.MinValue);
        public static Vector3 Invalid = new Vector3(float.NaN);
        /// <summary>
        /// Gets or sets the x-component of the vector.
        /// </summary>
        [ProtoBuf.ProtoMember]
        public float X;
        /// <summary>
        /// Gets or sets the y-component of the vector.
        /// </summary>
        [ProtoBuf.ProtoMember]
        public float Y;
        /// <summary>
        /// Gets or sets the z-component of the vector.
        /// </summary>
        [ProtoBuf.ProtoMember]
        public float Z;


        static Vector3()
        {
        }

        /// <summary>
        /// Initializes a new instance of Vector3.
        /// </summary>
        /// <param name="x">Initial value for the x-component of the vector.</param><param name="y">Initial value for the y-component of the vector.</param><param name="z">Initial value for the z-component of the vector.</param>
        public Vector3(float x, float y, float z)
        {
            this.X = x;
            this.Y = y;
            this.Z = z;
        }

        public Vector3(double x, double y, double z)
        {
            this.X = (float)x;
            this.Y = (float)y;
            this.Z = (float)z;
        }

        /// <summary>
        /// Creates a new instance of Vector3.
        /// </summary>
        /// <param name="value">Value to initialize each component to.</param>
        public Vector3(float value)
        {
            this.X = this.Y = this.Z = value;
        }

        /// <summary>
        /// Initializes a new instance of Vector3.
        /// </summary>
        /// <param name="value">A vector containing the values to initialize x and y components with.</param><param name="z">Initial value for the z-component of the vector.</param>
        public Vector3(Vector2 value, float z)
        {
            this.X = value.X;
            this.Y = value.Y;
            this.Z = z;
        }

        public Vector3(Vector4 xyz)
        {
            this.X = xyz.X;
            this.Y = xyz.Y;
            this.Z = xyz.Z;
        }

        public Vector3(ref Vector3I value)
        {
            X = value.X;
            Y = value.Y;
            Z = value.Z;
        }

        public Vector3(Vector3I value)
        {
            X = value.X;
            Y = value.Y;
            Z = value.Z;
        }

        /// <summary>
        /// Returns a vector pointing in the opposite direction.
        /// </summary>
        /// <param name="value">Source vector.</param>
        public static Vector3 operator -(Vector3 value)
        {
            Vector3 vector3;
            vector3.X = -value.X;
            vector3.Y = -value.Y;
            vector3.Z = -value.Z;
            return vector3;
        }

        /// <summary>
        /// Tests vectors for equality.
        /// </summary>
        /// <param name="value1">Source vector.</param><param name="value2">Source vector.</param>
        public static bool operator ==(Vector3 value1, Vector3 value2)
        {
            if ((double)value1.X == (double)value2.X && (double)value1.Y == (double)value2.Y)
                return (double)value1.Z == (double)value2.Z;
            else
                return false;
        }

        /// <summary>
        /// Tests vectors for inequality.
        /// </summary>
        /// <param name="value1">Vector to compare.</param><param name="value2">Vector to compare.</param>
        public static bool operator !=(Vector3 value1, Vector3 value2)
        {
            if ((double)value1.X == (double)value2.X && (double)value1.Y == (double)value2.Y)
                return (double)value1.Z != (double)value2.Z;
            else
                return true;
        }

        /// <summary>
        /// Adds two vectors.
        /// </summary>
        /// <param name="value1">Source vector.</param><param name="value2">Source vector.</param>
        public static Vector3 operator +(Vector3 value1, Vector3 value2)
        {
            Vector3 vector3;
            vector3.X = value1.X + value2.X;
            vector3.Y = value1.Y + value2.Y;
            vector3.Z = value1.Z + value2.Z;
            return vector3;
        }

        public static Vector3 operator +(Vector3 value1, float value2)
        {
            Vector3 vector3;
            vector3.X = value1.X + value2;
            vector3.Y = value1.Y + value2;
            vector3.Z = value1.Z + value2;
            return vector3;
        }

        /// <summary>
        /// Subtracts a vector from a vector.
        /// </summary>
        /// <param name="value1">Source vector.</param><param name="value2">Source vector.</param>
        public static Vector3 operator -(Vector3 value1, Vector3 value2)
        {
            Vector3 vector3;
            vector3.X = value1.X - value2.X;
            vector3.Y = value1.Y - value2.Y;
            vector3.Z = value1.Z - value2.Z;
            return vector3;
        }

        public static Vector3 operator -(Vector3 value1, float value2)
        {
            Vector3 vector3;
            vector3.X = value1.X - value2;
            vector3.Y = value1.Y - value2;
            vector3.Z = value1.Z - value2;
            return vector3;
        }

        /// <summary>
        /// Multiplies the components of two vectors by each other.
        /// </summary>
        /// <param name="value1">Source vector.</param><param name="value2">Source vector.</param>
        public static Vector3 operator *(Vector3 value1, Vector3 value2)
        {
            Vector3 vector3;
            vector3.X = value1.X * value2.X;
            vector3.Y = value1.Y * value2.Y;
            vector3.Z = value1.Z * value2.Z;
            return vector3;
        }

        /// <summary>
        /// Multiplies a vector by a scalar value.
        /// </summary>
        /// <param name="value">Source vector.</param><param name="scaleFactor">Scalar value.</param>
        public static Vector3 operator *(Vector3 value, float scaleFactor)
        {
            Vector3 vector3;
            vector3.X = value.X * scaleFactor;
            vector3.Y = value.Y * scaleFactor;
            vector3.Z = value.Z * scaleFactor;
            return vector3;
        }

        /// <summary>
        /// Multiplies a vector by a scalar value.
        /// </summary>
        /// <param name="scaleFactor">Scalar value.</param><param name="value">Source vector.</param>
        public static Vector3 operator *(float scaleFactor, Vector3 value)
        {
            Vector3 vector3;
            vector3.X = value.X * scaleFactor;
            vector3.Y = value.Y * scaleFactor;
            vector3.Z = value.Z * scaleFactor;
            return vector3;
        }

        /// <summary>
        /// Divides the components of a vector by the components of another vector.
        /// </summary>
        /// <param name="value1">Source vector.</param><param name="value2">Divisor vector.</param>
        public static Vector3 operator /(Vector3 value1, Vector3 value2)
        {
            Vector3 vector3;
            vector3.X = value1.X / value2.X;
            vector3.Y = value1.Y / value2.Y;
            vector3.Z = value1.Z / value2.Z;
            return vector3;
        }

        /// <summary>
        /// Divides a vector by a scalar value.
        /// </summary>
        /// <param name="value">Source vector.</param><param name="divider">The divisor.</param>
        public static Vector3 operator /(Vector3 value, float divider)
        {
            float num = 1f / divider;
            Vector3 vector3;
            vector3.X = value.X * num;
            vector3.Y = value.Y * num;
            vector3.Z = value.Z * num;
            return vector3;
        }

        public static Vector3 operator /(float value, Vector3 divider)
        {
            Vector3 vector3;
            vector3.X = value / divider.X;
            vector3.Y = value / divider.Y;
            vector3.Z = value / divider.Z;
            return vector3;
        }

        public void Divide(float divider)
        {
            float num = 1f / divider;
            X *= num;
            Y *= num;
            Z *= num;
        }

        public void Multiply(float scale)
        {
            X *= scale;
            Y *= scale;
            Z *= scale;
        }

        public void Add(Vector3 other)
        {
            X += other.X;
            Y += other.Y;
            Z += other.Z;
        }

        public static Vector3 Abs(Vector3 value)
        {
            return new Vector3(Math.Abs(value.X), Math.Abs(value.Y), Math.Abs(value.Z));
        }

        public static Vector3 Sign(Vector3 value)
        {
            return new Vector3(Math.Sign(value.X), Math.Sign(value.Y), Math.Sign(value.Z));
        }

        /// <summary>
        /// Returns per component sign, never returns zero.
        /// For zero component returns sign 1.
        /// Faster than Sign.
        /// </summary>
        public static Vector3 SignNonZero(Vector3 value)
        {
            return new Vector3(value.X < 0 ? -1 : 1, value.Y < 0 ? -1 : 1, value.Z < 0 ? -1 : 1);
        }

        public void Interpolate3(Vector3 v0, Vector3 v1, float rt)
        {
            float s = 1.0f - rt;
            X = s * v0.X + rt * v1.X;
            Y = s * v0.Y + rt * v1.Y;
            Z = s * v0.Z + rt * v1.Z;
        }

        public bool IsValid()
        {
            // We can multiply, when one component is infinity, others will be too. When one is NaN, others will be too.
            return (X * Y * Z).IsValid();
        }

        [Conditional("DEBUG")]
        public void AssertIsValid()
        {
            Debug.Assert(IsValid());
        }

        public static bool IsUnit(ref Vector3 value)
        {
            var length = value.LengthSquared();
            return length >= 0.9999f && length < 1.0001f;
        }

        public static bool ArePerpendicular(ref Vector3 a, ref Vector3 b)
        {
            float dot = a.Dot(b);
            // We want to calculate |D_normalized| < Epsilon, which is equivalent to: D^2 < Epsilon^2 * l1^2 * l2^2
            return dot * dot < 0.00000001f * a.LengthSquared() * b.LengthSquared();
        }

        // Per component IsZero, returns 1 for each component which equals to 0
        public static bool IsZero(Vector3 value)
        {
            return IsZero(value, 0.0001f);
        }

        // Per component IsZero, returns 1 for each component which equals to 0
        public static bool IsZero(Vector3 value, float epsilon)
        {
            return Math.Abs(value.X) < epsilon && Math.Abs(value.Y) < epsilon && Math.Abs(value.Z) < epsilon;
        }

        // Per component IsZero, returns 1 for each component which equals to 0
        public static Vector3 IsZeroVector(Vector3 value)
        {
            return new Vector3(value.X == 0 ? 1 : 0, value.Y == 0 ? 1 : 0, value.Z == 0 ? 1 : 0);
        }

        // Per component IsZero, returns 1 for each component which equals to 0
        public static Vector3 IsZeroVector(Vector3 value, float epsilon)
        {
            return new Vector3(Math.Abs(value.X) < epsilon ? 1 : 0, Math.Abs(value.Y) < epsilon ? 1 : 0, Math.Abs(value.Z) < epsilon ? 1 : 0);
        }

         // Per component Step (returns 0, 1 or -1 for each component)
        public static Vector3 Step(Vector3 value)
        {
            return new Vector3(value.X > 0 ? 1 : value.X < 0 ? -1 : 0, value.Y > 0 ? 1 : value.Y < 0 ? -1 : 0, value.Z > 0 ? 1 : value.Z < 0 ? -1 : 0);
        }
        
        /// <summary>
        /// Retrieves a string representation of the current object.
        /// </summary>
        public override string ToString()
        {
            CultureInfo currentCulture = CultureInfo.CurrentCulture;
            return string.Format((IFormatProvider)currentCulture, "{{X:{0} Y:{1} Z:{2}}}", (object)this.X.ToString((IFormatProvider)currentCulture), (object)this.Y.ToString((IFormatProvider)currentCulture), (object)this.Z.ToString((IFormatProvider)currentCulture));
        }

        public string ToString(string format)
        {
            CultureInfo currentCulture = CultureInfo.CurrentCulture;
            return string.Format((IFormatProvider)currentCulture, "{{X:{0} Y:{1} Z:{2}}}", (object)this.X.ToString(format, (IFormatProvider)currentCulture), (object)this.Y.ToString(format, (IFormatProvider)currentCulture), (object)this.Z.ToString(format, (IFormatProvider)currentCulture));
        }

        /// <summary>
        /// Determines whether the specified Object is equal to the Vector3.
        /// </summary>
        /// <param name="other">The Vector3 to compare with the current Vector3.</param>
        public bool Equals(Vector3 other)
        {
            if (this.X == (double)other.X && (double)this.Y == (double)other.Y)
                return (double)this.Z == (double)other.Z;
            else
                return false;
        }

        public bool Equals(Vector3 other,float epsilon)
        {
            return Math.Abs(this.X - other.X) < epsilon && Math.Abs(this.Y - other.Y) < epsilon && Math.Abs(this.Z - other.Z) < epsilon;
        }

        /// <summary>
        /// Returns a value that indicates whether the current instance is equal to a specified object.
        /// </summary>
        /// <param name="obj">Object to make the comparison with.</param>
        public override bool Equals(object obj)
        {
            bool flag = false;
            if (obj is Vector3)
                flag = this.Equals((Vector3)obj);
            return flag;
        }

        public override int GetHashCode()
        {
            int hash = (int)(X * 997);
            hash = (hash * 397) ^ (int)(Y * 997);
            hash = (hash * 397) ^ (int)(Z * 997);
            return hash;
        }

        /// <summary>
        /// Gets the hash code of the vector object.
        /// </summary>
        public long GetHash()
        {
            long result = 1;
            int modCode = 0;

            result = (long)Math.Round(Math.Abs(X * 1000));
            modCode = (1 << 1);

            result = (result * 397) ^ (long)Math.Round(Math.Abs(Y * 1000));
            modCode += (1 << 2);

            result = (result * 397) ^ (long)Math.Round(Math.Abs(Z * 1000));
            modCode += (1 << 4);

            result = (result * 397) ^ (long)(Math.Sign(X) + 5);
            modCode += (1 << 8);

            result = (result * 397) ^ (long)(Math.Sign(Y) + 7);
            modCode += (1 << 16);

            result = (result * 397) ^ (long)(Math.Sign(Z) + 11);
            modCode += (1 << 32);

            result = (result * 397) ^ modCode;

            return result;
        }

        /// <summary>
        /// Calculates the length of the vector.
        /// </summary>
        public float Length()
        {
            return (float)Math.Sqrt((double)this.X * (double)this.X + (double)this.Y * (double)this.Y + (double)this.Z * (double)this.Z);
        }

        /// <summary>
        /// Calculates the length of the vector squared.
        /// </summary>
        public float LengthSquared()
        {
            return (float)((double)this.X * (double)this.X + (double)this.Y * (double)this.Y + (double)this.Z * (double)this.Z);
        }

        /// <summary>
        /// Calculates the distance between two vectors.
        /// </summary>
        /// <param name="value1">Source vector.</param><param name="value2">Source vector.</param>
        public static float Distance(Vector3 value1, Vector3 value2)
        {
            float num1 = value1.X - value2.X;
            float num2 = value1.Y - value2.Y;
            float num3 = value1.Z - value2.Z;
            return (float)Math.Sqrt((double)num1 * (double)num1 + (double)num2 * (double)num2 + (double)num3 * (double)num3);
        }

        /// <summary>
        /// Calculates the distance between two vectors.
        /// </summary>
        /// <param name="value1">Source vector.</param><param name="value2">Source vector.</param><param name="result">[OutAttribute] The distance between the vectors.</param>
        public static void Distance(ref Vector3 value1, ref Vector3 value2, out float result)
        {
            float num1 = value1.X - value2.X;
            float num2 = value1.Y - value2.Y;
            float num3 = value1.Z - value2.Z;
            float num4 = (float)((double)num1 * (double)num1 + (double)num2 * (double)num2 + (double)num3 * (double)num3);
            result = (float)Math.Sqrt((double)num4);
        }

        /// <summary>
        /// Calculates the distance between two vectors squared.
        /// </summary>
        /// <param name="value1">Source vector.</param><param name="value2">Source vector.</param>
        public static float DistanceSquared(Vector3 value1, Vector3 value2)
        {
            float num1 = value1.X - value2.X;
            float num2 = value1.Y - value2.Y;
            float num3 = value1.Z - value2.Z;
            return (float)((double)num1 * (double)num1 + (double)num2 * (double)num2 + (double)num3 * (double)num3);
        }

        /// <summary>
        /// Calculates the distance between two vectors squared.
        /// </summary>
        /// <param name="value1">Source vector.</param><param name="value2">Source vector.</param><param name="result">[OutAttribute] The distance between the two vectors squared.</param>
        public static void DistanceSquared(ref Vector3 value1, ref Vector3 value2, out float result)
        {
            float num1 = value1.X - value2.X;
            float num2 = value1.Y - value2.Y;
            float num3 = value1.Z - value2.Z;
            result = (float)((double)num1 * (double)num1 + (double)num2 * (double)num2 + (double)num3 * (double)num3);
        }

        /// <summary>
        /// Calculates rectangular distance (a.k.a. Manhattan distance or Chessboard distace) between two vectors.
        /// </summary>
        /// <param name="value1">Source vector.</param><param name="value2">Source vector.</param>
        public static float RectangularDistance(Vector3 value1, Vector3 value2)
        {
            Vector3 dv = Vector3.Abs(value1 - value2);
            return dv.X + dv.Y + dv.Z;
        }

        /// <summary>
        /// Calculates rectangular distance (a.k.a. Manhattan distance or Chessboard distace) between two vectors.
        /// </summary>
        /// <param name="value1">Source vector.</param><param name="value2">Source vector.</param>
        public static float RectangularDistance(ref Vector3 value1, ref Vector3 value2)
        {
            Vector3 dv = Vector3.Abs(value1 - value2);
            return dv.X + dv.Y + dv.Z;
        }

        /// <summary>
        /// Calculates the dot product of two vectors. If the two vectors are unit vectors, the dot product returns a floating point value between -1 and 1 that can be used to determine some properties of the angle between two vectors. For example, it can show whether the vectors are orthogonal, parallel, or have an acute or obtuse angle between them.
        /// </summary>
        /// <param name="vector1">Source vector.</param><param name="vector2">Source vector.</param>
        public static float Dot(Vector3 vector1, Vector3 vector2)
        {
            return (float)((double)vector1.X * (double)vector2.X + (double)vector1.Y * (double)vector2.Y + (double)vector1.Z * (double)vector2.Z);
        }

        /// <summary>
        /// Calculates the dot product of two vectors and writes the result to a user-specified variable. If the two vectors are unit vectors, the dot product returns a floating point value between -1 and 1 that can be used to determine some properties of the angle between two vectors. For example, it can show whether the vectors are orthogonal, parallel, or have an acute or obtuse angle between them.
        /// </summary>
        /// <param name="vector1">Source vector.</param><param name="vector2">Source vector.</param><param name="result">[OutAttribute] The dot product of the two vectors.</param>
        public static void Dot(ref Vector3 vector1, ref Vector3 vector2, out float result)
        {
            result = (float)((double)vector1.X * (double)vector2.X + (double)vector1.Y * (double)vector2.Y + (double)vector1.Z * (double)vector2.Z);
        }

        public float Dot(Vector3 v)
        {
            return Vector3.Dot(this, v);
        }

        public float Dot(ref Vector3 v)
        {
            return X * v.X + Y * v.Y + Z * v.Z;
        }

        public Vector3 Cross(Vector3 v)
        {
            return Vector3.Cross(this, v);
        }

        /// <summary>
        /// Turns the current vector into a unit vector. The result is a vector one unit in length pointing in the same direction as the original vector.
        /// </summary>
        /// returns length
        public float Normalize()
        {
            float length = (float)Math.Sqrt((double)this.X * (double)this.X + (double)this.Y * (double)this.Y + (double)this.Z * (double)this.Z);
            float num = 1f / length;
            this.X *= num;
            this.Y *= num;
            this.Z *= num;
            return length;
        }

        /// <summary>
        /// Creates a unit vector from the specified vector. The result is a vector one unit in length pointing in the same direction as the original vector.
        /// </summary>
        /// <param name="value">The source Vector3.</param>
        public static Vector3 Normalize(Vector3 value)
        {
            float num = 1f / (float)Math.Sqrt((double)value.X * (double)value.X + (double)value.Y * (double)value.Y + (double)value.Z * (double)value.Z);
            Vector3 vector3;
            vector3.X = value.X * num;
            vector3.Y = value.Y * num;
            vector3.Z = value.Z * num;
            return vector3;
        }

        public static Vector3 Normalize(Vector3D value)
        {
            float num = 1f / (float)Math.Sqrt((double)value.X * (double)value.X + (double)value.Y * (double)value.Y + (double)value.Z * (double)value.Z);
            Vector3 vector3;
            vector3.X = (float)value.X * num;
            vector3.Y = (float)value.Y * num;
            vector3.Z = (float)value.Z * num;
            return vector3;
        }

        public static bool GetNormalized(ref Vector3 value)
        {
            float length = (float)Math.Sqrt((double)value.X * (double)value.X + (double)value.Y * (double)value.Y + (double)value.Z * (double)value.Z);
            if (length > 0.001f)
            {
                float num = 1f / length;
                Vector3 vector3;
                vector3.X = (float)value.X * num;
                vector3.Y = (float)value.Y * num;
                vector3.Z = (float)value.Z * num;
                return true;
            }

            return false;
        }


        /// <summary>
        /// Creates a unit vector from the specified vector, writing the result to a user-specified variable. The result is a vector one unit in length pointing in the same direction as the original vector.
        /// </summary>
        /// <param name="value">Source vector.</param><param name="result">[OutAttribute] The normalized vector.</param>
        public static void Normalize(ref Vector3 value, out Vector3 result)
        {
            float num = 1f / (float)Math.Sqrt((double)value.X * (double)value.X + (double)value.Y * (double)value.Y + (double)value.Z * (double)value.Z);
            result.X = value.X * num;
            result.Y = value.Y * num;
            result.Z = value.Z * num;
        }

        /// <summary>
        /// Calculates the cross product of two vectors.
        /// </summary>
        /// <param name="vector1">Source vector.</param><param name="vector2">Source vector.</param>
        public static Vector3 Cross(Vector3 vector1, Vector3 vector2)
        {
            Vector3 vector3;
            vector3.X = (float)((double)vector1.Y * (double)vector2.Z - (double)vector1.Z * (double)vector2.Y);
            vector3.Y = (float)((double)vector1.Z * (double)vector2.X - (double)vector1.X * (double)vector2.Z);
            vector3.Z = (float)((double)vector1.X * (double)vector2.Y - (double)vector1.Y * (double)vector2.X);
            return vector3;
        }

        /// <summary>
        /// Calculates the cross product of two vectors.
        /// </summary>
        /// <param name="vector1">Source vector.</param><param name="vector2">Source vector.</param><param name="result">[OutAttribute] The cross product of the vectors.</param>
        public static void Cross(ref Vector3 vector1, ref Vector3 vector2, out Vector3 result)
        {
            float num1 = (float)((double)vector1.Y * (double)vector2.Z - (double)vector1.Z * (double)vector2.Y);
            float num2 = (float)((double)vector1.Z * (double)vector2.X - (double)vector1.X * (double)vector2.Z);
            float num3 = (float)((double)vector1.X * (double)vector2.Y - (double)vector1.Y * (double)vector2.X);
            result.X = num1;
            result.Y = num2;
            result.Z = num3;
        }

        /// <summary>
        /// Returns the reflection of a vector off a surface that has the specified normal.  Reference page contains code sample.
        /// </summary>
        /// <param name="vector">Source vector.</param><param name="normal">Normal of the surface.</param>
        public static Vector3 Reflect(Vector3 vector, Vector3 normal)
        {
            float num = (float)((double)vector.X * (double)normal.X + (double)vector.Y * (double)normal.Y + (double)vector.Z * (double)normal.Z);
            Vector3 vector3;
            vector3.X = vector.X - 2f * num * normal.X;
            vector3.Y = vector.Y - 2f * num * normal.Y;
            vector3.Z = vector.Z - 2f * num * normal.Z;
            return vector3;
        }

        /// <summary>
        /// Returns the reflection of a vector off a surface that has the specified normal.  Reference page contains code sample.
        /// </summary>
        /// <param name="vector">Source vector.</param><param name="normal">Normal of the surface.</param><param name="result">[OutAttribute] The reflected vector.</param>
        public static void Reflect(ref Vector3 vector, ref Vector3 normal, out Vector3 result)
        {
            float num = (float)((double)vector.X * (double)normal.X + (double)vector.Y * (double)normal.Y + (double)vector.Z * (double)normal.Z);
            result.X = vector.X - 2f * num * normal.X;
            result.Y = vector.Y - 2f * num * normal.Y;
            result.Z = vector.Z - 2f * num * normal.Z;
        }

        /// <summary>
        /// Returns the rejection of vector from direction, i.e. projection of vector onto the plane defined by origin and direction
        /// </summary>
        /// <param name="vector">Vector which is to be rejected</param>
        /// <param name="direction">Direction from which the input vector will be rejected</param>
        /// <returns>Rejection of the vector from the given direction</returns>
        public static Vector3 Reject(Vector3 vector, Vector3 direction)
        {
            Vector3 result;
            Reject(ref vector, ref direction, out result);
            return result;
        }

        /// <summary>
        /// Returns the rejection of vector from direction, i.e. projection of vector onto the plane defined by origin and direction
        /// </summary>
        /// <param name="vector">Vector which is to be rejected</param>
        /// <param name="direction">Direction from which the input vector will be rejected</param>
        /// <param name="result">Rejection of the vector from the given direction</param>
        public static void Reject(ref Vector3 vector, ref Vector3 direction, out Vector3 result)
        {
            //  Optimized: float inv_denom = 1.0f / Vector3.Dot(normal, normal);
            float invDenom;
            Vector3.Dot(ref direction, ref direction, out invDenom);
            invDenom = 1.0f / invDenom;

            //  Optimized: float d = Vector3.Dot(normal, p) * inv_denom;
            float d;
            Vector3.Dot(ref direction, ref vector, out d);
            d = d * invDenom;

            //  Optimized: Vector3 n = normal * inv_denom;
            Vector3 n;
            n.X = direction.X * invDenom;
            n.Y = direction.Y * invDenom;
            n.Z = direction.Z * invDenom;

            //  Optimized: return p - d * n;
            result.X = vector.X - d * n.X;
            result.Y = vector.Y - d * n.Y;
            result.Z = vector.Z - d * n.Z;
        }

        /// <summary>
        /// Returns the component of the vector that is smallest of all the three components.
        /// </summary>
        public float Min()
        {
            if (X < Y)
            {
                if (X < Z) return X;
                else return Z;
            }
            else
            {
                if (Y < Z) return Y;
                else return Z;
            }
        }

        /// <summary>
        /// Returns the component of the vector, whose absolute value is smallest of all the three components.
        /// </summary>
        public float AbsMin()
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

        /// <summary>
        /// Returns the component of the vector that is largest of all the three components.
        /// </summary>
        public float Max()
        {
            if (X > Y)
            {
                if (X > Z) return X;
                else return Z;
            }
            else
            {
                if (Y > Z) return Y;
                else return Z;
            }
        }

        /// <summary>
        /// Keeps only component with maximal absolute, others are set to zero.
        /// </summary>
        public Vector3 MaxAbsComponent()
        {
            var result = this;
            var abs = Vector3.Abs(result);
            float max = abs.Max();
            if (abs.X != max)
                result.X = 0;
            if (abs.Y != max)
                result.Y = 0;
            if (abs.Z != max)
                result.Z = 0;
            return result;
        }

        /// <summary>
        /// Returns the component of the vector, whose absolute value is largest of all the three components.
        /// </summary>
        public float AbsMax()
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

        /// <summary>
        /// Returns a vector that contains the lowest value from each matching pair of components.
        /// </summary>
        /// <param name="value1">Source vector.</param><param name="value2">Source vector.</param>
        public static Vector3 Min(Vector3 value1, Vector3 value2)
        {
            Vector3 vector3;
            vector3.X = (double)value1.X < (double)value2.X ? value1.X : value2.X;
            vector3.Y = (double)value1.Y < (double)value2.Y ? value1.Y : value2.Y;
            vector3.Z = (double)value1.Z < (double)value2.Z ? value1.Z : value2.Z;
            return vector3;
        }

        /// <summary>
        /// Returns a vector that contains the lowest value from each matching pair of components.
        /// </summary>
        /// <param name="value1">Source vector.</param><param name="value2">Source vector.</param><param name="result">[OutAttribute] The minimized vector.</param>
        public static void Min(ref Vector3 value1, ref Vector3 value2, out Vector3 result)
        {
            result.X = (double)value1.X < (double)value2.X ? value1.X : value2.X;
            result.Y = (double)value1.Y < (double)value2.Y ? value1.Y : value2.Y;
            result.Z = (double)value1.Z < (double)value2.Z ? value1.Z : value2.Z;
        }

        /// <summary>
        /// Returns a vector that contains the highest value from each matching pair of components.
        /// </summary>
        /// <param name="value1">Source vector.</param><param name="value2">Source vector.</param>
        public static Vector3 Max(Vector3 value1, Vector3 value2)
        {
            Vector3 vector3;
            vector3.X = (double)value1.X > (double)value2.X ? value1.X : value2.X;
            vector3.Y = (double)value1.Y > (double)value2.Y ? value1.Y : value2.Y;
            vector3.Z = (double)value1.Z > (double)value2.Z ? value1.Z : value2.Z;
            return vector3;
        }

        /// <summary>
        /// Returns a vector that contains the highest value from each matching pair of components.
        /// </summary>
        /// <param name="value1">Source vector.</param><param name="value2">Source vector.</param><param name="result">[OutAttribute] The maximized vector.</param>
        public static void Max(ref Vector3 value1, ref Vector3 value2, out Vector3 result)
        {
            result.X = (double)value1.X > (double)value2.X ? value1.X : value2.X;
            result.Y = (double)value1.Y > (double)value2.Y ? value1.Y : value2.Y;
            result.Z = (double)value1.Z > (double)value2.Z ? value1.Z : value2.Z;
        }

        /// <summary>
        /// Separates minimal and maximal values of any two input vectors
        /// </summary>
        /// <param name="min">minimal values of the two vectors</param>
        /// <param name="max">maximal values of the two vectors</param>
        public static void MinMax(ref Vector3 min, ref Vector3 max)
        {
            float tmp;
            if (min.X > max.X)
            {
                tmp = min.X;
                min.X = max.X;
                max.X = tmp;
            }
            if (min.Y > max.Y)
            {
                tmp = min.Y;
                min.Y = max.Y;
                max.Y = tmp;
            }
            if (min.Z > max.Z)
            {
                tmp = min.Z;
                min.Z = max.Z;
                max.Z = tmp;
            }
        }

        /// <summary>
        /// Returns a vector that is equal to the projection of the input vector to the coordinate axis that corresponds
        /// to the original vector's largest value.
        /// </summary>
        /// <param name="value1">Source vector.</param>
        public static Vector3 DominantAxisProjection(Vector3 value1)
        {
            Vector3 res;

            DominantAxisProjection(ref value1, out res);

            return res;
        }

        /// <summary>
        /// Calculates a vector that is equal to the projection of the input vector to the coordinate axis that corresponds
        /// to the original vector's largest value. The result is saved into a user-specified variable.
        /// </summary>
        /// <param name="value1">Source vector.</param><param name="result">[OutAttribute] The projected vector.</param>
        public static void DominantAxisProjection(ref Vector3 value1, out Vector3 result)
        {
            if (Math.Abs(value1.X) > Math.Abs(value1.Y))
            {
                if (Math.Abs(value1.X) > Math.Abs(value1.Z))
                    result = new Vector3(value1.X, 0.0f, 0.0f);
                else
                    result = new Vector3(0.0f, 0.0f, value1.Z);
            }
            else
            {
                if (Math.Abs(value1.Y) > Math.Abs(value1.Z))
                    result = new Vector3(0.0f, value1.Y, 0.0f);
                else
                    result = new Vector3(0.0f, 0.0f, value1.Z);
            }


            const float threshold = 0.0001f;
            Debug.Assert(result.Length() > threshold, "Input must be not be zero vector.");
        }

        /// <summary>
        /// Restricts a value to be within a specified range.
        /// </summary>
        /// <param name="value1">The value to clamp.</param><param name="min">The minimum value.</param><param name="max">The maximum value.</param>
        public static Vector3 Clamp(Vector3 value1, Vector3 min, Vector3 max)
        {
            float num1 = value1.X;
            float num2 = (double)num1 > (double)max.X ? max.X : num1;
            float num3 = (double)num2 < (double)min.X ? min.X : num2;
            float num4 = value1.Y;
            float num5 = (double)num4 > (double)max.Y ? max.Y : num4;
            float num6 = (double)num5 < (double)min.Y ? min.Y : num5;
            float num7 = value1.Z;
            float num8 = (double)num7 > (double)max.Z ? max.Z : num7;
            float num9 = (double)num8 < (double)min.Z ? min.Z : num8;
            Vector3 vector3;
            vector3.X = num3;
            vector3.Y = num6;
            vector3.Z = num9;
            return vector3;
        }

        /// <summary>
        /// Restricts a value to be within a specified range.
        /// </summary>
        /// <param name="value1">The value to clamp.</param><param name="min">The minimum value.</param><param name="max">The maximum value.</param><param name="result">[OutAttribute] The clamped value.</param>
        public static void Clamp(ref Vector3 value1, ref Vector3 min, ref Vector3 max, out Vector3 result)
        {
            float num1 = value1.X;
            float num2 = (double)num1 > (double)max.X ? max.X : num1;
            float num3 = (double)num2 < (double)min.X ? min.X : num2;
            float num4 = value1.Y;
            float num5 = (double)num4 > (double)max.Y ? max.Y : num4;
            float num6 = (double)num5 < (double)min.Y ? min.Y : num5;
            float num7 = value1.Z;
            float num8 = (double)num7 > (double)max.Z ? max.Z : num7;
            float num9 = (double)num8 < (double)min.Z ? min.Z : num8;
            result.X = num3;
            result.Y = num6;
            result.Z = num9;
        }

        public static Vector3 ClampToSphere(Vector3 vector, float radius)
        {
            float lsq = vector.LengthSquared();
            float rsq = radius * radius;
            if (lsq > rsq)
            {
                return vector * (float)Math.Sqrt(rsq / lsq);
            }
            return vector;
        }

        public static void ClampToSphere(ref Vector3 vector, float radius)
        {
            float lsq = vector.LengthSquared();
            float rsq = radius * radius;
            if (lsq > rsq)
            {
                vector *= (float)Math.Sqrt(rsq / lsq);
            }
        }

        /// <summary>
        /// Performs a linear interpolation between two vectors.
        /// </summary>
        /// <param name="value1">Source vector.</param><param name="value2">Source vector.</param><param name="amount">Value between 0 and 1 indicating the weight of value2.</param>
        public static Vector3 Lerp(Vector3 value1, Vector3 value2, float amount)
        {
            Vector3 vector3;
            vector3.X = value1.X + (value2.X - value1.X) * amount;
            vector3.Y = value1.Y + (value2.Y - value1.Y) * amount;
            vector3.Z = value1.Z + (value2.Z - value1.Z) * amount;
            return vector3;
        }

        /// <summary>
        /// Performs a linear interpolation between two vectors.
        /// </summary>
        /// <param name="value1">Source vector.</param><param name="value2">Source vector.</param><param name="amount">Value between 0 and 1 indicating the weight of value2.</param><param name="result">[OutAttribute] The result of the interpolation.</param>
        public static void Lerp(ref Vector3 value1, ref Vector3 value2, float amount, out Vector3 result)
        {
            result.X = value1.X + (value2.X - value1.X) * amount;
            result.Y = value1.Y + (value2.Y - value1.Y) * amount;
            result.Z = value1.Z + (value2.Z - value1.Z) * amount;
        }

        /// <summary>
        /// Returns a Vector3 containing the 3D Cartesian coordinates of a point specified in Barycentric coordinates relative to a 3D triangle.
        /// </summary>
        /// <param name="value1">A Vector3 containing the 3D Cartesian coordinates of vertex 1 of the triangle.</param><param name="value2">A Vector3 containing the 3D Cartesian coordinates of vertex 2 of the triangle.</param><param name="value3">A Vector3 containing the 3D Cartesian coordinates of vertex 3 of the triangle.</param><param name="amount1">Barycentric coordinate b2, which expresses the weighting factor toward vertex 2 (specified in value2).</param><param name="amount2">Barycentric coordinate b3, which expresses the weighting factor toward vertex 3 (specified in value3).</param>
        public static Vector3 Barycentric(Vector3 value1, Vector3 value2, Vector3 value3, float amount1, float amount2)
        {
            Vector3 vector3;
            vector3.X = (float)((double)value1.X + (double)amount1 * ((double)value2.X - (double)value1.X) + (double)amount2 * ((double)value3.X - (double)value1.X));
            vector3.Y = (float)((double)value1.Y + (double)amount1 * ((double)value2.Y - (double)value1.Y) + (double)amount2 * ((double)value3.Y - (double)value1.Y));
            vector3.Z = (float)((double)value1.Z + (double)amount1 * ((double)value2.Z - (double)value1.Z) + (double)amount2 * ((double)value3.Z - (double)value1.Z));
            return vector3;
        }

        /// <summary>
        /// Returns a Vector3 containing the 3D Cartesian coordinates of a point specified in barycentric (areal) coordinates relative to a 3D triangle.
        /// </summary>
        /// <param name="value1">A Vector3 containing the 3D Cartesian coordinates of vertex 1 of the triangle.</param><param name="value2">A Vector3 containing the 3D Cartesian coordinates of vertex 2 of the triangle.</param><param name="value3">A Vector3 containing the 3D Cartesian coordinates of vertex 3 of the triangle.</param><param name="amount1">Barycentric coordinate b2, which expresses the weighting factor toward vertex 2 (specified in value2).</param><param name="amount2">Barycentric coordinate b3, which expresses the weighting factor toward vertex 3 (specified in value3).</param><param name="result">[OutAttribute] The 3D Cartesian coordinates of the specified point are placed in this Vector3 on exit.</param>
        public static void Barycentric(ref Vector3 value1, ref Vector3 value2, ref Vector3 value3, float amount1, float amount2, out Vector3 result)
        {
            result.X = (float)((double)value1.X + (double)amount1 * ((double)value2.X - (double)value1.X) + (double)amount2 * ((double)value3.X - (double)value1.X));
            result.Y = (float)((double)value1.Y + (double)amount1 * ((double)value2.Y - (double)value1.Y) + (double)amount2 * ((double)value3.Y - (double)value1.Y));
            result.Z = (float)((double)value1.Z + (double)amount1 * ((double)value2.Z - (double)value1.Z) + (double)amount2 * ((double)value3.Z - (double)value1.Z));
        }

        /// <summary>
        /// Compute barycentric coordinates (u, v, w) for point p with respect to triangle (a, b, c)
        /// From : Real-Time Collision Detection, Christer Ericson, CRC Press
        /// 3.4 Barycentric Coordinates
        /// </summary>
        public static void Barycentric(Vector3 p, Vector3 a, Vector3 b, Vector3 c, out float u, out float v, out float w)
        {
            Vector3 v0 = b - a, v1 = c - a, v2 = p - a;
            float d00 = Dot(v0, v0);
            float d01 = Dot(v0, v1);
            float d11 = Dot(v1, v1);
            float d20 = Dot(v2, v0);
            float d21 = Dot(v2, v1);
            float denom = d00 * d11 - d01 * d01;
            v = (d11 * d20 - d01 * d21) / denom;
            w = (d00 * d21 - d01 * d20) / denom;
            u = 1.0f - v - w;
        }

        /// <summary>
        /// Interpolates between two values using a cubic equation.
        /// </summary>
        /// <param name="value1">Source value.</param><param name="value2">Source value.</param><param name="amount">Weighting value.</param>
        public static Vector3 SmoothStep(Vector3 value1, Vector3 value2, float amount)
        {
            amount = (double)amount > 1.0 ? 1f : ((double)amount < 0.0 ? 0.0f : amount);
            amount = (float)((double)amount * (double)amount * (3.0 - 2.0 * (double)amount));
            Vector3 vector3;
            vector3.X = value1.X + (value2.X - value1.X) * amount;
            vector3.Y = value1.Y + (value2.Y - value1.Y) * amount;
            vector3.Z = value1.Z + (value2.Z - value1.Z) * amount;
            return vector3;
        }

        /// <summary>
        /// Interpolates between two values using a cubic equation.
        /// </summary>
        /// <param name="value1">Source vector.</param><param name="value2">Source vector.</param><param name="amount">Weighting value.</param><param name="result">[OutAttribute] The interpolated value.</param>
        public static void SmoothStep(ref Vector3 value1, ref Vector3 value2, float amount, out Vector3 result)
        {
            amount = (double)amount > 1.0 ? 1f : ((double)amount < 0.0 ? 0.0f : amount);
            amount = (float)((double)amount * (double)amount * (3.0 - 2.0 * (double)amount));
            result.X = value1.X + (value2.X - value1.X) * amount;
            result.Y = value1.Y + (value2.Y - value1.Y) * amount;
            result.Z = value1.Z + (value2.Z - value1.Z) * amount;
        }

        /// <summary>
        /// Performs a Catmull-Rom interpolation using the specified positions.
        /// </summary>
        /// <param name="value1">The first position in the interpolation.</param><param name="value2">The second position in the interpolation.</param><param name="value3">The third position in the interpolation.</param><param name="value4">The fourth position in the interpolation.</param><param name="amount">Weighting factor.</param>
        public static Vector3 CatmullRom(Vector3 value1, Vector3 value2, Vector3 value3, Vector3 value4, float amount)
        {
            float num1 = amount * amount;
            float num2 = amount * num1;
            Vector3 vector3;
            vector3.X = (float)(0.5 * (2.0 * (double)value2.X + (-(double)value1.X + (double)value3.X) * (double)amount + (2.0 * (double)value1.X - 5.0 * (double)value2.X + 4.0 * (double)value3.X - (double)value4.X) * (double)num1 + (-(double)value1.X + 3.0 * (double)value2.X - 3.0 * (double)value3.X + (double)value4.X) * (double)num2));
            vector3.Y = (float)(0.5 * (2.0 * (double)value2.Y + (-(double)value1.Y + (double)value3.Y) * (double)amount + (2.0 * (double)value1.Y - 5.0 * (double)value2.Y + 4.0 * (double)value3.Y - (double)value4.Y) * (double)num1 + (-(double)value1.Y + 3.0 * (double)value2.Y - 3.0 * (double)value3.Y + (double)value4.Y) * (double)num2));
            vector3.Z = (float)(0.5 * (2.0 * (double)value2.Z + (-(double)value1.Z + (double)value3.Z) * (double)amount + (2.0 * (double)value1.Z - 5.0 * (double)value2.Z + 4.0 * (double)value3.Z - (double)value4.Z) * (double)num1 + (-(double)value1.Z + 3.0 * (double)value2.Z - 3.0 * (double)value3.Z + (double)value4.Z) * (double)num2));
            return vector3;
        }

        /// <summary>
        /// Performs a Catmull-Rom interpolation using the specified positions.
        /// </summary>
        /// <param name="value1">The first position in the interpolation.</param><param name="value2">The second position in the interpolation.</param><param name="value3">The third position in the interpolation.</param><param name="value4">The fourth position in the interpolation.</param><param name="amount">Weighting factor.</param><param name="result">[OutAttribute] A vector that is the result of the Catmull-Rom interpolation.</param>
        public static void CatmullRom(ref Vector3 value1, ref Vector3 value2, ref Vector3 value3, ref Vector3 value4, float amount, out Vector3 result)
        {
            float num1 = amount * amount;
            float num2 = amount * num1;
            result.X = (float)(0.5 * (2.0 * (double)value2.X + (-(double)value1.X + (double)value3.X) * (double)amount + (2.0 * (double)value1.X - 5.0 * (double)value2.X + 4.0 * (double)value3.X - (double)value4.X) * (double)num1 + (-(double)value1.X + 3.0 * (double)value2.X - 3.0 * (double)value3.X + (double)value4.X) * (double)num2));
            result.Y = (float)(0.5 * (2.0 * (double)value2.Y + (-(double)value1.Y + (double)value3.Y) * (double)amount + (2.0 * (double)value1.Y - 5.0 * (double)value2.Y + 4.0 * (double)value3.Y - (double)value4.Y) * (double)num1 + (-(double)value1.Y + 3.0 * (double)value2.Y - 3.0 * (double)value3.Y + (double)value4.Y) * (double)num2));
            result.Z = (float)(0.5 * (2.0 * (double)value2.Z + (-(double)value1.Z + (double)value3.Z) * (double)amount + (2.0 * (double)value1.Z - 5.0 * (double)value2.Z + 4.0 * (double)value3.Z - (double)value4.Z) * (double)num1 + (-(double)value1.Z + 3.0 * (double)value2.Z - 3.0 * (double)value3.Z + (double)value4.Z) * (double)num2));
        }

        /// <summary>
        /// Performs a Hermite spline interpolation.
        /// </summary>
        /// <param name="value1">Source position vector.</param><param name="tangent1">Source tangent vector.</param><param name="value2">Source position vector.</param><param name="tangent2">Source tangent vector.</param><param name="amount">Weighting factor.</param>
        public static Vector3 Hermite(Vector3 value1, Vector3 tangent1, Vector3 value2, Vector3 tangent2, float amount)
        {
            float num1 = amount * amount;
            float num2 = amount * num1;
            float num3 = (float)(2.0 * (double)num2 - 3.0 * (double)num1 + 1.0);
            float num4 = (float)(-2.0 * (double)num2 + 3.0 * (double)num1);
            float num5 = num2 - 2f * num1 + amount;
            float num6 = num2 - num1;
            Vector3 vector3;
            vector3.X = (float)((double)value1.X * (double)num3 + (double)value2.X * (double)num4 + (double)tangent1.X * (double)num5 + (double)tangent2.X * (double)num6);
            vector3.Y = (float)((double)value1.Y * (double)num3 + (double)value2.Y * (double)num4 + (double)tangent1.Y * (double)num5 + (double)tangent2.Y * (double)num6);
            vector3.Z = (float)((double)value1.Z * (double)num3 + (double)value2.Z * (double)num4 + (double)tangent1.Z * (double)num5 + (double)tangent2.Z * (double)num6);
            return vector3;
        }

        /// <summary>
        /// Performs a Hermite spline interpolation.
        /// </summary>
        /// <param name="value1">Source position vector.</param><param name="tangent1">Source tangent vector.</param><param name="value2">Source position vector.</param><param name="tangent2">Source tangent vector.</param><param name="amount">Weighting factor.</param><param name="result">[OutAttribute] The result of the Hermite spline interpolation.</param>
        public static void Hermite(ref Vector3 value1, ref Vector3 tangent1, ref Vector3 value2, ref Vector3 tangent2, float amount, out Vector3 result)
        {
            float num1 = amount * amount;
            float num2 = amount * num1;
            float num3 = (float)(2.0 * (double)num2 - 3.0 * (double)num1 + 1.0);
            float num4 = (float)(-2.0 * (double)num2 + 3.0 * (double)num1);
            float num5 = num2 - 2f * num1 + amount;
            float num6 = num2 - num1;
            result.X = (float)((double)value1.X * (double)num3 + (double)value2.X * (double)num4 + (double)tangent1.X * (double)num5 + (double)tangent2.X * (double)num6);
            result.Y = (float)((double)value1.Y * (double)num3 + (double)value2.Y * (double)num4 + (double)tangent1.Y * (double)num5 + (double)tangent2.Y * (double)num6);
            result.Z = (float)((double)value1.Z * (double)num3 + (double)value2.Z * (double)num4 + (double)tangent1.Z * (double)num5 + (double)tangent2.Z * (double)num6);
        }

        /// <summary>
        /// Transforms a 3D vector by the given matrix.
        /// </summary>
        /// <param name="position">The source vector.</param><param name="matrix">The transformation matrix.</param>
        public static Vector3 Transform(Vector3 position, Matrix matrix)
        {
#if NATIVE_SUPPORT
            Vector3 vector3;
            Transform_Native(ref position, ref matrix, out vector3);
            return vector3;
#else
            float num1 = (float)((double)position.X * (double)matrix.M11 + (double)position.Y * (double)matrix.M21 + (double)position.Z * (double)matrix.M31) + matrix.M41;
            float num2 = (float)((double)position.X * (double)matrix.M12 + (double)position.Y * (double)matrix.M22 + (double)position.Z * (double)matrix.M32) + matrix.M42;
            float num3 = (float)((double)position.X * (double)matrix.M13 + (double)position.Y * (double)matrix.M23 + (double)position.Z * (double)matrix.M33) + matrix.M43;
            float num4 = 1f / ((((position.X * matrix.M14) + (position.Y * matrix.M24)) + (position.Z * matrix.M34)) + matrix.M44);
            Vector3 vector3;
            vector3.X = num1 * num4;
            vector3.Y = num2 * num4;
            vector3.Z = num3 * num4;
            return vector3;
#endif
        }

        /// <summary>
        /// Transforms a 3D vector by the given matrix.
        /// </summary>
        /// <param name="position">The source vector.</param><param name="matrix">The transformation matrix.</param>
        public static Vector3D Transform(Vector3 position, MatrixD matrix)
        {
            double num1 = position.X * matrix.M11 + position.Y * matrix.M21 + position.Z * matrix.M31 + matrix.M41;
            double num2 = position.X * matrix.M12 + position.Y * matrix.M22 + position.Z * matrix.M32 + matrix.M42;
            double num3 = position.X * matrix.M13 + position.Y * matrix.M23 + position.Z * matrix.M33 + matrix.M43;
            double num4 = 1 / ((((position.X * matrix.M14) + (position.Y * matrix.M24)) + (position.Z * matrix.M34)) + matrix.M44);
            Vector3D vector3;
            vector3.X = num1 * num4;
            vector3.Y = num2 * num4;
            vector3.Z = num3 * num4;
            return vector3;
        }


        public static Vector3 Transform(Vector3 position, ref Matrix matrix)
        {
            Transform(ref position, ref matrix, out position);
            return position;
        }

        /// <summary>
        /// Transforms a Vector3 by the given Matrix.
        /// </summary>
        /// <param name="position">The source Vector3.</param><param name="matrix">The transformation Matrix.</param><param name="result">[OutAttribute] The transformed vector.</param>
        public static void Transform(ref Vector3 position, ref Matrix matrix, out Vector3 result)
        {
#if NATIVE_SUPPORT
            Transform_Native(ref position, ref matrix, out result);
#else
            float num1 = (float)((double)position.X * (double)matrix.M11 + (double)position.Y * (double)matrix.M21 + (double)position.Z * (double)matrix.M31) + matrix.M41;
            float num2 = (float)((double)position.X * (double)matrix.M12 + (double)position.Y * (double)matrix.M22 + (double)position.Z * (double)matrix.M32) + matrix.M42;
            float num3 = (float)((double)position.X * (double)matrix.M13 + (double)position.Y * (double)matrix.M23 + (double)position.Z * (double)matrix.M33) + matrix.M43;
            float num4 = 1f / ((((position.X * matrix.M14) + (position.Y * matrix.M24)) + (position.Z * matrix.M34)) + matrix.M44);
            result.X = num1 * num4;
            result.Y = num2 * num4;
            result.Z = num3 * num4;
#endif
        }

        public static void Transform(ref Vector3 position, ref MatrixI matrix, out Vector3 result)
        {
            result = position.X * Base6Directions.GetVector(matrix.Right) +
                     position.Y * Base6Directions.GetVector(matrix.Up) +
                     position.Z * Base6Directions.GetVector(matrix.Backward) +
                     matrix.Translation;
        }

        /**
         * Transform the provided vector only about the rotation, scale and translation terms of a matrix.
         * 
         * This effectively treats the matrix as a 3x4 matrix and the input vector as a 4 dimensional vector with unit W coordinate.
         */
        public static void TransformNoProjection(ref Vector3 vector, ref Matrix matrix, out Vector3 result)
        {
            float x = (vector.X * matrix.M11 + vector.Y * matrix.M21 + vector.Z * matrix.M31) + matrix.M41;
            float y = (vector.X * matrix.M12 + vector.Y * matrix.M22 + vector.Z * matrix.M32) + matrix.M42;
            float z = (vector.X * matrix.M13 + vector.Y * matrix.M23 + vector.Z * matrix.M33) + matrix.M43;

            result.X = x;
            result.Y = y;
            result.Z = z;
        }

        /**
         * Transform the provided vector only about the rotation and scale terms of a matrix.
         */
        public static void RotateAndScale(ref Vector3 vector, ref Matrix matrix, out Vector3 result)
        {
            float x = (vector.X * matrix.M11 + vector.Y * matrix.M21 + vector.Z * matrix.M31);
            float y = (vector.X * matrix.M12 + vector.Y * matrix.M22 + vector.Z * matrix.M32);
            float z = (vector.X * matrix.M13 + vector.Y * matrix.M23 + vector.Z * matrix.M33);

            result.X = x;
            result.Y = y;
            result.Z = z;
        }

        // Transform (x, y, z, 1) by matrix, project result back into w=1.
        //D3DXVECTOR3* WINAPI D3DXVec3TransformCoord
        //  ( D3DXVECTOR3 *pOut, CONST D3DXVECTOR3 *pV, CONST D3DXMATRIX *pM );

#if NATIVE_SUPPORT
        /// <summary>Native Interop Function</summary>
        [DllImport("d3dx9_43.dll", EntryPoint = "D3DXVec3TransformCoord", CallingConvention = CallingConvention.StdCall, PreserveSig = true), SuppressUnmanagedCodeSecurityAttribute]
        private unsafe extern static Vector3* D3DXVec3TransformCoord_([Out] Vector3* pOut, [In] Vector3* pV,[In] Matrix* pM);

        public static void Transform_Native(ref Vector3 position, ref Matrix matrix, out Vector3 result)
        {
            unsafe
            {
                fixed (Vector3* resultRef_ = &result)
                fixed (Vector3* posRef_ = &position)
                fixed (Matrix* matRef_ = &matrix)

                    D3DXVec3TransformCoord_(resultRef_, posRef_, matRef_);
            }
        }
#endif



        /// <summary>
        /// Transforms a 3D vector normal by a matrix.
        /// </summary>
        /// <param name="normal">The source vector.</param><param name="matrix">The transformation matrix.</param>
        public static Vector3 TransformNormal(Vector3 normal, Matrix matrix)
        {
            float num1 = (float)((double)normal.X * (double)matrix.M11 + (double)normal.Y * (double)matrix.M21 + (double)normal.Z * (double)matrix.M31);
            float num2 = (float)((double)normal.X * (double)matrix.M12 + (double)normal.Y * (double)matrix.M22 + (double)normal.Z * (double)matrix.M32);
            float num3 = (float)((double)normal.X * (double)matrix.M13 + (double)normal.Y * (double)matrix.M23 + (double)normal.Z * (double)matrix.M33);
            Vector3 vector3;
            vector3.X = num1;
            vector3.Y = num2;
            vector3.Z = num3;
            return vector3;
        }

        /// <summary>
        /// Transforms a 3D vector normal by a matrix.
        /// </summary>
        /// <param name="normal">The source vector.</param><param name="matrix">The transformation matrix.</param>
        public static Vector3 TransformNormal(Vector3 normal, MatrixD matrix)
        {
            float num1 = (float)((double)normal.X * (double)matrix.M11 + (double)normal.Y * (double)matrix.M21 + (double)normal.Z * (double)matrix.M31);
            float num2 = (float)((double)normal.X * (double)matrix.M12 + (double)normal.Y * (double)matrix.M22 + (double)normal.Z * (double)matrix.M32);
            float num3 = (float)((double)normal.X * (double)matrix.M13 + (double)normal.Y * (double)matrix.M23 + (double)normal.Z * (double)matrix.M33);
            Vector3 vector3;
            vector3.X = num1;
            vector3.Y = num2;
            vector3.Z = num3;
            return vector3;
        }

        /// <summary>
        /// Transforms a 3D vector normal by a matrix.
        /// </summary>
        /// <param name="normal">The source vector.</param><param name="matrix">The transformation matrix.</param>
        public static Vector3 TransformNormal(Vector3D normal, Matrix matrix)
        {
            float num1 = (float)((double)normal.X * (double)matrix.M11 + (double)normal.Y * (double)matrix.M21 + (double)normal.Z * (double)matrix.M31);
            float num2 = (float)((double)normal.X * (double)matrix.M12 + (double)normal.Y * (double)matrix.M22 + (double)normal.Z * (double)matrix.M32);
            float num3 = (float)((double)normal.X * (double)matrix.M13 + (double)normal.Y * (double)matrix.M23 + (double)normal.Z * (double)matrix.M33);
            Vector3 vector3;
            vector3.X = num1;
            vector3.Y = num2;
            vector3.Z = num3;
            return vector3;
        }

        /// <summary>
        /// Transforms a vector normal by a matrix.
        /// </summary>
        /// <param name="normal">The source vector.</param><param name="matrix">The transformation Matrix.</param><param name="result">[OutAttribute] The Vector3 resulting from the transformation.</param>
        public static void TransformNormal(ref Vector3 normal, ref Matrix matrix, out Vector3 result)
        {
            float num1 = (float)((double)normal.X * (double)matrix.M11 + (double)normal.Y * (double)matrix.M21 + (double)normal.Z * (double)matrix.M31);
            float num2 = (float)((double)normal.X * (double)matrix.M12 + (double)normal.Y * (double)matrix.M22 + (double)normal.Z * (double)matrix.M32);
            float num3 = (float)((double)normal.X * (double)matrix.M13 + (double)normal.Y * (double)matrix.M23 + (double)normal.Z * (double)matrix.M33);
            result.X = num1;
            result.Y = num2;
            result.Z = num3;
        }
        public static void TransformNormal(ref Vector3 normal, ref MatrixD matrix, out Vector3 result)
        {
            float num1 = (float)((double)normal.X * (double)matrix.M11 + (double)normal.Y * (double)matrix.M21 + (double)normal.Z * (double)matrix.M31);
            float num2 = (float)((double)normal.X * (double)matrix.M12 + (double)normal.Y * (double)matrix.M22 + (double)normal.Z * (double)matrix.M32);
            float num3 = (float)((double)normal.X * (double)matrix.M13 + (double)normal.Y * (double)matrix.M23 + (double)normal.Z * (double)matrix.M33);
            result.X = num1;
            result.Y = num2;
            result.Z = num3;
        }

        public static void TransformNormal(ref Vector3 normal, ref MatrixI matrix, out Vector3 result)
        {
            result = normal.X * Base6Directions.GetVector(matrix.Right) +
                     normal.Y * Base6Directions.GetVector(matrix.Up) +
                     normal.Z * Base6Directions.GetVector(matrix.Backward);
        }

        public static Vector3 TransformNormal(Vector3 normal, MyBlockOrientation orientation)
        {
            Vector3 retval;
            TransformNormal(ref normal, orientation, out retval);
            return retval;
        }

        public static void TransformNormal(ref Vector3 normal, MyBlockOrientation orientation, out Vector3 result)
        {
            result = - normal.X * Base6Directions.GetVector(orientation.Left)
                     + normal.Y * Base6Directions.GetVector(orientation.Up)
                     - normal.Z * Base6Directions.GetVector(orientation.Forward);
        }

        public static Vector3 TransformNormal(Vector3 normal, ref Matrix matrix)
        {
            TransformNormal(ref normal, ref matrix, out normal);
            return normal;
        }

        /// <summary>
        /// Transforms a Vector3 by a specified Quaternion rotation.
        /// </summary>
        /// <param name="value">The Vector3 to rotate.</param><param name="rotation">The Quaternion rotation to apply.</param>
        public static Vector3 Transform(Vector3 value, Quaternion rotation)
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
            Vector3 vector3;
            vector3.X = num13;
            vector3.Y = num14;
            vector3.Z = num15;
            return vector3;
        }

        /// <summary>
        /// Transforms a Vector3 by a specified Quaternion rotation.
        /// </summary>
        /// <param name="value">The Vector3 to rotate.</param><param name="rotation">The Quaternion rotation to apply.</param><param name="result">[OutAttribute] An existing Vector3 filled in with the results of the rotation.</param>
        public static void Transform(ref Vector3 value, ref Quaternion rotation, out Vector3 result)
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
            result.X = num13;
            result.Y = num14;
            result.Z = num15;
        }

        /// <summary>
        /// Transforms a source array of Vector3s by a specified Matrix and writes the results to an existing destination array.
        /// </summary>
        /// <param name="sourceArray">The source array.</param><param name="matrix">The transform Matrix to apply.</param><param name="destinationArray">An existing destination array into which the transformed Vector3s are written.</param>
        public static void Transform(Vector3[] sourceArray, ref Matrix matrix, Vector3[] destinationArray)
        {
            for (int index = 0; index < sourceArray.Length; ++index)
            {
                float num1 = sourceArray[index].X;
                float num2 = sourceArray[index].Y;
                float num3 = sourceArray[index].Z;
                destinationArray[index].X = (float)((double)num1 * (double)matrix.M11 + (double)num2 * (double)matrix.M21 + (double)num3 * (double)matrix.M31) + matrix.M41;
                destinationArray[index].Y = (float)((double)num1 * (double)matrix.M12 + (double)num2 * (double)matrix.M22 + (double)num3 * (double)matrix.M32) + matrix.M42;
                destinationArray[index].Z = (float)((double)num1 * (double)matrix.M13 + (double)num2 * (double)matrix.M23 + (double)num3 * (double)matrix.M33) + matrix.M43;
            }
        }

        /// <summary>
        /// Applies a specified transform Matrix to a specified range of an array of Vector3s and writes the results into a specified range of a destination array.
        /// </summary>
        /// <param name="sourceArray">The source array.</param><param name="sourceIndex">The index in the source array at which to start.</param><param name="matrix">The transform Matrix to apply.</param><param name="destinationArray">The existing destination array.</param><param name="destinationIndex">The index in the destination array at which to start.</param><param name="length">The number of Vector3s to transform.</param>
        public static void Transform(Vector3[] sourceArray, int sourceIndex, ref Matrix matrix, Vector3[] destinationArray, int destinationIndex, int length)
        {
            for (; length > 0; --length)
            {
                float num1 = sourceArray[sourceIndex].X;
                float num2 = sourceArray[sourceIndex].Y;
                float num3 = sourceArray[sourceIndex].Z;
                destinationArray[destinationIndex].X = (float)((double)num1 * (double)matrix.M11 + (double)num2 * (double)matrix.M21 + (double)num3 * (double)matrix.M31) + matrix.M41;
                destinationArray[destinationIndex].Y = (float)((double)num1 * (double)matrix.M12 + (double)num2 * (double)matrix.M22 + (double)num3 * (double)matrix.M32) + matrix.M42;
                destinationArray[destinationIndex].Z = (float)((double)num1 * (double)matrix.M13 + (double)num2 * (double)matrix.M23 + (double)num3 * (double)matrix.M33) + matrix.M43;
                ++sourceIndex;
                ++destinationIndex;
            }
        }

        /// <summary>
        /// Transforms an array of 3D vector normals by a specified Matrix.
        /// </summary>
        /// <param name="sourceArray">The array of Vector3 normals to transform.</param><param name="matrix">The transform matrix to apply.</param><param name="destinationArray">An existing Vector3 array into which the results of the transforms are written.</param>
        public static void TransformNormal(Vector3[] sourceArray, ref Matrix matrix, Vector3[] destinationArray)
        {
            for (int index = 0; index < sourceArray.Length; ++index)
            {
                float num1 = sourceArray[index].X;
                float num2 = sourceArray[index].Y;
                float num3 = sourceArray[index].Z;
                destinationArray[index].X = (float)((double)num1 * (double)matrix.M11 + (double)num2 * (double)matrix.M21 + (double)num3 * (double)matrix.M31);
                destinationArray[index].Y = (float)((double)num1 * (double)matrix.M12 + (double)num2 * (double)matrix.M22 + (double)num3 * (double)matrix.M32);
                destinationArray[index].Z = (float)((double)num1 * (double)matrix.M13 + (double)num2 * (double)matrix.M23 + (double)num3 * (double)matrix.M33);
            }
        }

        /// <summary>
        /// Transforms a specified range in an array of 3D vector normals by a specified Matrix and writes the results to a specified range in a destination array.
        /// </summary>
        /// <param name="sourceArray">The source array of Vector3 normals.</param><param name="sourceIndex">The starting index in the source array.</param><param name="matrix">The transform Matrix to apply.</param><param name="destinationArray">The destination Vector3 array.</param><param name="destinationIndex">The starting index in the destination array.</param><param name="length">The number of vectors to transform.</param>
        public static void TransformNormal(Vector3[] sourceArray, int sourceIndex, ref Matrix matrix, Vector3[] destinationArray, int destinationIndex, int length)
        {
            for (; length > 0; --length)
            {
                float num1 = sourceArray[sourceIndex].X;
                float num2 = sourceArray[sourceIndex].Y;
                float num3 = sourceArray[sourceIndex].Z;
                destinationArray[destinationIndex].X = (float)((double)num1 * (double)matrix.M11 + (double)num2 * (double)matrix.M21 + (double)num3 * (double)matrix.M31);
                destinationArray[destinationIndex].Y = (float)((double)num1 * (double)matrix.M12 + (double)num2 * (double)matrix.M22 + (double)num3 * (double)matrix.M32);
                destinationArray[destinationIndex].Z = (float)((double)num1 * (double)matrix.M13 + (double)num2 * (double)matrix.M23 + (double)num3 * (double)matrix.M33);
                ++sourceIndex;
                ++destinationIndex;
            }
        }

        /// <summary>
        /// Transforms a source array of Vector3s by a specified Quaternion rotation and writes the results to an existing destination array.
        /// </summary>
        /// <param name="sourceArray">The source array.</param><param name="rotation">The Quaternion rotation to apply.</param><param name="destinationArray">An existing destination array into which the transformed Vector3s are written.</param>
        public static void Transform(Vector3[] sourceArray, ref Quaternion rotation, Vector3[] destinationArray)
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
            float num13 = 1f - num10 - num12;
            float num14 = num8 - num6;
            float num15 = num9 + num5;
            float num16 = num8 + num6;
            float num17 = 1f - num7 - num12;
            float num18 = num11 - num4;
            float num19 = num9 - num5;
            float num20 = num11 + num4;
            float num21 = 1f - num7 - num10;
            for (int index = 0; index < sourceArray.Length; ++index)
            {
                float num22 = sourceArray[index].X;
                float num23 = sourceArray[index].Y;
                float num24 = sourceArray[index].Z;
                destinationArray[index].X = (float)((double)num22 * (double)num13 + (double)num23 * (double)num14 + (double)num24 * (double)num15);
                destinationArray[index].Y = (float)((double)num22 * (double)num16 + (double)num23 * (double)num17 + (double)num24 * (double)num18);
                destinationArray[index].Z = (float)((double)num22 * (double)num19 + (double)num23 * (double)num20 + (double)num24 * (double)num21);
            }
        }

        /// <summary>
        /// Applies a specified Quaternion rotation to a specified range of an array of Vector3s and writes the results into a specified range of a destination array.
        /// </summary>
        /// <param name="sourceArray">The source array.</param><param name="sourceIndex">The index in the source array at which to start.</param><param name="rotation">The Quaternion rotation to apply.</param><param name="destinationArray">The existing destination array.</param><param name="destinationIndex">The index in the destination array at which to start.</param><param name="length">The number of Vector3s to transform.</param>
        public static void Transform(Vector3[] sourceArray, int sourceIndex, ref Quaternion rotation, Vector3[] destinationArray, int destinationIndex, int length)
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
            float num13 = 1f - num10 - num12;
            float num14 = num8 - num6;
            float num15 = num9 + num5;
            float num16 = num8 + num6;
            float num17 = 1f - num7 - num12;
            float num18 = num11 - num4;
            float num19 = num9 - num5;
            float num20 = num11 + num4;
            float num21 = 1f - num7 - num10;
            for (; length > 0; --length)
            {
                float num22 = sourceArray[sourceIndex].X;
                float num23 = sourceArray[sourceIndex].Y;
                float num24 = sourceArray[sourceIndex].Z;
                destinationArray[destinationIndex].X = (float)((double)num22 * (double)num13 + (double)num23 * (double)num14 + (double)num24 * (double)num15);
                destinationArray[destinationIndex].Y = (float)((double)num22 * (double)num16 + (double)num23 * (double)num17 + (double)num24 * (double)num18);
                destinationArray[destinationIndex].Z = (float)((double)num22 * (double)num19 + (double)num23 * (double)num20 + (double)num24 * (double)num21);
                ++sourceIndex;
                ++destinationIndex;
            }
        }

        /// <summary>
        /// Returns a vector pointing in the opposite direction.
        /// </summary>
        /// <param name="value">Source vector.</param>
        public static Vector3 Negate(Vector3 value)
        {
            Vector3 vector3;
            vector3.X = -value.X;
            vector3.Y = -value.Y;
            vector3.Z = -value.Z;
            return vector3;
        }

        /// <summary>
        /// Returns a vector pointing in the opposite direction.
        /// </summary>
        /// <param name="value">Source vector.</param><param name="result">[OutAttribute] Vector pointing in the opposite direction.</param>
        public static void Negate(ref Vector3 value, out Vector3 result)
        {
            result.X = -value.X;
            result.Y = -value.Y;
            result.Z = -value.Z;
        }

        /// <summary>
        /// Adds two vectors.
        /// </summary>
        /// <param name="value1">Source vector.</param><param name="value2">Source vector.</param>
        public static Vector3 Add(Vector3 value1, Vector3 value2)
        {
            Vector3 vector3;
            vector3.X = value1.X + value2.X;
            vector3.Y = value1.Y + value2.Y;
            vector3.Z = value1.Z + value2.Z;
            return vector3;
        }

        /// <summary>
        /// Adds two vectors.
        /// </summary>
        /// <param name="value1">Source vector.</param><param name="value2">Source vector.</param><param name="result">[OutAttribute] Sum of the source vectors.</param>
        public static void Add(ref Vector3 value1, ref Vector3 value2, out Vector3 result)
        {
            result.X = value1.X + value2.X;
            result.Y = value1.Y + value2.Y;
            result.Z = value1.Z + value2.Z;
        }

        /// <summary>
        /// Subtracts a vector from a vector.
        /// </summary>
        /// <param name="value1">Source vector.</param><param name="value2">Source vector.</param>
        public static Vector3 Subtract(Vector3 value1, Vector3 value2)
        {
            Vector3 vector3;
            vector3.X = value1.X - value2.X;
            vector3.Y = value1.Y - value2.Y;
            vector3.Z = value1.Z - value2.Z;
            return vector3;
        }

        /// <summary>
        /// Subtracts a vector from a vector.
        /// </summary>
        /// <param name="value1">Source vector.</param><param name="value2">Source vector.</param><param name="result">[OutAttribute] The result of the subtraction.</param>
        public static void Subtract(ref Vector3 value1, ref Vector3 value2, out Vector3 result)
        {
            result.X = value1.X - value2.X;
            result.Y = value1.Y - value2.Y;
            result.Z = value1.Z - value2.Z;
        }

        /// <summary>
        /// Multiplies the components of two vectors by each other.
        /// </summary>
        /// <param name="value1">Source vector.</param><param name="value2">Source vector.</param>
        public static Vector3 Multiply(Vector3 value1, Vector3 value2)
        {
            Vector3 vector3;
            vector3.X = value1.X * value2.X;
            vector3.Y = value1.Y * value2.Y;
            vector3.Z = value1.Z * value2.Z;
            return vector3;
        }

        /// <summary>
        /// Multiplies the components of two vectors by each other.
        /// </summary>
        /// <param name="value1">Source vector.</param><param name="value2">Source vector.</param><param name="result">[OutAttribute] The result of the multiplication.</param>
        public static void Multiply(ref Vector3 value1, ref Vector3 value2, out Vector3 result)
        {
            result.X = value1.X * value2.X;
            result.Y = value1.Y * value2.Y;
            result.Z = value1.Z * value2.Z;
        }

        /// <summary>
        /// Multiplies a vector by a scalar value.
        /// </summary>
        /// <param name="value1">Source vector.</param><param name="scaleFactor">Scalar value.</param>
        public static Vector3 Multiply(Vector3 value1, float scaleFactor)
        {
            Vector3 vector3;
            vector3.X = value1.X * scaleFactor;
            vector3.Y = value1.Y * scaleFactor;
            vector3.Z = value1.Z * scaleFactor;
            return vector3;
        }

        /// <summary>
        /// Multiplies a vector by a scalar value.
        /// </summary>
        /// <param name="value1">Source vector.</param><param name="scaleFactor">Scalar value.</param><param name="result">[OutAttribute] The result of the multiplication.</param>
        public static void Multiply(ref Vector3 value1, float scaleFactor, out Vector3 result)
        {
            result.X = value1.X * scaleFactor;
            result.Y = value1.Y * scaleFactor;
            result.Z = value1.Z * scaleFactor;
        }

        /// <summary>
        /// Divides the components of a vector by the components of another vector.
        /// </summary>
        /// <param name="value1">Source vector.</param><param name="value2">Divisor vector.</param>
        public static Vector3 Divide(Vector3 value1, Vector3 value2)
        {
            Vector3 vector3;
            vector3.X = value1.X / value2.X;
            vector3.Y = value1.Y / value2.Y;
            vector3.Z = value1.Z / value2.Z;
            return vector3;
        }

        /// <summary>
        /// Divides the components of a vector by the components of another vector.
        /// </summary>
        /// <param name="value1">Source vector.</param><param name="value2">The divisor.</param><param name="result">[OutAttribute] The result of the division.</param>
        public static void Divide(ref Vector3 value1, ref Vector3 value2, out Vector3 result)
        {
            result.X = value1.X / value2.X;
            result.Y = value1.Y / value2.Y;
            result.Z = value1.Z / value2.Z;
        }

        /// <summary>
        /// Divides a vector by a scalar value.
        /// </summary>
        /// <param name="value1">Source vector.</param><param name="value2">The divisor.</param>
        public static Vector3 Divide(Vector3 value1, float value2)
        {
            float num = 1f / value2;
            Vector3 vector3;
            vector3.X = value1.X * num;
            vector3.Y = value1.Y * num;
            vector3.Z = value1.Z * num;
            return vector3;
        }

        /// <summary>
        /// Divides a vector by a scalar value.
        /// </summary>
        /// <param name="value1">Source vector.</param><param name="value2">The divisor.</param><param name="result">[OutAttribute] The result of the division.</param>
        public static void Divide(ref Vector3 value1, float value2, out Vector3 result)
        {
            float num = 1f / value2;
            result.X = value1.X * num;
            result.Y = value1.Y * num;
            result.Z = value1.Z * num;
        }

        public static Vector3 CalculatePerpendicularVector(Vector3 v)
        {
            Vector3 res;
            v.CalculatePerpendicularVector(out res);
            return res;
        }

        public void CalculatePerpendicularVector(out Vector3 result)
        {
            const float threshold = 0.0001f;
            Debug.Assert(Math.Abs(1f - this.Length()) < threshold, "Input must be unit length vector.");
            if (Math.Abs(Y + Z) > threshold || Math.Abs(X) > threshold)
                result = new Vector3(-(Y + Z), X, X);
            else
                result = new Vector3(Z, Z, -(X + Y));
            Vector3.Normalize(ref result, out result);
            Debug.Assert(Math.Abs(result.Length() - this.Length()) < threshold);
            Debug.Assert(Math.Abs(Vector3.Dot(this, result)) < threshold);
            Debug.Assert(Math.Abs(Vector3.Dot(this, Vector3.Cross(result, this))) < threshold);
        }

        public static void GetAzimuthAndElevation(Vector3 v, out float azimuth, out float elevation)
        {
            float elevationSin, azimuthCos;
            Vector3.Dot(ref v, ref Vector3.Up, out elevationSin);
            v.Y = 0f;
            v.Normalize();
            Vector3.Dot(ref v, ref Vector3.Forward, out azimuthCos);
            elevation = (float)Math.Asin(elevationSin);
            if (v.X >= 0)
            {
                azimuth = -(float)Math.Acos(azimuthCos);
            }
            else
            {
                azimuth = (float)Math.Acos(azimuthCos);
            }
        }

        public static void CreateFromAzimuthAndElevation(float azimuth, float elevation, out Vector3 direction)
        {
            var yRot = Matrix.CreateRotationY(azimuth);
            var xRot = Matrix.CreateRotationX(elevation);
            direction = Vector3.Forward;
            Vector3.TransformNormal(ref direction, ref xRot, out direction);
            Vector3.TransformNormal(ref direction, ref yRot, out direction);
        }

        public float Sum
        {
            get
            {
                return X + Y + Z;
            }
        }

        public float Volume
        {
            get
            {
                return X * Y * Z;
            }
        }

        public long VolumeInt(float multiplier)
        {
            return (long)(X * multiplier) * (long)(Y * multiplier) * (long)(Z * multiplier);
        }

        public bool IsInsideInclusive(ref Vector3 min, ref Vector3 max)
        {
            return
                min.X <= this.X && this.X <= max.X &&
                min.Y <= this.Y && this.Y <= max.Y &&
                min.Z <= this.Z && this.Z <= max.Z;
        }

        public static Vector3 SwapYZCoordinates(Vector3 v)
        {
            return new Vector3(v.X, v.Z, -v.Y);
        }

        public float GetDim(int i)
        {
            switch (i)
            {
                case 0: return X;
                case 1: return Y;
                case 2: return Z;
                default: return GetDim((i % 3 + 3) % 3);  // reduce to 0..2
            }
        }
        public void SetDim(int i, float value)
        {
            switch (i)
            {
                case 0: X = value; break;
                case 1: Y = value; break;
                case 2: Z = value; break;
                default: SetDim((i % 3 + 3) % 3, value); break;  // reduce to 0..2
            }
        }

        public static Vector3 Ceiling(Vector3 v)
        {
            return new Vector3(Math.Ceiling(v.X), Math.Ceiling(v.Y), Math.Ceiling(v.Z));
        }

        public static Vector3 Floor(Vector3 v)
        {
            return new Vector3(Math.Floor(v.X), Math.Floor(v.Y), Math.Floor(v.Z));
        }

        public static Vector3 Round(Vector3 v)
        {
            return new Vector3(Math.Round(v.X), Math.Round(v.Y), Math.Round(v.Z));
        }

        public static Vector3 Round(Vector3 v,int numDecimals)
        {
            return new Vector3(Math.Round(v.X, numDecimals), Math.Round(v.Y, numDecimals), Math.Round(v.Z, numDecimals));
        }

#if XB1 // XB1_SYNC_SERIALIZER_NOEMIT
        public object GetMemberData(MemberInfo m)
        {
            if (m.Name == "X")
                return X;
            if (m.Name == "Y")
                return Y;
            if (m.Name == "Z")
                return Z;

            System.Diagnostics.Debug.Assert(false, "TODO for XB1.");
            return null;
        }
#endif // XB1
    }

    public static class NullableVector3Extensions
    {
        public static bool IsValid(this Vector3? value)
        {
            return !value.HasValue || value.Value.IsValid();
        }

        [Conditional("DEBUG")]
        public static void AssertIsValid(this Vector3? value)
        {
            Debug.Assert(value.IsValid());
        }
    }

}
