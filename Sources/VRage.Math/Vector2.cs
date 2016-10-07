using System;
using System.Diagnostics;
using System.Globalization;

namespace VRageMath
{
    /// <summary>
    /// Defines a vector with two components.
    /// </summary>
    [ProtoBuf.ProtoContract, Serializable]
    public struct Vector2 : IEquatable<Vector2>
    {
        public static Vector2 Zero  = new Vector2();
        public static Vector2 One   = new Vector2(1f, 1f);
        public static Vector2 UnitX = new Vector2(1f, 0f);
        public static Vector2 UnitY = new Vector2(0f, 1f);
        public static Vector2 PositiveInfinity = One * float.PositiveInfinity;

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

        static Vector2()
        {
        }

        /// <summary>
        /// Initializes a new instance of Vector2.
        /// </summary>
        /// <param name="x">Initial value for the x-component of the vector.</param><param name="y">Initial value for the y-component of the vector.</param>
        public Vector2(float x, float y)
        {
            this.X = x;
            this.Y = y;
        }

        /// <summary>
        /// Creates a new instance of Vector2.
        /// </summary>
        /// <param name="value">Value to initialize both components to.</param>
        public Vector2(float value)
        {
            this.X = this.Y = value;
        }

        public float this[int index]
        {
            set
            {
                if (index == 0) X = value;
                else if (index == 1) Y = value;
                else throw new ArgumentException();
            }
            get
            {
                if (index == 0) return X;
                else if (index == 1) return Y;
                else throw new ArgumentException();
            }
        }

        public static explicit operator Vector2I(Vector2 vector)
        {
            return new Vector2I(vector);
        }

        /// <summary>
        /// Returns a vector pointing in the opposite direction.
        /// </summary>
        /// <param name="value">Source vector.</param>
        public static Vector2 operator -(Vector2 value)
        {
            Vector2 vector2;
            vector2.X = -value.X;
            vector2.Y = -value.Y;
            return vector2;
        }

        /// <summary>
        /// Tests vectors for equality.
        /// </summary>
        /// <param name="value1">Source vector.</param><param name="value2">Source vector.</param>
        public static bool operator ==(Vector2 value1, Vector2 value2)
        {
            if ((double)value1.X == (double)value2.X)
                return (double)value1.Y == (double)value2.Y;
            else
                return false;
        }

        /// <summary>
        /// Tests vectors for inequality.
        /// </summary>
        /// <param name="value1">Vector to compare.</param><param name="value2">Vector to compare.</param>
        public static bool operator !=(Vector2 value1, Vector2 value2)
        {
            if ((double)value1.X == (double)value2.X)
                return (double)value1.Y != (double)value2.Y;
            else
                return true;
        }

        /// <summary>
        /// Adds two vectors.
        /// </summary>
        /// <param name="value1">Source vector.</param><param name="value2">Source vector.</param>
        public static Vector2 operator +(Vector2 value1, Vector2 value2)
        {
            Vector2 vector2;
            vector2.X = value1.X + value2.X;
            vector2.Y = value1.Y + value2.Y;
            return vector2;
        }

        /// <summary>
        /// Adds float to each component of a vector.
        /// </summary>
        /// <param name="value1">Source vector.</param><param name="value2">Source float.</param>
        public static Vector2 operator +(Vector2 value1, float value2)
        {
            Vector2 vector2;
            vector2.X = value1.X + value2;
            vector2.Y = value1.Y + value2;
            return vector2;
        }

        /// <summary>
        /// Subtracts a vector from a vector.
        /// </summary>
        /// <param name="value1">Source vector.</param><param name="value2">source vector.</param>
        public static Vector2 operator -(Vector2 value1, Vector2 value2)
        {
            Vector2 vector2;
            vector2.X = value1.X - value2.X;
            vector2.Y = value1.Y - value2.Y;
            return vector2;
        }

        /// <summary>
        /// Subtracts a vector from a vector.
        /// </summary>
        /// <param name="value1">Source vector.</param><param name="value2">source vector.</param>
        public static Vector2 operator -(Vector2 value1, float value2)
        {
            Vector2 vector2;
            vector2.X = value1.X - value2;
            vector2.Y = value1.Y - value2;
            return vector2;
        }

        /// <summary>
        /// Multiplies the components of two vectors by each other.
        /// </summary>
        /// <param name="value1">Source vector.</param><param name="value2">Source vector.</param>
        public static Vector2 operator *(Vector2 value1, Vector2 value2)
        {
            Vector2 vector2;
            vector2.X = value1.X * value2.X;
            vector2.Y = value1.Y * value2.Y;
            return vector2;
        }

        /// <summary>
        /// Multiplies a vector by a scalar value.
        /// </summary>
        /// <param name="value">Source vector.</param><param name="scaleFactor">Scalar value.</param>
        public static Vector2 operator *(Vector2 value, float scaleFactor)
        {
            Vector2 vector2;
            vector2.X = value.X * scaleFactor;
            vector2.Y = value.Y * scaleFactor;
            return vector2;
        }

        /// <summary>
        /// Multiplies a vector by a scalar value.
        /// </summary>
        /// <param name="scaleFactor">Scalar value.</param><param name="value">Source vector.</param>
        public static Vector2 operator *(float scaleFactor, Vector2 value)
        {
            Vector2 vector2;
            vector2.X = value.X * scaleFactor;
            vector2.Y = value.Y * scaleFactor;
            return vector2;
        }

        /// <summary>
        /// Divides the components of a vector by the components of another vector.
        /// </summary>
        /// <param name="value1">Source vector.</param><param name="value2">Divisor vector.</param>
        public static Vector2 operator /(Vector2 value1, Vector2 value2)
        {
            Vector2 vector2;
            vector2.X = value1.X / value2.X;
            vector2.Y = value1.Y / value2.Y;
            return vector2;
        }

        /// <summary>
        /// Divides a vector by a scalar value.
        /// </summary>
        /// <param name="value1">Source vector.</param><param name="divider">The divisor.</param>
        public static Vector2 operator /(Vector2 value1, float divider)
        {
            float num = 1f / divider;
            Vector2 vector2;
            vector2.X = value1.X * num;
            vector2.Y = value1.Y * num;
            return vector2;
        }

        /// <summary>
        /// Divides a scalar value by a vector.
        /// </summary>
        public static Vector2 operator /(float value1, Vector2 value2)
        {
            Vector2 res;
            res.X = value1 / value2.X;
            res.Y = value1 / value2.Y;
            return res;
        }

        /// <summary>
        /// Retrieves a string representation of the current object.
        /// </summary>
        public override string ToString()
        {
            CultureInfo currentCulture = CultureInfo.CurrentCulture;
            return string.Format((IFormatProvider)currentCulture, "{{X:{0} Y:{1}}}", new object[2]
      {
        (object) this.X.ToString((IFormatProvider) currentCulture),
        (object) this.Y.ToString((IFormatProvider) currentCulture)
      });
        }

        /// <summary>
        /// Determines whether the specified Object is equal to the Vector2.
        /// </summary>
        /// <param name="other">The Object to compare with the current Vector2.</param>
        public bool Equals(Vector2 other)
        {
            if ((double)this.X == (double)other.X)
                return (double)this.Y == (double)other.Y;
            else
                return false;
        }

        /// <summary>
        /// Returns a value that indicates whether the current instance is equal to a specified object.
        /// </summary>
        /// <param name="obj">Object to make the comparison with.</param>
        public override bool Equals(object obj)
        {
            bool flag = false;
            if (obj is Vector2)
                flag = this.Equals((Vector2)obj);
            return flag;
        }

        /// <summary>
        /// Gets the hash code of the vector object.
        /// </summary>
        public override int GetHashCode()
        {
            return this.X.GetHashCode() + this.Y.GetHashCode();
        }

        public bool IsValid()
        {
            // We can multiply, when one component is infinity, others will be too. When one is NaN, others will be too.
            return (X * Y).IsValid();
        }

        [Conditional("DEBUG")]
        public void AssertIsValid()
        {
            Debug.Assert(IsValid());
        }

        /// <summary>
        /// Calculates the length of the vector.
        /// </summary>
        public float Length()
        {
            return (float)Math.Sqrt((double)this.X * (double)this.X + (double)this.Y * (double)this.Y);
        }

        /// <summary>
        /// Calculates the length of the vector squared.
        /// </summary>
        public float LengthSquared()
        {
            return (float)((double)this.X * (double)this.X + (double)this.Y * (double)this.Y);
        }

        /// <summary>
        /// Calculates the distance between two vectors.
        /// </summary>
        /// <param name="value1">Source vector.</param><param name="value2">Source vector.</param>
        public static float Distance(Vector2 value1, Vector2 value2)
        {
            float num1 = value1.X - value2.X;
            float num2 = value1.Y - value2.Y;
            return (float)Math.Sqrt((double)num1 * (double)num1 + (double)num2 * (double)num2);
        }

        /// <summary>
        /// Calculates the distance between two vectors.
        /// </summary>
        /// <param name="value1">Source vector.</param><param name="value2">Source vector.</param><param name="result">[OutAttribute] The distance between the vectors.</param>
        public static void Distance(ref Vector2 value1, ref Vector2 value2, out float result)
        {
            float num1 = value1.X - value2.X;
            float num2 = value1.Y - value2.Y;
            float num3 = (float)((double)num1 * (double)num1 + (double)num2 * (double)num2);
            result = (float)Math.Sqrt((double)num3);
        }

        /// <summary>
        /// Calculates the distance between two vectors squared.
        /// </summary>
        /// <param name="value1">Source vector.</param><param name="value2">Source vector.</param>
        public static float DistanceSquared(Vector2 value1, Vector2 value2)
        {
            float num1 = value1.X - value2.X;
            float num2 = value1.Y - value2.Y;
            return (float)((double)num1 * (double)num1 + (double)num2 * (double)num2);
        }

        /// <summary>
        /// Calculates the distance between two vectors squared.
        /// </summary>
        /// <param name="value1">Source vector.</param><param name="value2">Source vector.</param><param name="result">[OutAttribute] The distance between the vectors squared.</param>
        public static void DistanceSquared(ref Vector2 value1, ref Vector2 value2, out float result)
        {
            float num1 = value1.X - value2.X;
            float num2 = value1.Y - value2.Y;
            result = (float)((double)num1 * (double)num1 + (double)num2 * (double)num2);
        }

        /// <summary>
        /// Calculates the dot product of two vectors. If the two vectors are unit vectors, the dot product returns a floating point value between -1 and 1 that can be used to determine some properties of the angle between two vectors. For example, it can show whether the vectors are orthogonal, parallel, or have an acute or obtuse angle between them.
        /// </summary>
        /// <param name="value1">Source vector.</param><param name="value2">Source vector.</param>
        public static float Dot(Vector2 value1, Vector2 value2)
        {
            return (float)((double)value1.X * (double)value2.X + (double)value1.Y * (double)value2.Y);
        }

        /// <summary>
        /// Calculates the dot product of two vectors and writes the result to a user-specified variable. If the two vectors are unit vectors, the dot product returns a floating point value between -1 and 1 that can be used to determine some properties of the angle between two vectors. For example, it can show whether the vectors are orthogonal, parallel, or have an acute or obtuse angle between them.
        /// </summary>
        /// <param name="value1">Source vector.</param><param name="value2">Source vector.</param><param name="result">[OutAttribute] The dot product of the two vectors.</param>
        public static void Dot(ref Vector2 value1, ref Vector2 value2, out float result)
        {
            result = (float)((double)value1.X * (double)value2.X + (double)value1.Y * (double)value2.Y);
        }

        /// <summary>
        /// Turns the current vector into a unit vector. The result is a vector one unit in length pointing in the same direction as the original vector.
        /// </summary>
        public void Normalize()
        {
            float num = 1f / (float)Math.Sqrt((double)this.X * (double)this.X + (double)this.Y * (double)this.Y);
            this.X *= num;
            this.Y *= num;
        }

        /// <summary>
        /// Creates a unit vector from the specified vector. The result is a vector one unit in length pointing in the same direction as the original vector.
        /// </summary>
        /// <param name="value">Source Vector2.</param>
        public static Vector2 Normalize(Vector2 value)
        {
            float num = 1f / (float)Math.Sqrt((double)value.X * (double)value.X + (double)value.Y * (double)value.Y);
            Vector2 vector2;
            vector2.X = value.X * num;
            vector2.Y = value.Y * num;
            return vector2;
        }

        /// <summary>
        /// Creates a unit vector from the specified vector, writing the result to a user-specified variable. The result is a vector one unit in length pointing in the same direction as the original vector.
        /// </summary>
        /// <param name="value">Source vector.</param><param name="result">[OutAttribute] Normalized vector.</param>
        public static void Normalize(ref Vector2 value, out Vector2 result)
        {
            float num = 1f / (float)Math.Sqrt((double)value.X * (double)value.X + (double)value.Y * (double)value.Y);
            result.X = value.X * num;
            result.Y = value.Y * num;
        }

        /// <summary>
        /// Determines the reflect vector of the given vector and normal.
        /// </summary>
        /// <param name="vector">Source vector.</param><param name="normal">Normal of vector.</param>
        public static Vector2 Reflect(Vector2 vector, Vector2 normal)
        {
            float num = (float)((double)vector.X * (double)normal.X + (double)vector.Y * (double)normal.Y);
            Vector2 vector2;
            vector2.X = vector.X - 2f * num * normal.X;
            vector2.Y = vector.Y - 2f * num * normal.Y;
            return vector2;
        }

        /// <summary>
        /// Determines the reflect vector of the given vector and normal.
        /// </summary>
        /// <param name="vector">Source vector.</param><param name="normal">Normal of vector.</param><param name="result">[OutAttribute] The created reflect vector.</param>
        public static void Reflect(ref Vector2 vector, ref Vector2 normal, out Vector2 result)
        {
            float num = (float)((double)vector.X * (double)normal.X + (double)vector.Y * (double)normal.Y);
            result.X = vector.X - 2f * num * normal.X;
            result.Y = vector.Y - 2f * num * normal.Y;
        }

        /// <summary>
        /// Returns a vector that contains the lowest value from each matching pair of components.
        /// </summary>
        /// <param name="value1">Source vector.</param><param name="value2">Source vector.</param>
        public static Vector2 Min(Vector2 value1, Vector2 value2)
        {
            Vector2 vector2;
            vector2.X = (double)value1.X < (double)value2.X ? value1.X : value2.X;
            vector2.Y = (double)value1.Y < (double)value2.Y ? value1.Y : value2.Y;
            return vector2;
        }

        /// <summary>
        /// Returns a vector that contains the lowest value from each matching pair of components.
        /// </summary>
        /// <param name="value1">Source vector.</param><param name="value2">Source vector.</param><param name="result">[OutAttribute] The minimized vector.</param>
        public static void Min(ref Vector2 value1, ref Vector2 value2, out Vector2 result)
        {
            result.X = (double)value1.X < (double)value2.X ? value1.X : value2.X;
            result.Y = (double)value1.Y < (double)value2.Y ? value1.Y : value2.Y;
        }

        /// <summary>
        /// Returns a vector that contains the highest value from each matching pair of components.
        /// </summary>
        /// <param name="value1">Source vector.</param><param name="value2">Source vector.</param>
        public static Vector2 Max(Vector2 value1, Vector2 value2)
        {
            Vector2 vector2;
            vector2.X = (double)value1.X > (double)value2.X ? value1.X : value2.X;
            vector2.Y = (double)value1.Y > (double)value2.Y ? value1.Y : value2.Y;
            return vector2;
        }

        /// <summary>
        /// Returns a vector that contains the highest value from each matching pair of components.
        /// </summary>
        /// <param name="value1">Source vector.</param><param name="value2">Source vector.</param><param name="result">[OutAttribute] The maximized vector.</param>
        public static void Max(ref Vector2 value1, ref Vector2 value2, out Vector2 result)
        {
            result.X = (double)value1.X > (double)value2.X ? value1.X : value2.X;
            result.Y = (double)value1.Y > (double)value2.Y ? value1.Y : value2.Y;
        }

        /// <summary>
        /// Restricts a value to be within a specified range.
        /// </summary>
        /// <param name="value1">The value to clamp.</param><param name="min">The minimum value.</param><param name="max">The maximum value.</param>
        public static Vector2 Clamp(Vector2 value1, Vector2 min, Vector2 max)
        {
            float num1 = value1.X;
            float num2 = (double)num1 > (double)max.X ? max.X : num1;
            float num3 = (double)num2 < (double)min.X ? min.X : num2;
            float num4 = value1.Y;
            float num5 = (double)num4 > (double)max.Y ? max.Y : num4;
            float num6 = (double)num5 < (double)min.Y ? min.Y : num5;
            Vector2 vector2;
            vector2.X = num3;
            vector2.Y = num6;
            return vector2;
        }

        /// <summary>
        /// Restricts a value to be within a specified range.
        /// </summary>
        /// <param name="value1">The value to clamp.</param><param name="min">The minimum value.</param><param name="max">The maximum value.</param><param name="result">[OutAttribute] The clamped value.</param>
        public static void Clamp(ref Vector2 value1, ref Vector2 min, ref Vector2 max, out Vector2 result)
        {
            float num1 = value1.X;
            float num2 = (double)num1 > (double)max.X ? max.X : num1;
            float num3 = (double)num2 < (double)min.X ? min.X : num2;
            float num4 = value1.Y;
            float num5 = (double)num4 > (double)max.Y ? max.Y : num4;
            float num6 = (double)num5 < (double)min.Y ? min.Y : num5;
            result.X = num3;
            result.Y = num6;
        }
		
		[Unsharper.UnsharperDisableReflection()]
        public static Vector2 ClampToSphere(Vector2 vector, float radius)
        {
            float lsq = vector.LengthSquared();
            float rsq = radius * radius;
            if (lsq > rsq)
            {
                return vector * (float)Math.Sqrt(rsq / lsq);
            }
            return vector;
        }

		[Unsharper.UnsharperDisableReflection()]
		public static void ClampToSphere(ref Vector2 vector, float radius)
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
        public static Vector2 Lerp(Vector2 value1, Vector2 value2, float amount)
        {
            Vector2 vector2;
            vector2.X = value1.X + (value2.X - value1.X) * amount;
            vector2.Y = value1.Y + (value2.Y - value1.Y) * amount;
            return vector2;
        }

        /// <summary>
        /// Performs a linear interpolation between two vectors.
        /// </summary>
        /// <param name="value1">Source vector.</param><param name="value2">Source vector.</param><param name="amount">Value between 0 and 1 indicating the weight of value2.</param><param name="result">[OutAttribute] The result of the interpolation.</param>
        public static void Lerp(ref Vector2 value1, ref Vector2 value2, float amount, out Vector2 result)
        {
            result.X = value1.X + (value2.X - value1.X) * amount;
            result.Y = value1.Y + (value2.Y - value1.Y) * amount;
        }

        /// <summary>
        /// Returns a Vector2 containing the 2D Cartesian coordinates of a point specified in barycentric (areal) coordinates relative to a 2D triangle.
        /// </summary>
        /// <param name="value1">A Vector2 containing the 2D Cartesian coordinates of vertex 1 of the triangle.</param><param name="value2">A Vector2 containing the 2D Cartesian coordinates of vertex 2 of the triangle.</param><param name="value3">A Vector2 containing the 2D Cartesian coordinates of vertex 3 of the triangle.</param><param name="amount1">Barycentric coordinate b2, which expresses the weighting factor toward vertex 2 (specified in value2).</param><param name="amount2">Barycentric coordinate b3, which expresses the weighting factor toward vertex 3 (specified in value3).</param>
        public static Vector2 Barycentric(Vector2 value1, Vector2 value2, Vector2 value3, float amount1, float amount2)
        {
            Vector2 vector2;
            vector2.X = (float)((double)value1.X + (double)amount1 * ((double)value2.X - (double)value1.X) + (double)amount2 * ((double)value3.X - (double)value1.X));
            vector2.Y = (float)((double)value1.Y + (double)amount1 * ((double)value2.Y - (double)value1.Y) + (double)amount2 * ((double)value3.Y - (double)value1.Y));
            return vector2;
        }

        /// <summary>
        /// Returns a Vector2 containing the 2D Cartesian coordinates of a point specified in barycentric (areal) coordinates relative to a 2D triangle.
        /// </summary>
        /// <param name="value1">A Vector2 containing the 2D Cartesian coordinates of vertex 1 of the triangle.</param><param name="value2">A Vector2 containing the 2D Cartesian coordinates of vertex 2 of the triangle.</param><param name="value3">A Vector2 containing the 2D Cartesian coordinates of vertex 3 of the triangle.</param><param name="amount1">Barycentric coordinate b2, which expresses the weighting factor toward vertex 2 (specified in value2).</param><param name="amount2">Barycentric coordinate b3, which expresses the weighting factor toward vertex 3 (specified in value3).</param><param name="result">[OutAttribute] The 2D Cartesian coordinates of the specified point are placed in this Vector2 on exit.</param>
        public static void Barycentric(ref Vector2 value1, ref Vector2 value2, ref Vector2 value3, float amount1, float amount2, out Vector2 result)
        {
            result.X = (float)((double)value1.X + (double)amount1 * ((double)value2.X - (double)value1.X) + (double)amount2 * ((double)value3.X - (double)value1.X));
            result.Y = (float)((double)value1.Y + (double)amount1 * ((double)value2.Y - (double)value1.Y) + (double)amount2 * ((double)value3.Y - (double)value1.Y));
        }

        /// <summary>
        /// Interpolates between two values using a cubic equation.
        /// </summary>
        /// <param name="value1">Source value.</param><param name="value2">Source value.</param><param name="amount">Weighting value.</param>
        public static Vector2 SmoothStep(Vector2 value1, Vector2 value2, float amount)
        {
            amount = (double)amount > 1.0 ? 1f : ((double)amount < 0.0 ? 0.0f : amount);
            amount = (float)((double)amount * (double)amount * (3.0 - 2.0 * (double)amount));
            Vector2 vector2;
            vector2.X = value1.X + (value2.X - value1.X) * amount;
            vector2.Y = value1.Y + (value2.Y - value1.Y) * amount;
            return vector2;
        }

        /// <summary>
        /// Interpolates between two values using a cubic equation.
        /// </summary>
        /// <param name="value1">Source value.</param><param name="value2">Source value.</param><param name="amount">Weighting value.</param><param name="result">[OutAttribute] The interpolated value.</param>
        public static void SmoothStep(ref Vector2 value1, ref Vector2 value2, float amount, out Vector2 result)
        {
            amount = (double)amount > 1.0 ? 1f : ((double)amount < 0.0 ? 0.0f : amount);
            amount = (float)((double)amount * (double)amount * (3.0 - 2.0 * (double)amount));
            result.X = value1.X + (value2.X - value1.X) * amount;
            result.Y = value1.Y + (value2.Y - value1.Y) * amount;
        }

        /// <summary>
        /// Performs a Catmull-Rom interpolation using the specified positions.
        /// </summary>
        /// <param name="value1">The first position in the interpolation.</param><param name="value2">The second position in the interpolation.</param><param name="value3">The third position in the interpolation.</param><param name="value4">The fourth position in the interpolation.</param><param name="amount">Weighting factor.</param>
        public static Vector2 CatmullRom(Vector2 value1, Vector2 value2, Vector2 value3, Vector2 value4, float amount)
        {
            float num1 = amount * amount;
            float num2 = amount * num1;
            Vector2 vector2;
            vector2.X = (float)(0.5 * (2.0 * (double)value2.X + (-(double)value1.X + (double)value3.X) * (double)amount + (2.0 * (double)value1.X - 5.0 * (double)value2.X + 4.0 * (double)value3.X - (double)value4.X) * (double)num1 + (-(double)value1.X + 3.0 * (double)value2.X - 3.0 * (double)value3.X + (double)value4.X) * (double)num2));
            vector2.Y = (float)(0.5 * (2.0 * (double)value2.Y + (-(double)value1.Y + (double)value3.Y) * (double)amount + (2.0 * (double)value1.Y - 5.0 * (double)value2.Y + 4.0 * (double)value3.Y - (double)value4.Y) * (double)num1 + (-(double)value1.Y + 3.0 * (double)value2.Y - 3.0 * (double)value3.Y + (double)value4.Y) * (double)num2));
            return vector2;
        }

        /// <summary>
        /// Performs a Catmull-Rom interpolation using the specified positions.
        /// </summary>
        /// <param name="value1">The first position in the interpolation.</param><param name="value2">The second position in the interpolation.</param><param name="value3">The third position in the interpolation.</param><param name="value4">The fourth position in the interpolation.</param><param name="amount">Weighting factor.</param><param name="result">[OutAttribute] A vector that is the result of the Catmull-Rom interpolation.</param>
        public static void CatmullRom(ref Vector2 value1, ref Vector2 value2, ref Vector2 value3, ref Vector2 value4, float amount, out Vector2 result)
        {
            float num1 = amount * amount;
            float num2 = amount * num1;
            result.X = (float)(0.5 * (2.0 * (double)value2.X + (-(double)value1.X + (double)value3.X) * (double)amount + (2.0 * (double)value1.X - 5.0 * (double)value2.X + 4.0 * (double)value3.X - (double)value4.X) * (double)num1 + (-(double)value1.X + 3.0 * (double)value2.X - 3.0 * (double)value3.X + (double)value4.X) * (double)num2));
            result.Y = (float)(0.5 * (2.0 * (double)value2.Y + (-(double)value1.Y + (double)value3.Y) * (double)amount + (2.0 * (double)value1.Y - 5.0 * (double)value2.Y + 4.0 * (double)value3.Y - (double)value4.Y) * (double)num1 + (-(double)value1.Y + 3.0 * (double)value2.Y - 3.0 * (double)value3.Y + (double)value4.Y) * (double)num2));
        }

        /// <summary>
        /// Performs a Hermite spline interpolation.
        /// </summary>
        /// <param name="value1">Source position vector.</param><param name="tangent1">Source tangent vector.</param><param name="value2">Source position vector.</param><param name="tangent2">Source tangent vector.</param><param name="amount">Weighting factor.</param>
        public static Vector2 Hermite(Vector2 value1, Vector2 tangent1, Vector2 value2, Vector2 tangent2, float amount)
        {
            float num1 = amount * amount;
            float num2 = amount * num1;
            float num3 = (float)(2.0 * (double)num2 - 3.0 * (double)num1 + 1.0);
            float num4 = (float)(-2.0 * (double)num2 + 3.0 * (double)num1);
            float num5 = num2 - 2f * num1 + amount;
            float num6 = num2 - num1;
            Vector2 vector2;
            vector2.X = (float)((double)value1.X * (double)num3 + (double)value2.X * (double)num4 + (double)tangent1.X * (double)num5 + (double)tangent2.X * (double)num6);
            vector2.Y = (float)((double)value1.Y * (double)num3 + (double)value2.Y * (double)num4 + (double)tangent1.Y * (double)num5 + (double)tangent2.Y * (double)num6);
            return vector2;
        }

        /// <summary>
        /// Performs a Hermite spline interpolation.
        /// </summary>
        /// <param name="value1">Source position vector.</param><param name="tangent1">Source tangent vector.</param><param name="value2">Source position vector.</param><param name="tangent2">Source tangent vector.</param><param name="amount">Weighting factor.</param><param name="result">[OutAttribute] The result of the Hermite spline interpolation.</param>
        public static void Hermite(ref Vector2 value1, ref Vector2 tangent1, ref Vector2 value2, ref Vector2 tangent2, float amount, out Vector2 result)
        {
            float num1 = amount * amount;
            float num2 = amount * num1;
            float num3 = (float)(2.0 * (double)num2 - 3.0 * (double)num1 + 1.0);
            float num4 = (float)(-2.0 * (double)num2 + 3.0 * (double)num1);
            float num5 = num2 - 2f * num1 + amount;
            float num6 = num2 - num1;
            result.X = (float)((double)value1.X * (double)num3 + (double)value2.X * (double)num4 + (double)tangent1.X * (double)num5 + (double)tangent2.X * (double)num6);
            result.Y = (float)((double)value1.Y * (double)num3 + (double)value2.Y * (double)num4 + (double)tangent1.Y * (double)num5 + (double)tangent2.Y * (double)num6);
        }

        /// <summary>
        /// Transforms the vector (x, y, 0, 1) by the specified matrix.
        /// </summary>
        /// <param name="position">The source vector.</param><param name="matrix">The transformation matrix.</param>
        public static Vector2 Transform(Vector2 position, Matrix matrix)
        {
            float num1 = (float)((double)position.X * (double)matrix.M11 + (double)position.Y * (double)matrix.M21) + matrix.M41;
            float num2 = (float)((double)position.X * (double)matrix.M12 + (double)position.Y * (double)matrix.M22) + matrix.M42;
            Vector2 vector2;
            vector2.X = num1;
            vector2.Y = num2;
            return vector2;
        }

        /// <summary>
        /// Transforms a Vector2 by the given Matrix.
        /// </summary>
        /// <param name="position">The source Vector2.</param><param name="matrix">The transformation Matrix.</param><param name="result">[OutAttribute] The Vector2 resulting from the transformation.</param>
        public static void Transform(ref Vector2 position, ref Matrix matrix, out Vector2 result)
        {
            float num1 = (float)((double)position.X * (double)matrix.M11 + (double)position.Y * (double)matrix.M21) + matrix.M41;
            float num2 = (float)((double)position.X * (double)matrix.M12 + (double)position.Y * (double)matrix.M22) + matrix.M42;
            result.X = num1;
            result.Y = num2;
        }

        /// <summary>
        /// Transforms a 2D vector normal by a matrix.
        /// </summary>
        /// <param name="normal">The source vector.</param><param name="matrix">The transformation matrix.</param>
        public static Vector2 TransformNormal(Vector2 normal, Matrix matrix)
        {
            float num1 = (float)((double)normal.X * (double)matrix.M11 + (double)normal.Y * (double)matrix.M21);
            float num2 = (float)((double)normal.X * (double)matrix.M12 + (double)normal.Y * (double)matrix.M22);
            Vector2 vector2;
            vector2.X = num1;
            vector2.Y = num2;
            return vector2;
        }

        /// <summary>
        /// Transforms a vector normal by a matrix.
        /// </summary>
        /// <param name="normal">The source vector.</param><param name="matrix">The transformation matrix.</param><param name="result">[OutAttribute] The Vector2 resulting from the transformation.</param>
        public static void TransformNormal(ref Vector2 normal, ref Matrix matrix, out Vector2 result)
        {
            float num1 = (float)((double)normal.X * (double)matrix.M11 + (double)normal.Y * (double)matrix.M21);
            float num2 = (float)((double)normal.X * (double)matrix.M12 + (double)normal.Y * (double)matrix.M22);
            result.X = num1;
            result.Y = num2;
        }

        /// <summary>
        /// Transforms a single Vector2, or the vector normal (x, y, 0, 0), by a specified Quaternion rotation.
        /// </summary>
        /// <param name="value">The vector to rotate.</param><param name="rotation">The Quaternion rotation to apply.</param>
        public static Vector2 Transform(Vector2 value, Quaternion rotation)
        {
            float num1 = rotation.X + rotation.X;
            float num2 = rotation.Y + rotation.Y;
            float num3 = rotation.Z + rotation.Z;
            float num4 = rotation.W * num3;
            float num5 = rotation.X * num1;
            float num6 = rotation.X * num2;
            float num7 = rotation.Y * num2;
            float num8 = rotation.Z * num3;
            float num9 = (float)((double)value.X * (1.0 - (double)num7 - (double)num8) + (double)value.Y * ((double)num6 - (double)num4));
            float num10 = (float)((double)value.X * ((double)num6 + (double)num4) + (double)value.Y * (1.0 - (double)num5 - (double)num8));
            Vector2 vector2;
            vector2.X = num9;
            vector2.Y = num10;
            return vector2;
        }

        /// <summary>
        /// Transforms a Vector2, or the vector normal (x, y, 0, 0), by a specified Quaternion rotation.
        /// </summary>
        /// <param name="value">The vector to rotate.</param><param name="rotation">The Quaternion rotation to apply.</param><param name="result">[OutAttribute] An existing Vector2 filled in with the result of the rotation.</param>
        public static void Transform(ref Vector2 value, ref Quaternion rotation, out Vector2 result)
        {
            float num1 = rotation.X + rotation.X;
            float num2 = rotation.Y + rotation.Y;
            float num3 = rotation.Z + rotation.Z;
            float num4 = rotation.W * num3;
            float num5 = rotation.X * num1;
            float num6 = rotation.X * num2;
            float num7 = rotation.Y * num2;
            float num8 = rotation.Z * num3;
            float num9 = (float)((double)value.X * (1.0 - (double)num7 - (double)num8) + (double)value.Y * ((double)num6 - (double)num4));
            float num10 = (float)((double)value.X * ((double)num6 + (double)num4) + (double)value.Y * (1.0 - (double)num5 - (double)num8));
            result.X = num9;
            result.Y = num10;
        }

        /// <summary>
        /// Transforms an array of Vector2s by a specified Matrix.
        /// </summary>
        /// <param name="sourceArray">The array of Vector2s to transform.</param><param name="matrix">The transform Matrix to apply.</param><param name="destinationArray">An existing array into which the transformed Vector2s are written.</param>
        public static void Transform(Vector2[] sourceArray, ref Matrix matrix, Vector2[] destinationArray)
        {
            for (int index = 0; index < sourceArray.Length; ++index)
            {
                float num1 = sourceArray[index].X;
                float num2 = sourceArray[index].Y;
                destinationArray[index].X = (float)((double)num1 * (double)matrix.M11 + (double)num2 * (double)matrix.M21) + matrix.M41;
                destinationArray[index].Y = (float)((double)num1 * (double)matrix.M12 + (double)num2 * (double)matrix.M22) + matrix.M42;
            }
        }

        /// <summary>
        /// Transforms a specified range in an array of Vector2s by a specified Matrix and places the results in a specified range in a destination array.
        /// </summary>
        /// <param name="sourceArray">The source array.</param><param name="sourceIndex">The index of the first Vector2 to transform in the source array.</param><param name="matrix">The Matrix to transform by.</param><param name="destinationArray">The destination array into which the resulting Vector2s will be written.</param><param name="destinationIndex">The index of the position in the destination array where the first result Vector2 should be written.</param><param name="length">The number of Vector2s to be transformed.</param>
        public static void Transform(Vector2[] sourceArray, int sourceIndex, ref Matrix matrix, Vector2[] destinationArray, int destinationIndex, int length)
        {
            for (; length > 0; --length)
            {
                float num1 = sourceArray[sourceIndex].X;
                float num2 = sourceArray[sourceIndex].Y;
                destinationArray[destinationIndex].X = (float)((double)num1 * (double)matrix.M11 + (double)num2 * (double)matrix.M21) + matrix.M41;
                destinationArray[destinationIndex].Y = (float)((double)num1 * (double)matrix.M12 + (double)num2 * (double)matrix.M22) + matrix.M42;
                ++sourceIndex;
                ++destinationIndex;
            }
        }

        /// <summary>
        /// Transforms an array of Vector2 vector normals by a specified Matrix.
        /// </summary>
        /// <param name="sourceArray">The array of vector normals to transform.</param><param name="matrix">The transform Matrix to apply.</param><param name="destinationArray">An existing array into which the transformed vector normals are written.</param>
        public static void TransformNormal(Vector2[] sourceArray, ref Matrix matrix, Vector2[] destinationArray)
        {
            for (int index = 0; index < sourceArray.Length; ++index)
            {
                float num1 = sourceArray[index].X;
                float num2 = sourceArray[index].Y;
                destinationArray[index].X = (float)((double)num1 * (double)matrix.M11 + (double)num2 * (double)matrix.M21);
                destinationArray[index].Y = (float)((double)num1 * (double)matrix.M12 + (double)num2 * (double)matrix.M22);
            }
        }

        /// <summary>
        /// Transforms a specified range in an array of Vector2 vector normals by a specified Matrix and places the results in a specified range in a destination array.
        /// </summary>
        /// <param name="sourceArray">The source array.</param><param name="sourceIndex">The index of the first Vector2 to transform in the source array.</param><param name="matrix">The Matrix to apply.</param><param name="destinationArray">The destination array into which the resulting Vector2s are written.</param><param name="destinationIndex">The index of the position in the destination array where the first result Vector2 should be written.</param><param name="length">The number of vector normals to be transformed.</param>
        public static void TransformNormal(Vector2[] sourceArray, int sourceIndex, ref Matrix matrix, Vector2[] destinationArray, int destinationIndex, int length)
        {
            for (; length > 0; --length)
            {
                float num1 = sourceArray[sourceIndex].X;
                float num2 = sourceArray[sourceIndex].Y;
                destinationArray[destinationIndex].X = (float)((double)num1 * (double)matrix.M11 + (double)num2 * (double)matrix.M21);
                destinationArray[destinationIndex].Y = (float)((double)num1 * (double)matrix.M12 + (double)num2 * (double)matrix.M22);
                ++sourceIndex;
                ++destinationIndex;
            }
        }

        /// <summary>
        /// Transforms an array of Vector2s by a specified Quaternion.
        /// </summary>
        /// <param name="sourceArray">The array of Vector2s to transform.</param><param name="rotation">The transform Matrix to use.</param><param name="destinationArray">An existing array into which the transformed Vector2s are written.</param>
        public static void Transform(Vector2[] sourceArray, ref Quaternion rotation, Vector2[] destinationArray)
        {
            float num1 = rotation.X + rotation.X;
            float num2 = rotation.Y + rotation.Y;
            float num3 = rotation.Z + rotation.Z;
            float num4 = rotation.W * num3;
            float num5 = rotation.X * num1;
            float num6 = rotation.X * num2;
            float num7 = rotation.Y * num2;
            float num8 = rotation.Z * num3;
            float num9 = 1f - num7 - num8;
            float num10 = num6 - num4;
            float num11 = num6 + num4;
            float num12 = 1f - num5 - num8;
            for (int index = 0; index < sourceArray.Length; ++index)
            {
                float num13 = sourceArray[index].X;
                float num14 = sourceArray[index].Y;
                destinationArray[index].X = (float)((double)num13 * (double)num9 + (double)num14 * (double)num10);
                destinationArray[index].Y = (float)((double)num13 * (double)num11 + (double)num14 * (double)num12);
            }
        }

        /// <summary>
        /// Transforms a specified range in an array of Vector2s by a specified Quaternion and places the results in a specified range in a destination array.
        /// </summary>
        /// <param name="sourceArray">The source array.</param><param name="sourceIndex">The index of the first Vector2 to transform in the source array.</param><param name="rotation">The Quaternion rotation to apply.</param><param name="destinationArray">The destination array into which the resulting Vector2s are written.</param><param name="destinationIndex">The index of the position in the destination array where the first result Vector2 should be written.</param><param name="length">The number of Vector2s to be transformed.</param>
        public static void Transform(Vector2[] sourceArray, int sourceIndex, ref Quaternion rotation, Vector2[] destinationArray, int destinationIndex, int length)
        {
            float num1 = rotation.X + rotation.X;
            float num2 = rotation.Y + rotation.Y;
            float num3 = rotation.Z + rotation.Z;
            float num4 = rotation.W * num3;
            float num5 = rotation.X * num1;
            float num6 = rotation.X * num2;
            float num7 = rotation.Y * num2;
            float num8 = rotation.Z * num3;
            float num9 = 1f - num7 - num8;
            float num10 = num6 - num4;
            float num11 = num6 + num4;
            float num12 = 1f - num5 - num8;
            for (; length > 0; --length)
            {
                float num13 = sourceArray[sourceIndex].X;
                float num14 = sourceArray[sourceIndex].Y;
                destinationArray[destinationIndex].X = (float)((double)num13 * (double)num9 + (double)num14 * (double)num10);
                destinationArray[destinationIndex].Y = (float)((double)num13 * (double)num11 + (double)num14 * (double)num12);
                ++sourceIndex;
                ++destinationIndex;
            }
        }

        /// <summary>
        /// Returns a vector pointing in the opposite direction.
        /// </summary>
        /// <param name="value">Source vector.</param>
        public static Vector2 Negate(Vector2 value)
        {
            Vector2 vector2;
            vector2.X = -value.X;
            vector2.Y = -value.Y;
            return vector2;
        }

        /// <summary>
        /// Returns a vector pointing in the opposite direction.
        /// </summary>
        /// <param name="value">Source vector.</param><param name="result">[OutAttribute] Vector pointing in the opposite direction.</param>
        public static void Negate(ref Vector2 value, out Vector2 result)
        {
            result.X = -value.X;
            result.Y = -value.Y;
        }

        /// <summary>
        /// Adds two vectors.
        /// </summary>
        /// <param name="value1">Source vector.</param><param name="value2">Source vector.</param>
        public static Vector2 Add(Vector2 value1, Vector2 value2)
        {
            Vector2 vector2;
            vector2.X = value1.X + value2.X;
            vector2.Y = value1.Y + value2.Y;
            return vector2;
        }

        /// <summary>
        /// Adds two vectors.
        /// </summary>
        /// <param name="value1">Source vector.</param><param name="value2">Source vector.</param><param name="result">[OutAttribute] Sum of the source vectors.</param>
        public static void Add(ref Vector2 value1, ref Vector2 value2, out Vector2 result)
        {
            result.X = value1.X + value2.X;
            result.Y = value1.Y + value2.Y;
        }

        /// <summary>
        /// Subtracts a vector from a vector.
        /// </summary>
        /// <param name="value1">Source vector.</param><param name="value2">Source vector.</param>
        public static Vector2 Subtract(Vector2 value1, Vector2 value2)
        {
            Vector2 vector2;
            vector2.X = value1.X - value2.X;
            vector2.Y = value1.Y - value2.Y;
            return vector2;
        }

        /// <summary>
        /// Subtracts a vector from a vector.
        /// </summary>
        /// <param name="value1">Source vector.</param><param name="value2">Source vector.</param><param name="result">[OutAttribute] The result of the subtraction.</param>
        public static void Subtract(ref Vector2 value1, ref Vector2 value2, out Vector2 result)
        {
            result.X = value1.X - value2.X;
            result.Y = value1.Y - value2.Y;
        }

        /// <summary>
        /// Multiplies the components of two vectors by each other.
        /// </summary>
        /// <param name="value1">Source vector.</param><param name="value2">Source vector.</param>
        public static Vector2 Multiply(Vector2 value1, Vector2 value2)
        {
            Vector2 vector2;
            vector2.X = value1.X * value2.X;
            vector2.Y = value1.Y * value2.Y;
            return vector2;
        }

        /// <summary>
        /// Multiplies the components of two vectors by each other.
        /// </summary>
        /// <param name="value1">Source vector.</param><param name="value2">Source vector.</param><param name="result">[OutAttribute] The result of the multiplication.</param>
        public static void Multiply(ref Vector2 value1, ref Vector2 value2, out Vector2 result)
        {
            result.X = value1.X * value2.X;
            result.Y = value1.Y * value2.Y;
        }

        /// <summary>
        /// Multiplies a vector by a scalar value.
        /// </summary>
        /// <param name="value1">Source vector.</param><param name="scaleFactor">Scalar value.</param>
        public static Vector2 Multiply(Vector2 value1, float scaleFactor)
        {
            Vector2 vector2;
            vector2.X = value1.X * scaleFactor;
            vector2.Y = value1.Y * scaleFactor;
            return vector2;
        }

        /// <summary>
        /// Multiplies a vector by a scalar value.
        /// </summary>
        /// <param name="value1">Source vector.</param><param name="scaleFactor">Scalar value.</param><param name="result">[OutAttribute] The result of the multiplication.</param>
        public static void Multiply(ref Vector2 value1, float scaleFactor, out Vector2 result)
        {
            result.X = value1.X * scaleFactor;
            result.Y = value1.Y * scaleFactor;
        }

        /// <summary>
        /// Divides the components of a vector by the components of another vector.
        /// </summary>
        /// <param name="value1">Source vector.</param><param name="value2">Divisor vector.</param>
        public static Vector2 Divide(Vector2 value1, Vector2 value2)
        {
            Vector2 vector2;
            vector2.X = value1.X / value2.X;
            vector2.Y = value1.Y / value2.Y;
            return vector2;
        }

        /// <summary>
        /// Divides the components of a vector by the components of another vector.
        /// </summary>
        /// <param name="value1">Source vector.</param><param name="value2">The divisor.</param><param name="result">[OutAttribute] The result of the division.</param>
        public static void Divide(ref Vector2 value1, ref Vector2 value2, out Vector2 result)
        {
            result.X = value1.X / value2.X;
            result.Y = value1.Y / value2.Y;
        }

        /// <summary>
        /// Divides a vector by a scalar value.
        /// </summary>
        /// <param name="value1">Source vector.</param><param name="divider">The divisor.</param>
        public static Vector2 Divide(Vector2 value1, float divider)
        {
            float num = 1f / divider;
            Vector2 vector2;
            vector2.X = value1.X * num;
            vector2.Y = value1.Y * num;
            return vector2;
        }

        /// <summary>
        /// Divides a vector by a scalar value.
        /// </summary>
        /// <param name="value1">Source vector.</param><param name="divider">The divisor.</param><param name="result">[OutAttribute] The result of the division.</param>
        public static void Divide(ref Vector2 value1, float divider, out Vector2 result)
        {
            float num = 1f / divider;
            result.X = value1.X * num;
            result.Y = value1.Y * num;
        }

        public bool Between(ref Vector2 start, ref Vector2 end)
        {
            return X >= start.X && X <= end.X || Y >= start.Y && Y <= end.Y;
        }

        public static Vector2 Floor(Vector2 position)
        {
            return new Vector2((float)Math.Floor(position.X), (float)Math.Floor(position.Y));
        }

        public void Rotate(double angle)
        {
            float tmpX = X;
            X = X * (float)Math.Cos(angle) - Y * (float)Math.Sin(angle);
            Y = Y * (float)Math.Cos(angle) + tmpX * (float)Math.Sin(angle);
        }

        public static bool IsZero(ref Vector2 value)
        {
            return IsZero(ref value, 0.0001f);
        }

        // Per component IsZero, returns 1 for each component which equals to 0
        public static bool IsZero(ref Vector2 value, float epsilon)
        {
            return Math.Abs(value.X) < epsilon && Math.Abs(value.Y) < epsilon;
        }

        public static bool IsZero(Vector2 value, float epsilon)
        {
            return Math.Abs(value.X) < epsilon && Math.Abs(value.Y) < epsilon;
        }

    }
}
