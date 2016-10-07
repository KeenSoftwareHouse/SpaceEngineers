using System;
using System.Diagnostics;
using System.Globalization;

namespace VRageMath
{
    /// <summary>
    /// Defines a vector with two components.
    /// </summary>
    [ProtoBuf.ProtoContract, Serializable]
    public struct Vector2D : IEquatable<Vector2D>
    {
        public static Vector2D Zero  = new Vector2D();
        public static Vector2D One   = new Vector2D(1f, 1f);
        public static Vector2D UnitX = new Vector2D(1f, 0f);
        public static Vector2D UnitY = new Vector2D(0f, 1f);
        public static Vector2D PositiveInfinity = One * double.PositiveInfinity;

        /// <summary>
        /// Gets or sets the x-component of the vector.
        /// </summary>
        [ProtoBuf.ProtoMember]
        public double X;
        /// <summary>
        /// Gets or sets the y-component of the vector.
        /// </summary>
        [ProtoBuf.ProtoMember]
        public double Y;

        static Vector2D()
        {
        }

        /// <summary>
        /// Initializes a new instance of Vector2D.
        /// </summary>
        /// <param name="x">Initial value for the x-component of the vector.</param><param name="y">Initial value for the y-component of the vector.</param>
        public Vector2D(double x, double y)
        {
            this.X = x;
            this.Y = y;
        }

        /// <summary>
        /// Creates a new instance of Vector2D.
        /// </summary>
        /// <param name="value">Value to initialize both components to.</param>
        public Vector2D(double value)
        {
            this.X = this.Y = value;
        }

        public double this[int index]
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

        public static explicit operator Vector2I(Vector2D vector)
        {
            return new Vector2I(vector);
        }

        /// <summary>
        /// Returns a vector pointing in the opposite direction.
        /// </summary>
        /// <param name="value">Source vector.</param>
        public static Vector2D operator -(Vector2D value)
        {
            Vector2D vector2D;
            vector2D.X = -value.X;
            vector2D.Y = -value.Y;
            return vector2D;
        }

        /// <summary>
        /// Tests vectors for equality.
        /// </summary>
        /// <param name="value1">Source vector.</param><param name="value2">Source vector.</param>
        public static bool operator ==(Vector2D value1, Vector2D value2)
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
        public static bool operator !=(Vector2D value1, Vector2D value2)
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
        public static Vector2D operator +(Vector2D value1, Vector2D value2)
        {
            Vector2D vector2D;
            vector2D.X = value1.X + value2.X;
            vector2D.Y = value1.Y + value2.Y;
            return vector2D;
        }

        /// <summary>
        /// Adds double to each component of a vector.
        /// </summary>
        /// <param name="value1">Source vector.</param><param name="value2">Source double.</param>
        public static Vector2D operator +(Vector2D value1, double value2)
        {
            Vector2D vector2D;
            vector2D.X = value1.X + value2;
            vector2D.Y = value1.Y + value2;
            return vector2D;
        }

        /// <summary>
        /// Subtracts a vector from a vector.
        /// </summary>
        /// <param name="value1">Source vector.</param><param name="value2">source vector.</param>
        public static Vector2D operator -(Vector2D value1, Vector2D value2)
        {
            Vector2D vector2D;
            vector2D.X = value1.X - value2.X;
            vector2D.Y = value1.Y - value2.Y;
            return vector2D;
        }

        /// <summary>
        /// Subtracts a vector from a vector.
        /// </summary>
        /// <param name="value1">Source vector.</param><param name="value2">source vector.</param>
        public static Vector2D operator -(Vector2D value1, double value2)
        {
            Vector2D vector2D;
            vector2D.X = value1.X - value2;
            vector2D.Y = value1.Y - value2;
            return vector2D;
        }

        /// <summary>
        /// Multiplies the components of two vectors by each other.
        /// </summary>
        /// <param name="value1">Source vector.</param><param name="value2">Source vector.</param>
        public static Vector2D operator *(Vector2D value1, Vector2D value2)
        {
            Vector2D vector2D;
            vector2D.X = value1.X * value2.X;
            vector2D.Y = value1.Y * value2.Y;
            return vector2D;
        }

        /// <summary>
        /// Multiplies a vector by a scalar value.
        /// </summary>
        /// <param name="value">Source vector.</param><param name="scaleFactor">Scalar value.</param>
        public static Vector2D operator *(Vector2D value, double scaleFactor)
        {
            Vector2D vector2D;
            vector2D.X = value.X * scaleFactor;
            vector2D.Y = value.Y * scaleFactor;
            return vector2D;
        }

        /// <summary>
        /// Multiplies a vector by a scalar value.
        /// </summary>
        /// <param name="scaleFactor">Scalar value.</param><param name="value">Source vector.</param>
        public static Vector2D operator *(double scaleFactor, Vector2D value)
        {
            Vector2D vector2D;
            vector2D.X = value.X * scaleFactor;
            vector2D.Y = value.Y * scaleFactor;
            return vector2D;
        }

        /// <summary>
        /// Divides the components of a vector by the components of another vector.
        /// </summary>
        /// <param name="value1">Source vector.</param><param name="value2">Divisor vector.</param>
        public static Vector2D operator /(Vector2D value1, Vector2D value2)
        {
            Vector2D vector2D;
            vector2D.X = value1.X / value2.X;
            vector2D.Y = value1.Y / value2.Y;
            return vector2D;
        }

        /// <summary>
        /// Divides a vector by a scalar value.
        /// </summary>
        /// <param name="value1">Source vector.</param><param name="divider">The divisor.</param>
        public static Vector2D operator /(Vector2D value1, double divider)
        {
            double num = 1f / divider;
            Vector2D vector2D;
            vector2D.X = value1.X * num;
            vector2D.Y = value1.Y * num;
            return vector2D;
        }

        /// <summary>
        /// Divides a scalar value by a vector.
        /// </summary>
        public static Vector2D operator /(double value1, Vector2D value2)
        {
            Vector2D res;
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
        /// Determines whether the specified Object is equal to the Vector2D.
        /// </summary>
        /// <param name="other">The Object to compare with the current Vector2D.</param>
        public bool Equals(Vector2D other)
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
            if (obj is Vector2D)
                flag = this.Equals((Vector2D)obj);
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
        public double Length()
        {
            return (double)Math.Sqrt((double)this.X * (double)this.X + (double)this.Y * (double)this.Y);
        }

        /// <summary>
        /// Calculates the length of the vector squared.
        /// </summary>
        public double LengthSquared()
        {
            return (double)((double)this.X * (double)this.X + (double)this.Y * (double)this.Y);
        }

        /// <summary>
        /// Calculates the distance between two vectors.
        /// </summary>
        /// <param name="value1">Source vector.</param><param name="value2">Source vector.</param>
        public static double Distance(Vector2D value1, Vector2D value2)
        {
            double num1 = value1.X - value2.X;
            double num2 = value1.Y - value2.Y;
            return (double)Math.Sqrt((double)num1 * (double)num1 + (double)num2 * (double)num2);
        }

        /// <summary>
        /// Calculates the distance between two vectors.
        /// </summary>
        /// <param name="value1">Source vector.</param><param name="value2">Source vector.</param><param name="result">[OutAttribute] The distance between the vectors.</param>
        public static void Distance(ref Vector2D value1, ref Vector2D value2, out double result)
        {
            double num1 = value1.X - value2.X;
            double num2 = value1.Y - value2.Y;
            double num3 = (double)((double)num1 * (double)num1 + (double)num2 * (double)num2);
            result = (double)Math.Sqrt((double)num3);
        }

        /// <summary>
        /// Calculates the distance between two vectors squared.
        /// </summary>
        /// <param name="value1">Source vector.</param><param name="value2">Source vector.</param>
        public static double DistanceSquared(Vector2D value1, Vector2D value2)
        {
            double num1 = value1.X - value2.X;
            double num2 = value1.Y - value2.Y;
            return (double)((double)num1 * (double)num1 + (double)num2 * (double)num2);
        }

        /// <summary>
        /// Calculates the distance between two vectors squared.
        /// </summary>
        /// <param name="value1">Source vector.</param><param name="value2">Source vector.</param><param name="result">[OutAttribute] The distance between the vectors squared.</param>
        public static void DistanceSquared(ref Vector2D value1, ref Vector2D value2, out double result)
        {
            double num1 = value1.X - value2.X;
            double num2 = value1.Y - value2.Y;
            result = (double)((double)num1 * (double)num1 + (double)num2 * (double)num2);
        }

        /// <summary>
        /// Calculates the dot product of two vectors. If the two vectors are unit vectors, the dot product returns a doubleing point value between -1 and 1 that can be used to determine some properties of the angle between two vectors. For example, it can show whether the vectors are orthogonal, parallel, or have an acute or obtuse angle between them.
        /// </summary>
        /// <param name="value1">Source vector.</param><param name="value2">Source vector.</param>
        public static double Dot(Vector2D value1, Vector2D value2)
        {
            return (double)((double)value1.X * (double)value2.X + (double)value1.Y * (double)value2.Y);
        }

        /// <summary>
        /// Calculates the dot product of two vectors and writes the result to a user-specified variable. If the two vectors are unit vectors, the dot product returns a doubleing point value between -1 and 1 that can be used to determine some properties of the angle between two vectors. For example, it can show whether the vectors are orthogonal, parallel, or have an acute or obtuse angle between them.
        /// </summary>
        /// <param name="value1">Source vector.</param><param name="value2">Source vector.</param><param name="result">[OutAttribute] The dot product of the two vectors.</param>
        public static void Dot(ref Vector2D value1, ref Vector2D value2, out double result)
        {
            result = (double)((double)value1.X * (double)value2.X + (double)value1.Y * (double)value2.Y);
        }

        /// <summary>
        /// Turns the current vector into a unit vector. The result is a vector one unit in length pointing in the same direction as the original vector.
        /// </summary>
        public void Normalize()
        {
            double num = 1f / (double)Math.Sqrt((double)this.X * (double)this.X + (double)this.Y * (double)this.Y);
            this.X *= num;
            this.Y *= num;
        }

        /// <summary>
        /// Creates a unit vector from the specified vector. The result is a vector one unit in length pointing in the same direction as the original vector.
        /// </summary>
        /// <param name="value">Source Vector2D.</param>
        public static Vector2D Normalize(Vector2D value)
        {
            double num = 1f / (double)Math.Sqrt((double)value.X * (double)value.X + (double)value.Y * (double)value.Y);
            Vector2D vector2D;
            vector2D.X = value.X * num;
            vector2D.Y = value.Y * num;
            return vector2D;
        }

        /// <summary>
        /// Creates a unit vector from the specified vector, writing the result to a user-specified variable. The result is a vector one unit in length pointing in the same direction as the original vector.
        /// </summary>
        /// <param name="value">Source vector.</param><param name="result">[OutAttribute] Normalized vector.</param>
        public static void Normalize(ref Vector2D value, out Vector2D result)
        {
            double num = 1f / (double)Math.Sqrt((double)value.X * (double)value.X + (double)value.Y * (double)value.Y);
            result.X = value.X * num;
            result.Y = value.Y * num;
        }

        /// <summary>
        /// Determines the reflect vector of the given vector and normal.
        /// </summary>
        /// <param name="vector">Source vector.</param><param name="normal">Normal of vector.</param>
        public static Vector2D Reflect(Vector2D vector, Vector2D normal)
        {
            double num = (double)((double)vector.X * (double)normal.X + (double)vector.Y * (double)normal.Y);
            Vector2D vector2D;
            vector2D.X = vector.X - 2f * num * normal.X;
            vector2D.Y = vector.Y - 2f * num * normal.Y;
            return vector2D;
        }

        /// <summary>
        /// Determines the reflect vector of the given vector and normal.
        /// </summary>
        /// <param name="vector">Source vector.</param><param name="normal">Normal of vector.</param><param name="result">[OutAttribute] The created reflect vector.</param>
        public static void Reflect(ref Vector2D vector, ref Vector2D normal, out Vector2D result)
        {
            double num = (double)((double)vector.X * (double)normal.X + (double)vector.Y * (double)normal.Y);
            result.X = vector.X - 2f * num * normal.X;
            result.Y = vector.Y - 2f * num * normal.Y;
        }

        /// <summary>
        /// Returns a vector that contains the lowest value from each matching pair of components.
        /// </summary>
        /// <param name="value1">Source vector.</param><param name="value2">Source vector.</param>
        public static Vector2D Min(Vector2D value1, Vector2D value2)
        {
            Vector2D vector2D;
            vector2D.X = (double)value1.X < (double)value2.X ? value1.X : value2.X;
            vector2D.Y = (double)value1.Y < (double)value2.Y ? value1.Y : value2.Y;
            return vector2D;
        }

        /// <summary>
        /// Returns a vector that contains the lowest value from each matching pair of components.
        /// </summary>
        /// <param name="value1">Source vector.</param><param name="value2">Source vector.</param><param name="result">[OutAttribute] The minimized vector.</param>
        public static void Min(ref Vector2D value1, ref Vector2D value2, out Vector2D result)
        {
            result.X = (double)value1.X < (double)value2.X ? value1.X : value2.X;
            result.Y = (double)value1.Y < (double)value2.Y ? value1.Y : value2.Y;
        }

        /// <summary>
        /// Returns a vector that contains the highest value from each matching pair of components.
        /// </summary>
        /// <param name="value1">Source vector.</param><param name="value2">Source vector.</param>
        public static Vector2D Max(Vector2D value1, Vector2D value2)
        {
            Vector2D vector2D;
            vector2D.X = (double)value1.X > (double)value2.X ? value1.X : value2.X;
            vector2D.Y = (double)value1.Y > (double)value2.Y ? value1.Y : value2.Y;
            return vector2D;
        }

        /// <summary>
        /// Returns a vector that contains the highest value from each matching pair of components.
        /// </summary>
        /// <param name="value1">Source vector.</param><param name="value2">Source vector.</param><param name="result">[OutAttribute] The maximized vector.</param>
        public static void Max(ref Vector2D value1, ref Vector2D value2, out Vector2D result)
        {
            result.X = (double)value1.X > (double)value2.X ? value1.X : value2.X;
            result.Y = (double)value1.Y > (double)value2.Y ? value1.Y : value2.Y;
        }

        /// <summary>
        /// Restricts a value to be within a specified range.
        /// </summary>
        /// <param name="value1">The value to clamp.</param><param name="min">The minimum value.</param><param name="max">The maximum value.</param>
        public static Vector2D Clamp(Vector2D value1, Vector2D min, Vector2D max)
        {
            double num1 = value1.X;
            double num2 = (double)num1 > (double)max.X ? max.X : num1;
            double num3 = (double)num2 < (double)min.X ? min.X : num2;
            double num4 = value1.Y;
            double num5 = (double)num4 > (double)max.Y ? max.Y : num4;
            double num6 = (double)num5 < (double)min.Y ? min.Y : num5;
            Vector2D vector2D;
            vector2D.X = num3;
            vector2D.Y = num6;
            return vector2D;
        }

        /// <summary>
        /// Restricts a value to be within a specified range.
        /// </summary>
        /// <param name="value1">The value to clamp.</param><param name="min">The minimum value.</param><param name="max">The maximum value.</param><param name="result">[OutAttribute] The clamped value.</param>
        public static void Clamp(ref Vector2D value1, ref Vector2D min, ref Vector2D max, out Vector2D result)
        {
            double num1 = value1.X;
            double num2 = (double)num1 > (double)max.X ? max.X : num1;
            double num3 = (double)num2 < (double)min.X ? min.X : num2;
            double num4 = value1.Y;
            double num5 = (double)num4 > (double)max.Y ? max.Y : num4;
            double num6 = (double)num5 < (double)min.Y ? min.Y : num5;
            result.X = num3;
            result.Y = num6;
        }

        public static Vector2D ClampToSphere(Vector2D vector, double radius)
        {
            double lsq = vector.LengthSquared();
            double rsq = radius * radius;
            if (lsq > rsq)
            {
                return vector * (double)Math.Sqrt(rsq / lsq);
            }
            return vector;
        }

        public static void ClampToSphere(ref Vector2D vector, double radius)
        {
            double lsq = vector.LengthSquared();
            double rsq = radius * radius;
            if (lsq > rsq)
            {
                vector *= (double)Math.Sqrt(rsq / lsq);
            }
        }

        /// <summary>
        /// Performs a linear interpolation between two vectors.
        /// </summary>
        /// <param name="value1">Source vector.</param><param name="value2">Source vector.</param><param name="amount">Value between 0 and 1 indicating the weight of value2.</param>
        public static Vector2D Lerp(Vector2D value1, Vector2D value2, double amount)
        {
            Vector2D vector2D;
            vector2D.X = value1.X + (value2.X - value1.X) * amount;
            vector2D.Y = value1.Y + (value2.Y - value1.Y) * amount;
            return vector2D;
        }

        /// <summary>
        /// Performs a linear interpolation between two vectors.
        /// </summary>
        /// <param name="value1">Source vector.</param><param name="value2">Source vector.</param><param name="amount">Value between 0 and 1 indicating the weight of value2.</param><param name="result">[OutAttribute] The result of the interpolation.</param>
        public static void Lerp(ref Vector2D value1, ref Vector2D value2, double amount, out Vector2D result)
        {
            result.X = value1.X + (value2.X - value1.X) * amount;
            result.Y = value1.Y + (value2.Y - value1.Y) * amount;
        }

        /// <summary>
        /// Returns a Vector2D containing the 2D Cartesian coordinates of a point specified in barycentric (areal) coordinates relative to a 2D triangle.
        /// </summary>
        /// <param name="value1">A Vector2D containing the 2D Cartesian coordinates of vertex 1 of the triangle.</param><param name="value2">A Vector2D containing the 2D Cartesian coordinates of vertex 2 of the triangle.</param><param name="value3">A Vector2D containing the 2D Cartesian coordinates of vertex 3 of the triangle.</param><param name="amount1">Barycentric coordinate b2, which expresses the weighting factor toward vertex 2 (specified in value2).</param><param name="amount2">Barycentric coordinate b3, which expresses the weighting factor toward vertex 3 (specified in value3).</param>
        public static Vector2D Barycentric(Vector2D value1, Vector2D value2, Vector2D value3, double amount1, double amount2)
        {
            Vector2D vector2D;
            vector2D.X = (double)((double)value1.X + (double)amount1 * ((double)value2.X - (double)value1.X) + (double)amount2 * ((double)value3.X - (double)value1.X));
            vector2D.Y = (double)((double)value1.Y + (double)amount1 * ((double)value2.Y - (double)value1.Y) + (double)amount2 * ((double)value3.Y - (double)value1.Y));
            return vector2D;
        }

        /// <summary>
        /// Returns a Vector2D containing the 2D Cartesian coordinates of a point specified in barycentric (areal) coordinates relative to a 2D triangle.
        /// </summary>
        /// <param name="value1">A Vector2D containing the 2D Cartesian coordinates of vertex 1 of the triangle.</param><param name="value2">A Vector2D containing the 2D Cartesian coordinates of vertex 2 of the triangle.</param><param name="value3">A Vector2D containing the 2D Cartesian coordinates of vertex 3 of the triangle.</param><param name="amount1">Barycentric coordinate b2, which expresses the weighting factor toward vertex 2 (specified in value2).</param><param name="amount2">Barycentric coordinate b3, which expresses the weighting factor toward vertex 3 (specified in value3).</param><param name="result">[OutAttribute] The 2D Cartesian coordinates of the specified point are placed in this Vector2D on exit.</param>
        public static void Barycentric(ref Vector2D value1, ref Vector2D value2, ref Vector2D value3, double amount1, double amount2, out Vector2D result)
        {
            result.X = (double)((double)value1.X + (double)amount1 * ((double)value2.X - (double)value1.X) + (double)amount2 * ((double)value3.X - (double)value1.X));
            result.Y = (double)((double)value1.Y + (double)amount1 * ((double)value2.Y - (double)value1.Y) + (double)amount2 * ((double)value3.Y - (double)value1.Y));
        }

        /// <summary>
        /// Interpolates between two values using a cubic equation.
        /// </summary>
        /// <param name="value1">Source value.</param><param name="value2">Source value.</param><param name="amount">Weighting value.</param>
        public static Vector2D SmoothStep(Vector2D value1, Vector2D value2, double amount)
        {
            amount = (double)amount > 1.0 ? 1f : ((double)amount < 0.0 ? 0.0f : amount);
            amount = (double)((double)amount * (double)amount * (3.0 - 2.0 * (double)amount));
            Vector2D vector2D;
            vector2D.X = value1.X + (value2.X - value1.X) * amount;
            vector2D.Y = value1.Y + (value2.Y - value1.Y) * amount;
            return vector2D;
        }

        /// <summary>
        /// Interpolates between two values using a cubic equation.
        /// </summary>
        /// <param name="value1">Source value.</param><param name="value2">Source value.</param><param name="amount">Weighting value.</param><param name="result">[OutAttribute] The interpolated value.</param>
        public static void SmoothStep(ref Vector2D value1, ref Vector2D value2, double amount, out Vector2D result)
        {
            amount = (double)amount > 1.0 ? 1f : ((double)amount < 0.0 ? 0.0f : amount);
            amount = (double)((double)amount * (double)amount * (3.0 - 2.0 * (double)amount));
            result.X = value1.X + (value2.X - value1.X) * amount;
            result.Y = value1.Y + (value2.Y - value1.Y) * amount;
        }

        /// <summary>
        /// Performs a Catmull-Rom interpolation using the specified positions.
        /// </summary>
        /// <param name="value1">The first position in the interpolation.</param><param name="value2">The second position in the interpolation.</param><param name="value3">The third position in the interpolation.</param><param name="value4">The fourth position in the interpolation.</param><param name="amount">Weighting factor.</param>
        public static Vector2D CatmullRom(Vector2D value1, Vector2D value2, Vector2D value3, Vector2D value4, double amount)
        {
            double num1 = amount * amount;
            double num2 = amount * num1;
            Vector2D vector2D;
            vector2D.X = (double)(0.5 * (2.0 * (double)value2.X + (-(double)value1.X + (double)value3.X) * (double)amount + (2.0 * (double)value1.X - 5.0 * (double)value2.X + 4.0 * (double)value3.X - (double)value4.X) * (double)num1 + (-(double)value1.X + 3.0 * (double)value2.X - 3.0 * (double)value3.X + (double)value4.X) * (double)num2));
            vector2D.Y = (double)(0.5 * (2.0 * (double)value2.Y + (-(double)value1.Y + (double)value3.Y) * (double)amount + (2.0 * (double)value1.Y - 5.0 * (double)value2.Y + 4.0 * (double)value3.Y - (double)value4.Y) * (double)num1 + (-(double)value1.Y + 3.0 * (double)value2.Y - 3.0 * (double)value3.Y + (double)value4.Y) * (double)num2));
            return vector2D;
        }

        /// <summary>
        /// Performs a Catmull-Rom interpolation using the specified positions.
        /// </summary>
        /// <param name="value1">The first position in the interpolation.</param><param name="value2">The second position in the interpolation.</param><param name="value3">The third position in the interpolation.</param><param name="value4">The fourth position in the interpolation.</param><param name="amount">Weighting factor.</param><param name="result">[OutAttribute] A vector that is the result of the Catmull-Rom interpolation.</param>
        public static void CatmullRom(ref Vector2D value1, ref Vector2D value2, ref Vector2D value3, ref Vector2D value4, double amount, out Vector2D result)
        {
            double num1 = amount * amount;
            double num2 = amount * num1;
            result.X = (double)(0.5 * (2.0 * (double)value2.X + (-(double)value1.X + (double)value3.X) * (double)amount + (2.0 * (double)value1.X - 5.0 * (double)value2.X + 4.0 * (double)value3.X - (double)value4.X) * (double)num1 + (-(double)value1.X + 3.0 * (double)value2.X - 3.0 * (double)value3.X + (double)value4.X) * (double)num2));
            result.Y = (double)(0.5 * (2.0 * (double)value2.Y + (-(double)value1.Y + (double)value3.Y) * (double)amount + (2.0 * (double)value1.Y - 5.0 * (double)value2.Y + 4.0 * (double)value3.Y - (double)value4.Y) * (double)num1 + (-(double)value1.Y + 3.0 * (double)value2.Y - 3.0 * (double)value3.Y + (double)value4.Y) * (double)num2));
        }

        /// <summary>
        /// Performs a Hermite spline interpolation.
        /// </summary>
        /// <param name="value1">Source position vector.</param><param name="tangent1">Source tangent vector.</param><param name="value2">Source position vector.</param><param name="tangent2">Source tangent vector.</param><param name="amount">Weighting factor.</param>
        public static Vector2D Hermite(Vector2D value1, Vector2D tangent1, Vector2D value2, Vector2D tangent2, double amount)
        {
            double num1 = amount * amount;
            double num2 = amount * num1;
            double num3 = (double)(2.0 * (double)num2 - 3.0 * (double)num1 + 1.0);
            double num4 = (double)(-2.0 * (double)num2 + 3.0 * (double)num1);
            double num5 = num2 - 2f * num1 + amount;
            double num6 = num2 - num1;
            Vector2D vector2D;
            vector2D.X = (double)((double)value1.X * (double)num3 + (double)value2.X * (double)num4 + (double)tangent1.X * (double)num5 + (double)tangent2.X * (double)num6);
            vector2D.Y = (double)((double)value1.Y * (double)num3 + (double)value2.Y * (double)num4 + (double)tangent1.Y * (double)num5 + (double)tangent2.Y * (double)num6);
            return vector2D;
        }

        /// <summary>
        /// Performs a Hermite spline interpolation.
        /// </summary>
        /// <param name="value1">Source position vector.</param><param name="tangent1">Source tangent vector.</param><param name="value2">Source position vector.</param><param name="tangent2">Source tangent vector.</param><param name="amount">Weighting factor.</param><param name="result">[OutAttribute] The result of the Hermite spline interpolation.</param>
        public static void Hermite(ref Vector2D value1, ref Vector2D tangent1, ref Vector2D value2, ref Vector2D tangent2, double amount, out Vector2D result)
        {
            double num1 = amount * amount;
            double num2 = amount * num1;
            double num3 = (double)(2.0 * (double)num2 - 3.0 * (double)num1 + 1.0);
            double num4 = (double)(-2.0 * (double)num2 + 3.0 * (double)num1);
            double num5 = num2 - 2f * num1 + amount;
            double num6 = num2 - num1;
            result.X = (double)((double)value1.X * (double)num3 + (double)value2.X * (double)num4 + (double)tangent1.X * (double)num5 + (double)tangent2.X * (double)num6);
            result.Y = (double)((double)value1.Y * (double)num3 + (double)value2.Y * (double)num4 + (double)tangent1.Y * (double)num5 + (double)tangent2.Y * (double)num6);
        }

        /// <summary>
        /// Transforms the vector (x, y, 0, 1) by the specified matrix.
        /// </summary>
        /// <param name="position">The source vector.</param><param name="matrix">The transformation matrix.</param>
        public static Vector2D Transform(Vector2D position, Matrix matrix)
        {
            double num1 = (double)((double)position.X * (double)matrix.M11 + (double)position.Y * (double)matrix.M21) + matrix.M41;
            double num2 = (double)((double)position.X * (double)matrix.M12 + (double)position.Y * (double)matrix.M22) + matrix.M42;
            Vector2D vector2D;
            vector2D.X = num1;
            vector2D.Y = num2;
            return vector2D;
        }

        /// <summary>
        /// Transforms a Vector2D by the given Matrix.
        /// </summary>
        /// <param name="position">The source Vector2D.</param><param name="matrix">The transformation Matrix.</param><param name="result">[OutAttribute] The Vector2D resulting from the transformation.</param>
        public static void Transform(ref Vector2D position, ref Matrix matrix, out Vector2D result)
        {
            double num1 = (double)((double)position.X * (double)matrix.M11 + (double)position.Y * (double)matrix.M21) + matrix.M41;
            double num2 = (double)((double)position.X * (double)matrix.M12 + (double)position.Y * (double)matrix.M22) + matrix.M42;
            result.X = num1;
            result.Y = num2;
        }

        /// <summary>
        /// Transforms a 2D vector normal by a matrix.
        /// </summary>
        /// <param name="normal">The source vector.</param><param name="matrix">The transformation matrix.</param>
        public static Vector2D TransformNormal(Vector2D normal, Matrix matrix)
        {
            double num1 = (double)((double)normal.X * (double)matrix.M11 + (double)normal.Y * (double)matrix.M21);
            double num2 = (double)((double)normal.X * (double)matrix.M12 + (double)normal.Y * (double)matrix.M22);
            Vector2D vector2D;
            vector2D.X = num1;
            vector2D.Y = num2;
            return vector2D;
        }

        /// <summary>
        /// Transforms a vector normal by a matrix.
        /// </summary>
        /// <param name="normal">The source vector.</param><param name="matrix">The transformation matrix.</param><param name="result">[OutAttribute] The Vector2D resulting from the transformation.</param>
        public static void TransformNormal(ref Vector2D normal, ref Matrix matrix, out Vector2D result)
        {
            double num1 = (double)((double)normal.X * (double)matrix.M11 + (double)normal.Y * (double)matrix.M21);
            double num2 = (double)((double)normal.X * (double)matrix.M12 + (double)normal.Y * (double)matrix.M22);
            result.X = num1;
            result.Y = num2;
        }

        /// <summary>
        /// Transforms a single Vector2D, or the vector normal (x, y, 0, 0), by a specified Quaternion rotation.
        /// </summary>
        /// <param name="value">The vector to rotate.</param><param name="rotation">The Quaternion rotation to apply.</param>
        public static Vector2D Transform(Vector2D value, Quaternion rotation)
        {
            double num1 = rotation.X + rotation.X;
            double num2 = rotation.Y + rotation.Y;
            double num3 = rotation.Z + rotation.Z;
            double num4 = rotation.W * num3;
            double num5 = rotation.X * num1;
            double num6 = rotation.X * num2;
            double num7 = rotation.Y * num2;
            double num8 = rotation.Z * num3;
            double num9 = (double)((double)value.X * (1.0 - (double)num7 - (double)num8) + (double)value.Y * ((double)num6 - (double)num4));
            double num10 = (double)((double)value.X * ((double)num6 + (double)num4) + (double)value.Y * (1.0 - (double)num5 - (double)num8));
            Vector2D vector2D;
            vector2D.X = num9;
            vector2D.Y = num10;
            return vector2D;
        }

        /// <summary>
        /// Transforms a Vector2D, or the vector normal (x, y, 0, 0), by a specified Quaternion rotation.
        /// </summary>
        /// <param name="value">The vector to rotate.</param><param name="rotation">The Quaternion rotation to apply.</param><param name="result">[OutAttribute] An existing Vector2D filled in with the result of the rotation.</param>
        public static void Transform(ref Vector2D value, ref Quaternion rotation, out Vector2D result)
        {
            double num1 = rotation.X + rotation.X;
            double num2 = rotation.Y + rotation.Y;
            double num3 = rotation.Z + rotation.Z;
            double num4 = rotation.W * num3;
            double num5 = rotation.X * num1;
            double num6 = rotation.X * num2;
            double num7 = rotation.Y * num2;
            double num8 = rotation.Z * num3;
            double num9 = (double)((double)value.X * (1.0 - (double)num7 - (double)num8) + (double)value.Y * ((double)num6 - (double)num4));
            double num10 = (double)((double)value.X * ((double)num6 + (double)num4) + (double)value.Y * (1.0 - (double)num5 - (double)num8));
            result.X = num9;
            result.Y = num10;
        }

        /// <summary>
        /// Transforms an array of Vector2s by a specified Matrix.
        /// </summary>
        /// <param name="sourceArray">The array of Vector2s to transform.</param><param name="matrix">The transform Matrix to apply.</param><param name="destinationArray">An existing array into which the transformed Vector2s are written.</param>
        public static void Transform(Vector2D[] sourceArray, ref Matrix matrix, Vector2D[] destinationArray)
        {
            for (int index = 0; index < sourceArray.Length; ++index)
            {
                double num1 = sourceArray[index].X;
                double num2 = sourceArray[index].Y;
                destinationArray[index].X = (double)((double)num1 * (double)matrix.M11 + (double)num2 * (double)matrix.M21) + matrix.M41;
                destinationArray[index].Y = (double)((double)num1 * (double)matrix.M12 + (double)num2 * (double)matrix.M22) + matrix.M42;
            }
        }

        /// <summary>
        /// Transforms a specified range in an array of Vector2s by a specified Matrix and places the results in a specified range in a destination array.
        /// </summary>
        /// <param name="sourceArray">The source array.</param><param name="sourceIndex">The index of the first Vector2D to transform in the source array.</param><param name="matrix">The Matrix to transform by.</param><param name="destinationArray">The destination array into which the resulting Vector2s will be written.</param><param name="destinationIndex">The index of the position in the destination array where the first result Vector2D should be written.</param><param name="length">The number of Vector2s to be transformed.</param>
        public static void Transform(Vector2D[] sourceArray, int sourceIndex, ref Matrix matrix, Vector2D[] destinationArray, int destinationIndex, int length)
        {
            for (; length > 0; --length)
            {
                double num1 = sourceArray[sourceIndex].X;
                double num2 = sourceArray[sourceIndex].Y;
                destinationArray[destinationIndex].X = (double)((double)num1 * (double)matrix.M11 + (double)num2 * (double)matrix.M21) + matrix.M41;
                destinationArray[destinationIndex].Y = (double)((double)num1 * (double)matrix.M12 + (double)num2 * (double)matrix.M22) + matrix.M42;
                ++sourceIndex;
                ++destinationIndex;
            }
        }

        /// <summary>
        /// Transforms an array of Vector2D vector normals by a specified Matrix.
        /// </summary>
        /// <param name="sourceArray">The array of vector normals to transform.</param><param name="matrix">The transform Matrix to apply.</param><param name="destinationArray">An existing array into which the transformed vector normals are written.</param>
        public static void TransformNormal(Vector2D[] sourceArray, ref Matrix matrix, Vector2D[] destinationArray)
        {
            for (int index = 0; index < sourceArray.Length; ++index)
            {
                double num1 = sourceArray[index].X;
                double num2 = sourceArray[index].Y;
                destinationArray[index].X = (double)((double)num1 * (double)matrix.M11 + (double)num2 * (double)matrix.M21);
                destinationArray[index].Y = (double)((double)num1 * (double)matrix.M12 + (double)num2 * (double)matrix.M22);
            }
        }

        /// <summary>
        /// Transforms a specified range in an array of Vector2D vector normals by a specified Matrix and places the results in a specified range in a destination array.
        /// </summary>
        /// <param name="sourceArray">The source array.</param><param name="sourceIndex">The index of the first Vector2D to transform in the source array.</param><param name="matrix">The Matrix to apply.</param><param name="destinationArray">The destination array into which the resulting Vector2s are written.</param><param name="destinationIndex">The index of the position in the destination array where the first result Vector2D should be written.</param><param name="length">The number of vector normals to be transformed.</param>
        public static void TransformNormal(Vector2D[] sourceArray, int sourceIndex, ref Matrix matrix, Vector2D[] destinationArray, int destinationIndex, int length)
        {
            for (; length > 0; --length)
            {
                double num1 = sourceArray[sourceIndex].X;
                double num2 = sourceArray[sourceIndex].Y;
                destinationArray[destinationIndex].X = (double)((double)num1 * (double)matrix.M11 + (double)num2 * (double)matrix.M21);
                destinationArray[destinationIndex].Y = (double)((double)num1 * (double)matrix.M12 + (double)num2 * (double)matrix.M22);
                ++sourceIndex;
                ++destinationIndex;
            }
        }

        /// <summary>
        /// Transforms an array of Vector2s by a specified Quaternion.
        /// </summary>
        /// <param name="sourceArray">The array of Vector2s to transform.</param><param name="rotation">The transform Matrix to use.</param><param name="destinationArray">An existing array into which the transformed Vector2s are written.</param>
        public static void Transform(Vector2D[] sourceArray, ref Quaternion rotation, Vector2D[] destinationArray)
        {
            double num1 = rotation.X + rotation.X;
            double num2 = rotation.Y + rotation.Y;
            double num3 = rotation.Z + rotation.Z;
            double num4 = rotation.W * num3;
            double num5 = rotation.X * num1;
            double num6 = rotation.X * num2;
            double num7 = rotation.Y * num2;
            double num8 = rotation.Z * num3;
            double num9 = 1f - num7 - num8;
            double num10 = num6 - num4;
            double num11 = num6 + num4;
            double num12 = 1f - num5 - num8;
            for (int index = 0; index < sourceArray.Length; ++index)
            {
                double num13 = sourceArray[index].X;
                double num14 = sourceArray[index].Y;
                destinationArray[index].X = (double)((double)num13 * (double)num9 + (double)num14 * (double)num10);
                destinationArray[index].Y = (double)((double)num13 * (double)num11 + (double)num14 * (double)num12);
            }
        }

        /// <summary>
        /// Transforms a specified range in an array of Vector2s by a specified Quaternion and places the results in a specified range in a destination array.
        /// </summary>
        /// <param name="sourceArray">The source array.</param><param name="sourceIndex">The index of the first Vector2D to transform in the source array.</param><param name="rotation">The Quaternion rotation to apply.</param><param name="destinationArray">The destination array into which the resulting Vector2s are written.</param><param name="destinationIndex">The index of the position in the destination array where the first result Vector2D should be written.</param><param name="length">The number of Vector2s to be transformed.</param>
        public static void Transform(Vector2D[] sourceArray, int sourceIndex, ref Quaternion rotation, Vector2D[] destinationArray, int destinationIndex, int length)
        {
            double num1 = rotation.X + rotation.X;
            double num2 = rotation.Y + rotation.Y;
            double num3 = rotation.Z + rotation.Z;
            double num4 = rotation.W * num3;
            double num5 = rotation.X * num1;
            double num6 = rotation.X * num2;
            double num7 = rotation.Y * num2;
            double num8 = rotation.Z * num3;
            double num9 = 1f - num7 - num8;
            double num10 = num6 - num4;
            double num11 = num6 + num4;
            double num12 = 1f - num5 - num8;
            for (; length > 0; --length)
            {
                double num13 = sourceArray[sourceIndex].X;
                double num14 = sourceArray[sourceIndex].Y;
                destinationArray[destinationIndex].X = (double)((double)num13 * (double)num9 + (double)num14 * (double)num10);
                destinationArray[destinationIndex].Y = (double)((double)num13 * (double)num11 + (double)num14 * (double)num12);
                ++sourceIndex;
                ++destinationIndex;
            }
        }

        /// <summary>
        /// Returns a vector pointing in the opposite direction.
        /// </summary>
        /// <param name="value">Source vector.</param>
        public static Vector2D Negate(Vector2D value)
        {
            Vector2D vector2D;
            vector2D.X = -value.X;
            vector2D.Y = -value.Y;
            return vector2D;
        }

        /// <summary>
        /// Returns a vector pointing in the opposite direction.
        /// </summary>
        /// <param name="value">Source vector.</param><param name="result">[OutAttribute] Vector pointing in the opposite direction.</param>
        public static void Negate(ref Vector2D value, out Vector2D result)
        {
            result.X = -value.X;
            result.Y = -value.Y;
        }

        /// <summary>
        /// Adds two vectors.
        /// </summary>
        /// <param name="value1">Source vector.</param><param name="value2">Source vector.</param>
        public static Vector2D Add(Vector2D value1, Vector2D value2)
        {
            Vector2D vector2D;
            vector2D.X = value1.X + value2.X;
            vector2D.Y = value1.Y + value2.Y;
            return vector2D;
        }

        /// <summary>
        /// Adds two vectors.
        /// </summary>
        /// <param name="value1">Source vector.</param><param name="value2">Source vector.</param><param name="result">[OutAttribute] Sum of the source vectors.</param>
        public static void Add(ref Vector2D value1, ref Vector2D value2, out Vector2D result)
        {
            result.X = value1.X + value2.X;
            result.Y = value1.Y + value2.Y;
        }

        /// <summary>
        /// Subtracts a vector from a vector.
        /// </summary>
        /// <param name="value1">Source vector.</param><param name="value2">Source vector.</param>
        public static Vector2D Subtract(Vector2D value1, Vector2D value2)
        {
            Vector2D vector2D;
            vector2D.X = value1.X - value2.X;
            vector2D.Y = value1.Y - value2.Y;
            return vector2D;
        }

        /// <summary>
        /// Subtracts a vector from a vector.
        /// </summary>
        /// <param name="value1">Source vector.</param><param name="value2">Source vector.</param><param name="result">[OutAttribute] The result of the subtraction.</param>
        public static void Subtract(ref Vector2D value1, ref Vector2D value2, out Vector2D result)
        {
            result.X = value1.X - value2.X;
            result.Y = value1.Y - value2.Y;
        }

        /// <summary>
        /// Multiplies the components of two vectors by each other.
        /// </summary>
        /// <param name="value1">Source vector.</param><param name="value2">Source vector.</param>
        public static Vector2D Multiply(Vector2D value1, Vector2D value2)
        {
            Vector2D vector2D;
            vector2D.X = value1.X * value2.X;
            vector2D.Y = value1.Y * value2.Y;
            return vector2D;
        }

        /// <summary>
        /// Multiplies the components of two vectors by each other.
        /// </summary>
        /// <param name="value1">Source vector.</param><param name="value2">Source vector.</param><param name="result">[OutAttribute] The result of the multiplication.</param>
        public static void Multiply(ref Vector2D value1, ref Vector2D value2, out Vector2D result)
        {
            result.X = value1.X * value2.X;
            result.Y = value1.Y * value2.Y;
        }

        /// <summary>
        /// Multiplies a vector by a scalar value.
        /// </summary>
        /// <param name="value1">Source vector.</param><param name="scaleFactor">Scalar value.</param>
        public static Vector2D Multiply(Vector2D value1, double scaleFactor)
        {
            Vector2D vector2D;
            vector2D.X = value1.X * scaleFactor;
            vector2D.Y = value1.Y * scaleFactor;
            return vector2D;
        }

        /// <summary>
        /// Multiplies a vector by a scalar value.
        /// </summary>
        /// <param name="value1">Source vector.</param><param name="scaleFactor">Scalar value.</param><param name="result">[OutAttribute] The result of the multiplication.</param>
        public static void Multiply(ref Vector2D value1, double scaleFactor, out Vector2D result)
        {
            result.X = value1.X * scaleFactor;
            result.Y = value1.Y * scaleFactor;
        }

        /// <summary>
        /// Divides the components of a vector by the components of another vector.
        /// </summary>
        /// <param name="value1">Source vector.</param><param name="value2">Divisor vector.</param>
        public static Vector2D Divide(Vector2D value1, Vector2D value2)
        {
            Vector2D vector2D;
            vector2D.X = value1.X / value2.X;
            vector2D.Y = value1.Y / value2.Y;
            return vector2D;
        }

        /// <summary>
        /// Divides the components of a vector by the components of another vector.
        /// </summary>
        /// <param name="value1">Source vector.</param><param name="value2">The divisor.</param><param name="result">[OutAttribute] The result of the division.</param>
        public static void Divide(ref Vector2D value1, ref Vector2D value2, out Vector2D result)
        {
            result.X = value1.X / value2.X;
            result.Y = value1.Y / value2.Y;
        }

        /// <summary>
        /// Divides a vector by a scalar value.
        /// </summary>
        /// <param name="value1">Source vector.</param><param name="divider">The divisor.</param>
        public static Vector2D Divide(Vector2D value1, double divider)
        {
            double num = 1f / divider;
            Vector2D vector2D;
            vector2D.X = value1.X * num;
            vector2D.Y = value1.Y * num;
            return vector2D;
        }

        /// <summary>
        /// Divides a vector by a scalar value.
        /// </summary>
        /// <param name="value1">Source vector.</param><param name="divider">The divisor.</param><param name="result">[OutAttribute] The result of the division.</param>
        public static void Divide(ref Vector2D value1, double divider, out Vector2D result)
        {
            double num = 1f / divider;
            result.X = value1.X * num;
            result.Y = value1.Y * num;
        }

        public bool Between(ref Vector2D start, ref Vector2D end)
        {
            return X >= start.X && X <= end.X || Y >= start.Y && Y <= end.Y;
        }

        public static Vector2D Floor(Vector2D position)
        {
            return new Vector2D((double)Math.Floor(position.X), (double)Math.Floor(position.Y));
        }

        public void Rotate(double angle)
        {
            double tmpX = X;
            X = X * (double)Math.Cos(angle) - Y * (double)Math.Sin(angle);
            Y = Y * (double)Math.Cos(angle) + tmpX * (double)Math.Sin(angle);
        }
    }
}
