using System;
using System.Globalization;

namespace VRageMath
{
    /// <summary>
    /// Defines a vector with four components.
    /// </summary>
    [ProtoBuf.ProtoContract, Serializable]
    public struct Vector4D : IEquatable<Vector4>
    {
        public static Vector4D Zero = new Vector4D();
        public static Vector4D One = new Vector4D(1, 1, 1, 1);
        public static Vector4D UnitX = new Vector4D(1, 0.0, 0.0, 0.0);
        public static Vector4D UnitY = new Vector4D(0.0, 1, 0.0, 0.0);
        public static Vector4D UnitZ = new Vector4D(0.0, 0.0, 1, 0.0);
        public static Vector4D UnitW = new Vector4D(0.0, 0.0, 0.0, 1);
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
        /// <summary>
        /// Gets or sets the z-component of the vector.
        /// </summary>
        [ProtoBuf.ProtoMember]
        public double Z;
        /// <summary>
        /// Gets or sets the w-component of the vector.
        /// </summary>
        [ProtoBuf.ProtoMember]
        public double W;

        static Vector4D()
        {
        }

        /// <summary>
        /// Initializes a new instance of Vector4.
        /// </summary>
        /// <param name="x">Initial value for the x-component of the vector.</param><param name="y">Initial value for the y-component of the vector.</param><param name="z">Initial value for the z-component of the vector.</param><param name="w">Initial value for the w-component of the vector.</param>
        public Vector4D(double x, double y, double z, double w)
        {
            this.X = x;
            this.Y = y;
            this.Z = z;
            this.W = w;
        }

        /// <summary>
        /// Initializes a new instance of Vector4.
        /// </summary>
        /// <param name="value">A vector containing the values to initialize x and y components with.</param><param name="z">Initial value for the z-component of the vector.</param><param name="w">Initial value for the w-component of the vector.</param>
        public Vector4D(Vector2 value, double z, double w)
        {
            this.X = value.X;
            this.Y = value.Y;
            this.Z = z;
            this.W = w;
        }

        /// <summary>
        /// Initializes a new instance of Vector4.
        /// </summary>
        /// <param name="value">A vector containing the values to initialize x, y, and z components with.</param><param name="w">Initial value for the w-component of the vector.</param>
        public Vector4D(Vector3D value, double w)
        {
            this.X = value.X;
            this.Y = value.Y;
            this.Z = value.Z;
            this.W = w;
        }

        /// <summary>
        /// Creates a new instance of Vector4.
        /// </summary>
        /// <param name="value">Value to initialize each component to.</param>
        public Vector4D(double value)
        {
            this.X = this.Y = this.Z = this.W = value;
        }

        /// <summary>
        /// Returns a vector pointing in the opposite direction.
        /// </summary>
        /// <param name="value">Source vector.</param>
        public static Vector4D operator -(Vector4D value)
        {
            Vector4D vector4;
            vector4.X = -value.X;
            vector4.Y = -value.Y;
            vector4.Z = -value.Z;
            vector4.W = -value.W;
            return vector4;
        }

        /// <summary>
        /// Tests vectors for equality.
        /// </summary>
        /// <param name="value1">Source vector.</param><param name="value2">Source vector.</param>
        public static bool operator ==(Vector4D value1, Vector4D value2)
        {
            if ((double)value1.X == (double)value2.X && (double)value1.Y == (double)value2.Y && (double)value1.Z == (double)value2.Z)
                return (double)value1.W == (double)value2.W;
            else
                return false;
        }

        /// <summary>
        /// Tests vectors for inequality.
        /// </summary>
        /// <param name="value1">Vector to compare.</param><param name="value2">Vector to compare.</param>
        public static bool operator !=(Vector4D value1, Vector4D value2)
        {
            if ((double)value1.X == (double)value2.X && (double)value1.Y == (double)value2.Y && (double)value1.Z == (double)value2.Z)
                return (double)value1.W != (double)value2.W;
            else
                return true;
        }

        /// <summary>
        /// Adds two vectors.
        /// </summary>
        /// <param name="value1">Source vector.</param><param name="value2">Source vector.</param>
        public static Vector4D operator +(Vector4D value1, Vector4D value2)
        {
            Vector4D vector4;
            vector4.X = value1.X + value2.X;
            vector4.Y = value1.Y + value2.Y;
            vector4.Z = value1.Z + value2.Z;
            vector4.W = value1.W + value2.W;
            return vector4;
        }

        /// <summary>
        /// Subtracts a vector from a vector.
        /// </summary>
        /// <param name="value1">Source vector.</param><param name="value2">Source vector.</param>
        public static Vector4D operator -(Vector4D value1, Vector4D value2)
        {
            Vector4D vector4;
            vector4.X = value1.X - value2.X;
            vector4.Y = value1.Y - value2.Y;
            vector4.Z = value1.Z - value2.Z;
            vector4.W = value1.W - value2.W;
            return vector4;
        }

        /// <summary>
        /// Multiplies the components of two vectors by each other.
        /// </summary>
        /// <param name="value1">Source vector.</param><param name="value2">Source vector.</param>
        public static Vector4D operator *(Vector4D value1, Vector4D value2)
        {
            Vector4D vector4;
            vector4.X = value1.X * value2.X;
            vector4.Y = value1.Y * value2.Y;
            vector4.Z = value1.Z * value2.Z;
            vector4.W = value1.W * value2.W;
            return vector4;
        }

        /// <summary>
        /// Multiplies a vector by a scalar.
        /// </summary>
        /// <param name="value1">Source vector.</param><param name="scaleFactor">Scalar value.</param>
        public static Vector4D operator *(Vector4D value1, double scaleFactor)
        {
            Vector4D vector4;
            vector4.X = value1.X * scaleFactor;
            vector4.Y = value1.Y * scaleFactor;
            vector4.Z = value1.Z * scaleFactor;
            vector4.W = value1.W * scaleFactor;
            return vector4;
        }

        /// <summary>
        /// Multiplies a vector by a scalar.
        /// </summary>
        /// <param name="scaleFactor">Scalar value.</param><param name="value1">Source vector.</param>
        public static Vector4D operator *(double scaleFactor, Vector4D value1)
        {
            Vector4D vector4;
            vector4.X = value1.X * scaleFactor;
            vector4.Y = value1.Y * scaleFactor;
            vector4.Z = value1.Z * scaleFactor;
            vector4.W = value1.W * scaleFactor;
            return vector4;
        }

        /// <summary>
        /// Divides the components of a vector by the components of another vector.
        /// </summary>
        /// <param name="value1">Source vector.</param><param name="value2">Divisor vector.</param>
        public static Vector4D operator /(Vector4D value1, Vector4D value2)
        {
            Vector4D vector4;
            vector4.X = value1.X / value2.X;
            vector4.Y = value1.Y / value2.Y;
            vector4.Z = value1.Z / value2.Z;
            vector4.W = value1.W / value2.W;
            return vector4;
        }

        /// <summary>
        /// Divides a vector by a scalar value.
        /// </summary>
        /// <param name="value1">Source vector.</param><param name="divider">The divisor.</param>
        public static Vector4D operator /(Vector4D value1, double divider)
        {
            double num = 1 / divider;
            Vector4D vector4;
            vector4.X = value1.X * num;
            vector4.Y = value1.Y * num;
            vector4.Z = value1.Z * num;
            vector4.W = value1.W * num;
            return vector4;
        }

        /// <summary>
        /// Divides a vector by a scalar value.
        /// </summary>
        /// <param name="value1">Source vector.</param><param name="divider">The divisor.</param>
        public static Vector4D operator /(double lhs, Vector4D rhs)
        {
            Vector4D vector4;
            vector4.X = lhs / rhs.X;
            vector4.Y = lhs / rhs.Y;
            vector4.Z = lhs / rhs.Z;
            vector4.W = lhs / rhs.W;
            return vector4;
        }

        public static Vector4D PackOrthoMatrix(Vector3D position, Vector3D forward, Vector3D up)
        {
            int forwardInt = (int)Base6Directions.GetDirection(forward);
            int upInt = (int)Base6Directions.GetDirection(up);
            return new Vector4D(position, forwardInt * 6 + upInt);
        }

        public static Vector4D PackOrthoMatrix(ref MatrixD matrix)
        {
            int forward = (int)Base6Directions.GetDirection(matrix.Forward);
            int up = (int)Base6Directions.GetDirection(matrix.Up);
            return new Vector4D(matrix.Translation, forward * 6 + up);
        }

        public static MatrixD UnpackOrthoMatrix(ref Vector4D packed)
        {
            int value = (int)packed.W;
            return MatrixD.CreateWorld((Vector3D)new Vector3((Vector4)packed), Base6Directions.GetVector(value / 6), Base6Directions.GetVector(value % 6));
        }

        /// <summary>
        /// Retrieves a string representation of the current object.
        /// </summary>
        public override string ToString()
        {
            CultureInfo currentCulture = CultureInfo.CurrentCulture;
            return string.Format((IFormatProvider)currentCulture, "{{X:{0} Y:{1} Z:{2} W:{3}}}", (object)this.X.ToString((IFormatProvider)currentCulture), (object)this.Y.ToString((IFormatProvider)currentCulture), (object)this.Z.ToString((IFormatProvider)currentCulture), (object)this.W.ToString((IFormatProvider)currentCulture));
        }

        /// <summary>
        /// Determines whether the specified Object is equal to the Vector4.
        /// </summary>
        /// <param name="other">The Vector4 to compare with the current Vector4.</param>
        public bool Equals(Vector4 other)
        {
            if ((double)this.X == (double)other.X && (double)this.Y == (double)other.Y && (double)this.Z == (double)other.Z)
                return (double)this.W == (double)other.W;
            else
                return false;
        }

        /// <summary>
        /// Returns a value that indicates whether the current instance is equal to a specified object.
        /// </summary>
        /// <param name="obj">Object with which to make the comparison.</param>
        public override bool Equals(object obj)
        {
            bool flag = false;
            if (obj is Vector4)
                flag = this.Equals((Vector4)obj);
            return flag;
        }

        /// <summary>
        /// Gets the hash code of this object.
        /// </summary>
        public override int GetHashCode()
        {
            return this.X.GetHashCode() + this.Y.GetHashCode() + this.Z.GetHashCode() + this.W.GetHashCode();
        }

        /// <summary>
        /// Calculates the length of the vector.
        /// </summary>
        public double Length()
        {
            return (double)Math.Sqrt((double)this.X * (double)this.X + (double)this.Y * (double)this.Y + (double)this.Z * (double)this.Z + (double)this.W * (double)this.W);
        }

        /// <summary>
        /// Calculates the length of the vector squared.
        /// </summary>
        public double LengthSquared()
        {
            return (double)((double)this.X * (double)this.X + (double)this.Y * (double)this.Y + (double)this.Z * (double)this.Z + (double)this.W * (double)this.W);
        }

        /// <summary>
        /// Calculates the distance between two vectors.
        /// </summary>
        /// <param name="value1">Source vector.</param><param name="value2">Source vector.</param>
        public static double Distance(Vector4 value1, Vector4 value2)
        {
            double num1 = value1.X - value2.X;
            double num2 = value1.Y - value2.Y;
            double num3 = value1.Z - value2.Z;
            double num4 = value1.W - value2.W;
            return (double)Math.Sqrt((double)num1 * (double)num1 + (double)num2 * (double)num2 + (double)num3 * (double)num3 + (double)num4 * (double)num4);
        }

        /// <summary>
        /// Calculates the distance between two vectors.
        /// </summary>
        /// <param name="value1">Source vector.</param><param name="value2">Source vector.</param><param name="result">[OutAttribute] The distance between the vectors.</param>
        public static void Distance(ref Vector4 value1, ref Vector4 value2, out double result)
        {
            double num1 = value1.X - value2.X;
            double num2 = value1.Y - value2.Y;
            double num3 = value1.Z - value2.Z;
            double num4 = value1.W - value2.W;
            double num5 = (double)((double)num1 * (double)num1 + (double)num2 * (double)num2 + (double)num3 * (double)num3 + (double)num4 * (double)num4);
            result = (double)Math.Sqrt((double)num5);
        }

        /// <summary>
        /// Calculates the distance between two vectors squared.
        /// </summary>
        /// <param name="value1">Source vector.</param><param name="value2">Source vector.</param>
        public static double DistanceSquared(Vector4 value1, Vector4 value2)
        {
            double num1 = value1.X - value2.X;
            double num2 = value1.Y - value2.Y;
            double num3 = value1.Z - value2.Z;
            double num4 = value1.W - value2.W;
            return (double)((double)num1 * (double)num1 + (double)num2 * (double)num2 + (double)num3 * (double)num3 + (double)num4 * (double)num4);
        }

        /// <summary>
        /// Calculates the distance between two vectors squared.
        /// </summary>
        /// <param name="value1">Source vector.</param><param name="value2">Source vector.</param><param name="result">[OutAttribute] The distance between the two vectors squared.</param>
        public static void DistanceSquared(ref Vector4 value1, ref Vector4 value2, out double result)
        {
            double num1 = value1.X - value2.X;
            double num2 = value1.Y - value2.Y;
            double num3 = value1.Z - value2.Z;
            double num4 = value1.W - value2.W;
            result = (double)((double)num1 * (double)num1 + (double)num2 * (double)num2 + (double)num3 * (double)num3 + (double)num4 * (double)num4);
        }

        /// <summary>
        /// Calculates the dot product of two vectors.
        /// </summary>
        /// <param name="vector1">Source vector.</param><param name="vector2">Source vector.</param>
        public static double Dot(Vector4 vector1, Vector4 vector2)
        {
            return (double)((double)vector1.X * (double)vector2.X + (double)vector1.Y * (double)vector2.Y + (double)vector1.Z * (double)vector2.Z + (double)vector1.W * (double)vector2.W);
        }

        /// <summary>
        /// Calculates the dot product of two vectors.
        /// </summary>
        /// <param name="vector1">Source vector.</param><param name="vector2">Source vector.</param><param name="result">[OutAttribute] The dot product of the two vectors.</param>
        public static void Dot(ref Vector4 vector1, ref Vector4 vector2, out double result)
        {
            result = (double)((double)vector1.X * (double)vector2.X + (double)vector1.Y * (double)vector2.Y + (double)vector1.Z * (double)vector2.Z + (double)vector1.W * (double)vector2.W);
        }

        /// <summary>
        /// Turns the current vector into a unit vector.
        /// </summary>
        public void Normalize()
        {
            double num = 1f / (double)Math.Sqrt((double)this.X * (double)this.X + (double)this.Y * (double)this.Y + (double)this.Z * (double)this.Z + (double)this.W * (double)this.W);
            this.X *= num;
            this.Y *= num;
            this.Z *= num;
            this.W *= num;
        }

        /// <summary>
        /// Creates a unit vector from the specified vector.
        /// </summary>
        /// <param name="vector">The source Vector4.</param>
        public static Vector4D Normalize(Vector4D vector)
        {
            double num = 1f / (double)Math.Sqrt((double)vector.X * (double)vector.X + (double)vector.Y * (double)vector.Y + (double)vector.Z * (double)vector.Z + (double)vector.W * (double)vector.W);
            Vector4D vector4;
            vector4.X = vector.X * num;
            vector4.Y = vector.Y * num;
            vector4.Z = vector.Z * num;
            vector4.W = vector.W * num;
            return vector4;
        }

        /// <summary>
        /// Returns a normalized version of the specified vector.
        /// </summary>
        /// <param name="vector">Source vector.</param><param name="result">[OutAttribute] The normalized vector.</param>
        public static void Normalize(ref Vector4D vector, out Vector4D result)
        {
            double num = 1f / (double)Math.Sqrt((double)vector.X * (double)vector.X + (double)vector.Y * (double)vector.Y + (double)vector.Z * (double)vector.Z + (double)vector.W * (double)vector.W);
            result.X = vector.X * num;
            result.Y = vector.Y * num;
            result.Z = vector.Z * num;
            result.W = vector.W * num;
        }

        /// <summary>
        /// Returns a vector that contains the lowest value from each matching pair of components.
        /// </summary>
        /// <param name="value1">Source vector.</param><param name="value2">Source vector.</param>
        public static Vector4 Min(Vector4 value1, Vector4 value2)
        {
            Vector4 vector4;
            vector4.X = (double)value1.X < (double)value2.X ? value1.X : value2.X;
            vector4.Y = (double)value1.Y < (double)value2.Y ? value1.Y : value2.Y;
            vector4.Z = (double)value1.Z < (double)value2.Z ? value1.Z : value2.Z;
            vector4.W = (double)value1.W < (double)value2.W ? value1.W : value2.W;
            return vector4;
        }

        /// <summary>
        /// Returns a vector that contains the lowest value from each matching pair of components.
        /// </summary>
        /// <param name="value1">Source vector.</param><param name="value2">Source vector.</param><param name="result">[OutAttribute] The minimized vector.</param>
        public static void Min(ref Vector4 value1, ref Vector4 value2, out Vector4 result)
        {
            result.X = (double)value1.X < (double)value2.X ? value1.X : value2.X;
            result.Y = (double)value1.Y < (double)value2.Y ? value1.Y : value2.Y;
            result.Z = (double)value1.Z < (double)value2.Z ? value1.Z : value2.Z;
            result.W = (double)value1.W < (double)value2.W ? value1.W : value2.W;
        }

        /// <summary>
        /// Returns a vector that contains the highest value from each matching pair of components.
        /// </summary>
        /// <param name="value1">Source vector.</param><param name="value2">Source vector.</param>
        public static Vector4 Max(Vector4 value1, Vector4 value2)
        {
            Vector4 vector4;
            vector4.X = (double)value1.X > (double)value2.X ? value1.X : value2.X;
            vector4.Y = (double)value1.Y > (double)value2.Y ? value1.Y : value2.Y;
            vector4.Z = (double)value1.Z > (double)value2.Z ? value1.Z : value2.Z;
            vector4.W = (double)value1.W > (double)value2.W ? value1.W : value2.W;
            return vector4;
        }

        /// <summary>
        /// Returns a vector that contains the highest value from each matching pair of components.
        /// </summary>
        /// <param name="value1">Source vector.</param><param name="value2">Source vector.</param><param name="result">[OutAttribute] The maximized vector.</param>
        public static void Max(ref Vector4 value1, ref Vector4 value2, out Vector4 result)
        {
            result.X = (double)value1.X > (double)value2.X ? value1.X : value2.X;
            result.Y = (double)value1.Y > (double)value2.Y ? value1.Y : value2.Y;
            result.Z = (double)value1.Z > (double)value2.Z ? value1.Z : value2.Z;
            result.W = (double)value1.W > (double)value2.W ? value1.W : value2.W;
        }

        /// <summary>
        /// Restricts a value to be within a specified range.
        /// </summary>
        /// <param name="value1">The value to clamp.</param><param name="min">The minimum value.</param><param name="max">The maximum value.</param>
        public static Vector4D Clamp(Vector4D value1, Vector4D min, Vector4D max)
        {
            double num1 = value1.X;
            double num2 = (double)num1 > (double)max.X ? max.X : num1;
            double num3 = (double)num2 < (double)min.X ? min.X : num2;
            double num4 = value1.Y;
            double num5 = (double)num4 > (double)max.Y ? max.Y : num4;
            double num6 = (double)num5 < (double)min.Y ? min.Y : num5;
            double num7 = value1.Z;
            double num8 = (double)num7 > (double)max.Z ? max.Z : num7;
            double num9 = (double)num8 < (double)min.Z ? min.Z : num8;
            double num10 = value1.W;
            double num11 = (double)num10 > (double)max.W ? max.W : num10;
            double num12 = (double)num11 < (double)min.W ? min.W : num11;
            Vector4D vector4;
            vector4.X = num3;
            vector4.Y = num6;
            vector4.Z = num9;
            vector4.W = num12;
            return vector4;
        }

        /// <summary>
        /// Restricts a value to be within a specified range.
        /// </summary>
        /// <param name="value1">The value to clamp.</param><param name="min">The minimum value.</param><param name="max">The maximum value.</param><param name="result">[OutAttribute] The clamped value.</param>
        public static void Clamp(ref Vector4D value1, ref Vector4D min, ref Vector4D max, out Vector4D result)
        {
            double num1 = value1.X;
            double num2 = (double)num1 > (double)max.X ? max.X : num1;
            double num3 = (double)num2 < (double)min.X ? min.X : num2;
            double num4 = value1.Y;
            double num5 = (double)num4 > (double)max.Y ? max.Y : num4;
            double num6 = (double)num5 < (double)min.Y ? min.Y : num5;
            double num7 = value1.Z;
            double num8 = (double)num7 > (double)max.Z ? max.Z : num7;
            double num9 = (double)num8 < (double)min.Z ? min.Z : num8;
            double num10 = value1.W;
            double num11 = (double)num10 > (double)max.W ? max.W : num10;
            double num12 = (double)num11 < (double)min.W ? min.W : num11;
            result.X = num3;
            result.Y = num6;
            result.Z = num9;
            result.W = num12;
        }

        /// <summary>
        /// Performs a linear interpolation between two vectors.
        /// </summary>
        /// <param name="value1">Source vector.</param><param name="value2">Source vector.</param><param name="amount">Value between 0 and 1 indicating the weight of value2.</param>
        public static Vector4D Lerp(Vector4D value1, Vector4D value2, double amount)
        {
            Vector4D vector4;
            vector4.X = value1.X + (value2.X - value1.X) * amount;
            vector4.Y = value1.Y + (value2.Y - value1.Y) * amount;
            vector4.Z = value1.Z + (value2.Z - value1.Z) * amount;
            vector4.W = value1.W + (value2.W - value1.W) * amount;
            return vector4;
        }

        /// <summary>
        /// Performs a linear interpolation between two vectors.
        /// </summary>
        /// <param name="value1">Source vector.</param><param name="value2">Source vector.</param><param name="amount">Value between 0 and 1 indicating the weight of value2.</param><param name="result">[OutAttribute] The result of the interpolation.</param>
        public static void Lerp(ref Vector4D value1, ref Vector4D value2, double amount, out Vector4D result)
        {
            result.X = value1.X + (value2.X - value1.X) * amount;
            result.Y = value1.Y + (value2.Y - value1.Y) * amount;
            result.Z = value1.Z + (value2.Z - value1.Z) * amount;
            result.W = value1.W + (value2.W - value1.W) * amount;
        }

        /// <summary>
        /// Returns a Vector4 containing the 4D Cartesian coordinates of a point specified in barycentric (areal) coordinates relative to a 4D triangle.
        /// </summary>
        /// <param name="value1">A Vector4 containing the 4D Cartesian coordinates of vertex 1 of the triangle.</param><param name="value2">A Vector4 containing the 4D Cartesian coordinates of vertex 2 of the triangle.</param><param name="value3">A Vector4 containing the 4D Cartesian coordinates of vertex 3 of the triangle.</param><param name="amount1">Barycentric coordinate b2, which expresses the weighting factor toward vertex 2 (specified in value2).</param><param name="amount2">Barycentric coordinate b3, which expresses the weighting factor toward vertex 3 (specified in value3).</param>
        public static Vector4D Barycentric(Vector4D value1, Vector4D value2, Vector4D value3, double amount1, double amount2)
        {
            Vector4D vector4;
            vector4.X = (double)((double)value1.X + (double)amount1 * ((double)value2.X - (double)value1.X) + (double)amount2 * ((double)value3.X - (double)value1.X));
            vector4.Y = (double)((double)value1.Y + (double)amount1 * ((double)value2.Y - (double)value1.Y) + (double)amount2 * ((double)value3.Y - (double)value1.Y));
            vector4.Z = (double)((double)value1.Z + (double)amount1 * ((double)value2.Z - (double)value1.Z) + (double)amount2 * ((double)value3.Z - (double)value1.Z));
            vector4.W = (double)((double)value1.W + (double)amount1 * ((double)value2.W - (double)value1.W) + (double)amount2 * ((double)value3.W - (double)value1.W));
            return vector4;
        }

        /// <summary>
        /// Returns a Vector4 containing the 4D Cartesian coordinates of a point specified in Barycentric (areal) coordinates relative to a 4D triangle.
        /// </summary>
        /// <param name="value1">A Vector4 containing the 4D Cartesian coordinates of vertex 1 of the triangle.</param><param name="value2">A Vector4 containing the 4D Cartesian coordinates of vertex 2 of the triangle.</param><param name="value3">A Vector4 containing the 4D Cartesian coordinates of vertex 3 of the triangle.</param><param name="amount1">Barycentric coordinate b2, which expresses the weighting factor toward vertex 2 (specified in value2).</param><param name="amount2">Barycentric coordinate b3, which expresses the weighting factor toward vertex 3 (specified in value3).</param><param name="result">[OutAttribute] The 4D Cartesian coordinates of the specified point are placed in this Vector4 on exit.</param>
        public static void Barycentric(ref Vector4D value1, ref Vector4D value2, ref Vector4D value3, double amount1, double amount2, out Vector4D result)
        {
            result.X = (double)((double)value1.X + (double)amount1 * ((double)value2.X - (double)value1.X) + (double)amount2 * ((double)value3.X - (double)value1.X));
            result.Y = (double)((double)value1.Y + (double)amount1 * ((double)value2.Y - (double)value1.Y) + (double)amount2 * ((double)value3.Y - (double)value1.Y));
            result.Z = (double)((double)value1.Z + (double)amount1 * ((double)value2.Z - (double)value1.Z) + (double)amount2 * ((double)value3.Z - (double)value1.Z));
            result.W = (double)((double)value1.W + (double)amount1 * ((double)value2.W - (double)value1.W) + (double)amount2 * ((double)value3.W - (double)value1.W));
        }

        /// <summary>
        /// Interpolates between two values using a cubic equation.
        /// </summary>
        /// <param name="value1">Source value.</param><param name="value2">Source value.</param><param name="amount">Weighting value.</param>
        public static Vector4D SmoothStep(Vector4D value1, Vector4D value2, double amount)
        {
            amount = (double)amount > 1.0 ? 1f : ((double)amount < 0.0 ? 0.0f : amount);
            amount = (double)((double)amount * (double)amount * (3.0 - 2.0 * (double)amount));
            Vector4D vector4;
            vector4.X = value1.X + (value2.X - value1.X) * amount;
            vector4.Y = value1.Y + (value2.Y - value1.Y) * amount;
            vector4.Z = value1.Z + (value2.Z - value1.Z) * amount;
            vector4.W = value1.W + (value2.W - value1.W) * amount;
            return vector4;
        }

        /// <summary>
        /// Interpolates between two values using a cubic equation.
        /// </summary>
        /// <param name="value1">Source vector.</param><param name="value2">Source vector.</param><param name="amount">Weighting factor.</param><param name="result">[OutAttribute] The interpolated value.</param>
        public static void SmoothStep(ref Vector4D value1, ref Vector4D value2, double amount, out Vector4D result)
        {
            amount = (double)amount > 1.0 ? 1f : ((double)amount < 0.0 ? 0.0f : amount);
            amount = (double)((double)amount * (double)amount * (3.0 - 2.0 * (double)amount));
            result.X = value1.X + (value2.X - value1.X) * amount;
            result.Y = value1.Y + (value2.Y - value1.Y) * amount;
            result.Z = value1.Z + (value2.Z - value1.Z) * amount;
            result.W = value1.W + (value2.W - value1.W) * amount;
        }

        /// <summary>
        /// Performs a Catmull-Rom interpolation using the specified positions.
        /// </summary>
        /// <param name="value1">The first position in the interpolation.</param><param name="value2">The second position in the interpolation.</param><param name="value3">The third position in the interpolation.</param><param name="value4">The fourth position in the interpolation.</param><param name="amount">Weighting factor.</param>
        public static Vector4D CatmullRom(Vector4D value1, Vector4D value2, Vector4D value3, Vector4D value4, double amount)
        {
            double num1 = amount * amount;
            double num2 = amount * num1;
            Vector4D vector4;
            vector4.X = (double)(0.5 * (2.0 * (double)value2.X + (-(double)value1.X + (double)value3.X) * (double)amount + (2.0 * (double)value1.X - 5.0 * (double)value2.X + 4.0 * (double)value3.X - (double)value4.X) * (double)num1 + (-(double)value1.X + 3.0 * (double)value2.X - 3.0 * (double)value3.X + (double)value4.X) * (double)num2));
            vector4.Y = (double)(0.5 * (2.0 * (double)value2.Y + (-(double)value1.Y + (double)value3.Y) * (double)amount + (2.0 * (double)value1.Y - 5.0 * (double)value2.Y + 4.0 * (double)value3.Y - (double)value4.Y) * (double)num1 + (-(double)value1.Y + 3.0 * (double)value2.Y - 3.0 * (double)value3.Y + (double)value4.Y) * (double)num2));
            vector4.Z = (double)(0.5 * (2.0 * (double)value2.Z + (-(double)value1.Z + (double)value3.Z) * (double)amount + (2.0 * (double)value1.Z - 5.0 * (double)value2.Z + 4.0 * (double)value3.Z - (double)value4.Z) * (double)num1 + (-(double)value1.Z + 3.0 * (double)value2.Z - 3.0 * (double)value3.Z + (double)value4.Z) * (double)num2));
            vector4.W = (double)(0.5 * (2.0 * (double)value2.W + (-(double)value1.W + (double)value3.W) * (double)amount + (2.0 * (double)value1.W - 5.0 * (double)value2.W + 4.0 * (double)value3.W - (double)value4.W) * (double)num1 + (-(double)value1.W + 3.0 * (double)value2.W - 3.0 * (double)value3.W + (double)value4.W) * (double)num2));
            return vector4;
        }

        /// <summary>
        /// Performs a Catmull-Rom interpolation using the specified positions.
        /// </summary>
        /// <param name="value1">The first position in the interpolation.</param><param name="value2">The second position in the interpolation.</param><param name="value3">The third position in the interpolation.</param><param name="value4">The fourth position in the interpolation.</param><param name="amount">Weighting factor.</param><param name="result">[OutAttribute] A vector that is the result of the Catmull-Rom interpolation.</param>
        public static void CatmullRom(ref Vector4D value1, ref Vector4D value2, ref Vector4D value3, ref Vector4D value4, double amount, out Vector4D result)
        {
            double num1 = amount * amount;
            double num2 = amount * num1;
            result.X = (double)(0.5 * (2.0 * (double)value2.X + (-(double)value1.X + (double)value3.X) * (double)amount + (2.0 * (double)value1.X - 5.0 * (double)value2.X + 4.0 * (double)value3.X - (double)value4.X) * (double)num1 + (-(double)value1.X + 3.0 * (double)value2.X - 3.0 * (double)value3.X + (double)value4.X) * (double)num2));
            result.Y = (double)(0.5 * (2.0 * (double)value2.Y + (-(double)value1.Y + (double)value3.Y) * (double)amount + (2.0 * (double)value1.Y - 5.0 * (double)value2.Y + 4.0 * (double)value3.Y - (double)value4.Y) * (double)num1 + (-(double)value1.Y + 3.0 * (double)value2.Y - 3.0 * (double)value3.Y + (double)value4.Y) * (double)num2));
            result.Z = (double)(0.5 * (2.0 * (double)value2.Z + (-(double)value1.Z + (double)value3.Z) * (double)amount + (2.0 * (double)value1.Z - 5.0 * (double)value2.Z + 4.0 * (double)value3.Z - (double)value4.Z) * (double)num1 + (-(double)value1.Z + 3.0 * (double)value2.Z - 3.0 * (double)value3.Z + (double)value4.Z) * (double)num2));
            result.W = (double)(0.5 * (2.0 * (double)value2.W + (-(double)value1.W + (double)value3.W) * (double)amount + (2.0 * (double)value1.W - 5.0 * (double)value2.W + 4.0 * (double)value3.W - (double)value4.W) * (double)num1 + (-(double)value1.W + 3.0 * (double)value2.W - 3.0 * (double)value3.W + (double)value4.W) * (double)num2));
        }

        /// <summary>
        /// Performs a Hermite spline interpolation.
        /// </summary>
        /// <param name="value1">Source position vector.</param><param name="tangent1">Source tangent vector.</param><param name="value2">Source position vector.</param><param name="tangent2">Source tangent vector.</param><param name="amount">Weighting factor.</param>
        public static Vector4D Hermite(Vector4D value1, Vector4D tangent1, Vector4D value2, Vector4D tangent2, double amount)
        {
            double num1 = amount * amount;
            double num2 = amount * num1;
            double num3 = (double)(2.0 * (double)num2 - 3.0 * (double)num1 + 1.0);
            double num4 = (double)(-2.0 * (double)num2 + 3.0 * (double)num1);
            double num5 = num2 - 2f * num1 + amount;
            double num6 = num2 - num1;
            Vector4D vector4;
            vector4.X = (double)((double)value1.X * (double)num3 + (double)value2.X * (double)num4 + (double)tangent1.X * (double)num5 + (double)tangent2.X * (double)num6);
            vector4.Y = (double)((double)value1.Y * (double)num3 + (double)value2.Y * (double)num4 + (double)tangent1.Y * (double)num5 + (double)tangent2.Y * (double)num6);
            vector4.Z = (double)((double)value1.Z * (double)num3 + (double)value2.Z * (double)num4 + (double)tangent1.Z * (double)num5 + (double)tangent2.Z * (double)num6);
            vector4.W = (double)((double)value1.W * (double)num3 + (double)value2.W * (double)num4 + (double)tangent1.W * (double)num5 + (double)tangent2.W * (double)num6);
            return vector4;
        }

        /// <summary>
        /// Performs a Hermite spline interpolation.
        /// </summary>
        /// <param name="value1">Source position vector.</param><param name="tangent1">Source tangent vector.</param><param name="value2">Source position vector.</param><param name="tangent2">Source tangent vector.</param><param name="amount">Weighting factor.</param><param name="result">[OutAttribute] The result of the Hermite spline interpolation.</param>
        public static void Hermite(ref Vector4D value1, ref Vector4D tangent1, ref Vector4D value2, ref Vector4D tangent2, double amount, out Vector4D result)
        {
            double num1 = amount * amount;
            double num2 = amount * num1;
            double num3 = (double)(2.0 * (double)num2 - 3.0 * (double)num1 + 1.0);
            double num4 = (double)(-2.0 * (double)num2 + 3.0 * (double)num1);
            double num5 = num2 - 2f * num1 + amount;
            double num6 = num2 - num1;
            result.X = (double)((double)value1.X * (double)num3 + (double)value2.X * (double)num4 + (double)tangent1.X * (double)num5 + (double)tangent2.X * (double)num6);
            result.Y = (double)((double)value1.Y * (double)num3 + (double)value2.Y * (double)num4 + (double)tangent1.Y * (double)num5 + (double)tangent2.Y * (double)num6);
            result.Z = (double)((double)value1.Z * (double)num3 + (double)value2.Z * (double)num4 + (double)tangent1.Z * (double)num5 + (double)tangent2.Z * (double)num6);
            result.W = (double)((double)value1.W * (double)num3 + (double)value2.W * (double)num4 + (double)tangent1.W * (double)num5 + (double)tangent2.W * (double)num6);
        }

        /// <summary>
        /// Transforms a Vector2 by the given Matrix.
        /// </summary>
        /// <param name="position">The source Vector2.</param><param name="matrix">The transformation Matrix.</param>
        public static Vector4D Transform(Vector2 position, MatrixD matrix)
        {
            double num1 = (double)((double)position.X * (double)matrix.M11 + (double)position.Y * (double)matrix.M21) + matrix.M41;
            double num2 = (double)((double)position.X * (double)matrix.M12 + (double)position.Y * (double)matrix.M22) + matrix.M42;
            double num3 = (double)((double)position.X * (double)matrix.M13 + (double)position.Y * (double)matrix.M23) + matrix.M43;
            double num4 = (double)((double)position.X * (double)matrix.M14 + (double)position.Y * (double)matrix.M24) + matrix.M44;
            Vector4D vector4;
            vector4.X = num1;
            vector4.Y = num2;
            vector4.Z = num3;
            vector4.W = num4;
            return vector4;
        }

        /// <summary>
        /// Transforms a Vector2 by the given Matrix.
        /// </summary>
        /// <param name="position">The source Vector2.</param><param name="matrix">The transformation Matrix.</param><param name="result">[OutAttribute] The Vector4 resulting from the transformation.</param>
        public static void Transform(ref Vector2 position, ref MatrixD matrix, out Vector4D result)
        {
            double num1 = (double)((double)position.X * (double)matrix.M11 + (double)position.Y * (double)matrix.M21) + matrix.M41;
            double num2 = (double)((double)position.X * (double)matrix.M12 + (double)position.Y * (double)matrix.M22) + matrix.M42;
            double num3 = (double)((double)position.X * (double)matrix.M13 + (double)position.Y * (double)matrix.M23) + matrix.M43;
            double num4 = (double)((double)position.X * (double)matrix.M14 + (double)position.Y * (double)matrix.M24) + matrix.M44;
            result.X = num1;
            result.Y = num2;
            result.Z = num3;
            result.W = num4;
        }

        /// <summary>
        /// Transforms a Vector3 by the given Matrix.
        /// </summary>
        /// <param name="position">The source Vector3.</param><param name="matrix">The transformation Matrix.</param>
        public static Vector4D Transform(Vector3D position, MatrixD matrix)
        {
            double num1 = (double)((double)position.X * (double)matrix.M11 + (double)position.Y * (double)matrix.M21 + (double)position.Z * (double)matrix.M31) + matrix.M41;
            double num2 = (double)((double)position.X * (double)matrix.M12 + (double)position.Y * (double)matrix.M22 + (double)position.Z * (double)matrix.M32) + matrix.M42;
            double num3 = (double)((double)position.X * (double)matrix.M13 + (double)position.Y * (double)matrix.M23 + (double)position.Z * (double)matrix.M33) + matrix.M43;
            double num4 = (double)((double)position.X * (double)matrix.M14 + (double)position.Y * (double)matrix.M24 + (double)position.Z * (double)matrix.M34) + matrix.M44;
            Vector4D vector4;
            vector4.X = num1;
            vector4.Y = num2;
            vector4.Z = num3;
            vector4.W = num4;
            return vector4;
        }

        /// <summary>
        /// Transforms a Vector3 by the given Matrix.
        /// </summary>
        /// <param name="position">The source Vector3.</param><param name="matrix">The transformation Matrix.</param><param name="result">[OutAttribute] The Vector4 resulting from the transformation.</param>
        public static void Transform(ref Vector3D position, ref MatrixD matrix, out Vector4D result)
        {
            double num1 = (double)((double)position.X * (double)matrix.M11 + (double)position.Y * (double)matrix.M21 + (double)position.Z * (double)matrix.M31) + matrix.M41;
            double num2 = (double)((double)position.X * (double)matrix.M12 + (double)position.Y * (double)matrix.M22 + (double)position.Z * (double)matrix.M32) + matrix.M42;
            double num3 = (double)((double)position.X * (double)matrix.M13 + (double)position.Y * (double)matrix.M23 + (double)position.Z * (double)matrix.M33) + matrix.M43;
            double num4 = (double)((double)position.X * (double)matrix.M14 + (double)position.Y * (double)matrix.M24 + (double)position.Z * (double)matrix.M34) + matrix.M44;
            result.X = num1;
            result.Y = num2;
            result.Z = num3;
            result.W = num4;
        }

        /// <summary>
        /// Transforms a Vector4 by the specified Matrix.
        /// </summary>
        /// <param name="vector">The source Vector4.</param><param name="matrix">The transformation Matrix.</param>
        public static Vector4D Transform(Vector4D vector, MatrixD matrix)
        {
            double num1 = (double)((double)vector.X * (double)matrix.M11 + (double)vector.Y * (double)matrix.M21 + (double)vector.Z * (double)matrix.M31 + (double)vector.W * (double)matrix.M41);
            double num2 = (double)((double)vector.X * (double)matrix.M12 + (double)vector.Y * (double)matrix.M22 + (double)vector.Z * (double)matrix.M32 + (double)vector.W * (double)matrix.M42);
            double num3 = (double)((double)vector.X * (double)matrix.M13 + (double)vector.Y * (double)matrix.M23 + (double)vector.Z * (double)matrix.M33 + (double)vector.W * (double)matrix.M43);
            double num4 = (double)((double)vector.X * (double)matrix.M14 + (double)vector.Y * (double)matrix.M24 + (double)vector.Z * (double)matrix.M34 + (double)vector.W * (double)matrix.M44);
            Vector4D vector4;
            vector4.X = num1;
            vector4.Y = num2;
            vector4.Z = num3;
            vector4.W = num4;
            return vector4;
        }

        /// <summary>
        /// Transforms a Vector4 by the given Matrix.
        /// </summary>
        /// <param name="vector">The source Vector4.</param><param name="matrix">The transformation Matrix.</param><param name="result">[OutAttribute] The Vector4 resulting from the transformation.</param>
        public static void Transform(ref Vector4D vector, ref MatrixD matrix, out Vector4D result)
        {
            double num1 = (double)((double)vector.X * (double)matrix.M11 + (double)vector.Y * (double)matrix.M21 + (double)vector.Z * (double)matrix.M31 + (double)vector.W * (double)matrix.M41);
            double num2 = (double)((double)vector.X * (double)matrix.M12 + (double)vector.Y * (double)matrix.M22 + (double)vector.Z * (double)matrix.M32 + (double)vector.W * (double)matrix.M42);
            double num3 = (double)((double)vector.X * (double)matrix.M13 + (double)vector.Y * (double)matrix.M23 + (double)vector.Z * (double)matrix.M33 + (double)vector.W * (double)matrix.M43);
            double num4 = (double)((double)vector.X * (double)matrix.M14 + (double)vector.Y * (double)matrix.M24 + (double)vector.Z * (double)matrix.M34 + (double)vector.W * (double)matrix.M44);
            result.X = num1;
            result.Y = num2;
            result.Z = num3;
            result.W = num4;
        }

        /// <summary>
        /// Transforms a Vector2 by a specified Quaternion into a Vector4.
        /// </summary>
        /// <param name="value">The Vector2 to transform.</param><param name="rotation">The Quaternion rotation to apply.</param>
        public static Vector4D Transform(Vector2 value, Quaternion rotation)
        {
            double num1 = rotation.X + rotation.X;
            double num2 = rotation.Y + rotation.Y;
            double num3 = rotation.Z + rotation.Z;
            double num4 = rotation.W * num1;
            double num5 = rotation.W * num2;
            double num6 = rotation.W * num3;
            double num7 = rotation.X * num1;
            double num8 = rotation.X * num2;
            double num9 = rotation.X * num3;
            double num10 = rotation.Y * num2;
            double num11 = rotation.Y * num3;
            double num12 = rotation.Z * num3;
            double num13 = (double)((double)value.X * (1.0 - (double)num10 - (double)num12) + (double)value.Y * ((double)num8 - (double)num6));
            double num14 = (double)((double)value.X * ((double)num8 + (double)num6) + (double)value.Y * (1.0 - (double)num7 - (double)num12));
            double num15 = (double)((double)value.X * ((double)num9 - (double)num5) + (double)value.Y * ((double)num11 + (double)num4));
            Vector4D vector4;
            vector4.X = num13;
            vector4.Y = num14;
            vector4.Z = num15;
            vector4.W = 1f;
            return vector4;
        }

        /// <summary>
        /// Transforms a Vector2 by a specified Quaternion into a Vector4.
        /// </summary>
        /// <param name="value">The Vector2 to transform.</param><param name="rotation">The Quaternion rotation to apply.</param><param name="result">[OutAttribute] The Vector4 resulting from the transformation.</param>
        public static void Transform(ref Vector2 value, ref Quaternion rotation, out Vector4D result)
        {
            double num1 = rotation.X + rotation.X;
            double num2 = rotation.Y + rotation.Y;
            double num3 = rotation.Z + rotation.Z;
            double num4 = rotation.W * num1;
            double num5 = rotation.W * num2;
            double num6 = rotation.W * num3;
            double num7 = rotation.X * num1;
            double num8 = rotation.X * num2;
            double num9 = rotation.X * num3;
            double num10 = rotation.Y * num2;
            double num11 = rotation.Y * num3;
            double num12 = rotation.Z * num3;
            double num13 = (double)((double)value.X * (1.0 - (double)num10 - (double)num12) + (double)value.Y * ((double)num8 - (double)num6));
            double num14 = (double)((double)value.X * ((double)num8 + (double)num6) + (double)value.Y * (1.0 - (double)num7 - (double)num12));
            double num15 = (double)((double)value.X * ((double)num9 - (double)num5) + (double)value.Y * ((double)num11 + (double)num4));
            result.X = num13;
            result.Y = num14;
            result.Z = num15;
            result.W = 1f;
        }

        /// <summary>
        /// Transforms a Vector3 by a specified Quaternion into a Vector4.
        /// </summary>
        /// <param name="value">The Vector3 to transform.</param><param name="rotation">The Quaternion rotation to apply.</param>
        public static Vector4D Transform(Vector3D value, Quaternion rotation)
        {
            double num1 = rotation.X + rotation.X;
            double num2 = rotation.Y + rotation.Y;
            double num3 = rotation.Z + rotation.Z;
            double num4 = rotation.W * num1;
            double num5 = rotation.W * num2;
            double num6 = rotation.W * num3;
            double num7 = rotation.X * num1;
            double num8 = rotation.X * num2;
            double num9 = rotation.X * num3;
            double num10 = rotation.Y * num2;
            double num11 = rotation.Y * num3;
            double num12 = rotation.Z * num3;
            double num13 = (double)((double)value.X * (1.0 - (double)num10 - (double)num12) + (double)value.Y * ((double)num8 - (double)num6) + (double)value.Z * ((double)num9 + (double)num5));
            double num14 = (double)((double)value.X * ((double)num8 + (double)num6) + (double)value.Y * (1.0 - (double)num7 - (double)num12) + (double)value.Z * ((double)num11 - (double)num4));
            double num15 = (double)((double)value.X * ((double)num9 - (double)num5) + (double)value.Y * ((double)num11 + (double)num4) + (double)value.Z * (1.0 - (double)num7 - (double)num10));
            Vector4D vector4;
            vector4.X = num13;
            vector4.Y = num14;
            vector4.Z = num15;
            vector4.W = 1f;
            return vector4;
        }

        /// <summary>
        /// Transforms a Vector3 by a specified Quaternion into a Vector4.
        /// </summary>
        /// <param name="value">The Vector3 to transform.</param><param name="rotation">The Quaternion rotation to apply.</param><param name="result">[OutAttribute] The Vector4 resulting from the transformation.</param>
        public static void Transform(ref Vector3D value, ref Quaternion rotation, out Vector4D result)
        {
            double num1 = rotation.X + rotation.X;
            double num2 = rotation.Y + rotation.Y;
            double num3 = rotation.Z + rotation.Z;
            double num4 = rotation.W * num1;
            double num5 = rotation.W * num2;
            double num6 = rotation.W * num3;
            double num7 = rotation.X * num1;
            double num8 = rotation.X * num2;
            double num9 = rotation.X * num3;
            double num10 = rotation.Y * num2;
            double num11 = rotation.Y * num3;
            double num12 = rotation.Z * num3;
            double num13 = (double)((double)value.X * (1.0 - (double)num10 - (double)num12) + (double)value.Y * ((double)num8 - (double)num6) + (double)value.Z * ((double)num9 + (double)num5));
            double num14 = (double)((double)value.X * ((double)num8 + (double)num6) + (double)value.Y * (1.0 - (double)num7 - (double)num12) + (double)value.Z * ((double)num11 - (double)num4));
            double num15 = (double)((double)value.X * ((double)num9 - (double)num5) + (double)value.Y * ((double)num11 + (double)num4) + (double)value.Z * (1.0 - (double)num7 - (double)num10));
            result.X = num13;
            result.Y = num14;
            result.Z = num15;
            result.W = 1f;
        }

        /// <summary>
        /// Transforms a Vector4 by a specified Quaternion.
        /// </summary>
        /// <param name="value">The Vector4 to transform.</param><param name="rotation">The Quaternion rotation to apply.</param>
        public static Vector4D Transform(Vector4D value, Quaternion rotation)
        {
            double num1 = rotation.X + rotation.X;
            double num2 = rotation.Y + rotation.Y;
            double num3 = rotation.Z + rotation.Z;
            double num4 = rotation.W * num1;
            double num5 = rotation.W * num2;
            double num6 = rotation.W * num3;
            double num7 = rotation.X * num1;
            double num8 = rotation.X * num2;
            double num9 = rotation.X * num3;
            double num10 = rotation.Y * num2;
            double num11 = rotation.Y * num3;
            double num12 = rotation.Z * num3;
            double num13 = (double)((double)value.X * (1.0 - (double)num10 - (double)num12) + (double)value.Y * ((double)num8 - (double)num6) + (double)value.Z * ((double)num9 + (double)num5));
            double num14 = (double)((double)value.X * ((double)num8 + (double)num6) + (double)value.Y * (1.0 - (double)num7 - (double)num12) + (double)value.Z * ((double)num11 - (double)num4));
            double num15 = (double)((double)value.X * ((double)num9 - (double)num5) + (double)value.Y * ((double)num11 + (double)num4) + (double)value.Z * (1.0 - (double)num7 - (double)num10));
            Vector4D vector4;
            vector4.X = num13;
            vector4.Y = num14;
            vector4.Z = num15;
            vector4.W = value.W;
            return vector4;
        }

        /// <summary>
        /// Transforms a Vector4 by a specified Quaternion.
        /// </summary>
        /// <param name="value">The Vector4 to transform.</param><param name="rotation">The Quaternion rotation to apply.</param><param name="result">[OutAttribute] The Vector4 resulting from the transformation.</param>
        public static void Transform(ref Vector4D value, ref Quaternion rotation, out Vector4D result)
        {
            double num1 = rotation.X + rotation.X;
            double num2 = rotation.Y + rotation.Y;
            double num3 = rotation.Z + rotation.Z;
            double num4 = rotation.W * num1;
            double num5 = rotation.W * num2;
            double num6 = rotation.W * num3;
            double num7 = rotation.X * num1;
            double num8 = rotation.X * num2;
            double num9 = rotation.X * num3;
            double num10 = rotation.Y * num2;
            double num11 = rotation.Y * num3;
            double num12 = rotation.Z * num3;
            double num13 = (double)((double)value.X * (1.0 - (double)num10 - (double)num12) + (double)value.Y * ((double)num8 - (double)num6) + (double)value.Z * ((double)num9 + (double)num5));
            double num14 = (double)((double)value.X * ((double)num8 + (double)num6) + (double)value.Y * (1.0 - (double)num7 - (double)num12) + (double)value.Z * ((double)num11 - (double)num4));
            double num15 = (double)((double)value.X * ((double)num9 - (double)num5) + (double)value.Y * ((double)num11 + (double)num4) + (double)value.Z * (1.0 - (double)num7 - (double)num10));
            result.X = num13;
            result.Y = num14;
            result.Z = num15;
            result.W = value.W;
        }

        /// <summary>
        /// Transforms an array of Vector4s by a specified Matrix.
        /// </summary>
        /// <param name="sourceArray">The array of Vector4s to transform.</param><param name="matrix">The transform Matrix to apply.</param><param name="destinationArray">The existing destination array into which the transformed Vector4s are written.</param>
        public static void Transform(Vector4D[] sourceArray, ref MatrixD matrix, Vector4D[] destinationArray)
        {
            for (int index = 0; index < sourceArray.Length; ++index)
            {
                double num1 = sourceArray[index].X;
                double num2 = sourceArray[index].Y;
                double num3 = sourceArray[index].Z;
                double num4 = sourceArray[index].W;
                destinationArray[index].X = (double)((double)num1 * (double)matrix.M11 + (double)num2 * (double)matrix.M21 + (double)num3 * (double)matrix.M31 + (double)num4 * (double)matrix.M41);
                destinationArray[index].Y = (double)((double)num1 * (double)matrix.M12 + (double)num2 * (double)matrix.M22 + (double)num3 * (double)matrix.M32 + (double)num4 * (double)matrix.M42);
                destinationArray[index].Z = (double)((double)num1 * (double)matrix.M13 + (double)num2 * (double)matrix.M23 + (double)num3 * (double)matrix.M33 + (double)num4 * (double)matrix.M43);
                destinationArray[index].W = (double)((double)num1 * (double)matrix.M14 + (double)num2 * (double)matrix.M24 + (double)num3 * (double)matrix.M34 + (double)num4 * (double)matrix.M44);
            }
        }

        /// <summary>
        /// Transforms a specified range in an array of Vector4s by a specified Matrix into a specified range in a destination array.
        /// </summary>
        /// <param name="sourceArray">The array of Vector4s containing the range to transform.</param><param name="sourceIndex">The index in the source array of the first Vector4 to transform.</param><param name="matrix">The transform Matrix to apply.</param><param name="destinationArray">The existing destination array of Vector4s into which to write the results.</param><param name="destinationIndex">The index in the destination array of the first result Vector4 to write.</param><param name="length">The number of Vector4s to transform.</param>
        public static void Transform(Vector4D[] sourceArray, int sourceIndex, ref MatrixD matrix, Vector4D[] destinationArray, int destinationIndex, int length)
        {
            for (; length > 0; --length)
            {
                double num1 = sourceArray[sourceIndex].X;
                double num2 = sourceArray[sourceIndex].Y;
                double num3 = sourceArray[sourceIndex].Z;
                double num4 = sourceArray[sourceIndex].W;
                destinationArray[destinationIndex].X = (double)((double)num1 * (double)matrix.M11 + (double)num2 * (double)matrix.M21 + (double)num3 * (double)matrix.M31 + (double)num4 * (double)matrix.M41);
                destinationArray[destinationIndex].Y = (double)((double)num1 * (double)matrix.M12 + (double)num2 * (double)matrix.M22 + (double)num3 * (double)matrix.M32 + (double)num4 * (double)matrix.M42);
                destinationArray[destinationIndex].Z = (double)((double)num1 * (double)matrix.M13 + (double)num2 * (double)matrix.M23 + (double)num3 * (double)matrix.M33 + (double)num4 * (double)matrix.M43);
                destinationArray[destinationIndex].W = (double)((double)num1 * (double)matrix.M14 + (double)num2 * (double)matrix.M24 + (double)num3 * (double)matrix.M34 + (double)num4 * (double)matrix.M44);
                ++sourceIndex;
                ++destinationIndex;
            }
        }

        /// <summary>
        /// Transforms an array of Vector4s by a specified Quaternion.
        /// </summary>
        /// <param name="sourceArray">The array of Vector4s to transform.</param><param name="rotation">The Quaternion rotation to apply.</param><param name="destinationArray">The existing destination array into which the transformed Vector4s are written.</param>
        public static void Transform(Vector4D[] sourceArray, ref Quaternion rotation, Vector4D[] destinationArray)
        {

            double num1 = rotation.X + rotation.X;
            double num2 = rotation.Y + rotation.Y;
            double num3 = rotation.Z + rotation.Z;
            double num4 = rotation.W * num1;
            double num5 = rotation.W * num2;
            double num6 = rotation.W * num3;
            double num7 = rotation.X * num1;
            double num8 = rotation.X * num2;
            double num9 = rotation.X * num3;
            double num10 = rotation.Y * num2;
            double num11 = rotation.Y * num3;
            double num12 = rotation.Z * num3;
            double num13 = 1f - num10 - num12;
            double num14 = num8 - num6;
            double num15 = num9 + num5;
            double num16 = num8 + num6;
            double num17 = 1f - num7 - num12;
            double num18 = num11 - num4;
            double num19 = num9 - num5;
            double num20 = num11 + num4;
            double num21 = 1f - num7 - num10;
            for (int index = 0; index < sourceArray.Length; ++index)
            {
                double num22 = sourceArray[index].X;
                double num23 = sourceArray[index].Y;
                double num24 = sourceArray[index].Z;
                destinationArray[index].X = (double)((double)num22 * (double)num13 + (double)num23 * (double)num14 + (double)num24 * (double)num15);
                destinationArray[index].Y = (double)((double)num22 * (double)num16 + (double)num23 * (double)num17 + (double)num24 * (double)num18);
                destinationArray[index].Z = (double)((double)num22 * (double)num19 + (double)num23 * (double)num20 + (double)num24 * (double)num21);
                destinationArray[index].W = sourceArray[index].W;
            }
        }

        /// <summary>
        /// Transforms a specified range in an array of Vector4s by a specified Quaternion into a specified range in a destination array.
        /// </summary>
        /// <param name="sourceArray">The array of Vector4s containing the range to transform.</param><param name="sourceIndex">The index in the source array of the first Vector4 to transform.</param><param name="rotation">The Quaternion rotation to apply.</param><param name="destinationArray">The existing destination array of Vector4s into which to write the results.</param><param name="destinationIndex">The index in the destination array of the first result Vector4 to write.</param><param name="length">The number of Vector4s to transform.</param>
        public static void Transform(Vector4D[] sourceArray, int sourceIndex, ref Quaternion rotation, Vector4D[] destinationArray, int destinationIndex, int length)
        {

            double num1 = rotation.X + rotation.X;
            double num2 = rotation.Y + rotation.Y;
            double num3 = rotation.Z + rotation.Z;
            double num4 = rotation.W * num1;
            double num5 = rotation.W * num2;
            double num6 = rotation.W * num3;
            double num7 = rotation.X * num1;
            double num8 = rotation.X * num2;
            double num9 = rotation.X * num3;
            double num10 = rotation.Y * num2;
            double num11 = rotation.Y * num3;
            double num12 = rotation.Z * num3;
            double num13 = 1f - num10 - num12;
            double num14 = num8 - num6;
            double num15 = num9 + num5;
            double num16 = num8 + num6;
            double num17 = 1f - num7 - num12;
            double num18 = num11 - num4;
            double num19 = num9 - num5;
            double num20 = num11 + num4;
            double num21 = 1f - num7 - num10;
            for (; length > 0; --length)
            {
                double num22 = sourceArray[sourceIndex].X;
                double num23 = sourceArray[sourceIndex].Y;
                double num24 = sourceArray[sourceIndex].Z;
                double num25 = sourceArray[sourceIndex].W;
                destinationArray[destinationIndex].X = (double)((double)num22 * (double)num13 + (double)num23 * (double)num14 + (double)num24 * (double)num15);
                destinationArray[destinationIndex].Y = (double)((double)num22 * (double)num16 + (double)num23 * (double)num17 + (double)num24 * (double)num18);
                destinationArray[destinationIndex].Z = (double)((double)num22 * (double)num19 + (double)num23 * (double)num20 + (double)num24 * (double)num21);
                destinationArray[destinationIndex].W = num25;
                ++sourceIndex;
                ++destinationIndex;
            }
        }

        /// <summary>
        /// Returns a vector pointing in the opposite direction.
        /// </summary>
        /// <param name="value">Source vector.</param>
        public static Vector4D Negate(Vector4D value)
        {
            Vector4D vector4;
            vector4.X = -value.X;
            vector4.Y = -value.Y;
            vector4.Z = -value.Z;
            vector4.W = -value.W;
            return vector4;
        }

        /// <summary>
        /// Returns a vector pointing in the opposite direction.
        /// </summary>
        /// <param name="value">Source vector.</param><param name="result">[OutAttribute] Vector pointing in the opposite direction.</param>
        public static void Negate(ref Vector4D value, out Vector4D result)
        {
            result.X = -value.X;
            result.Y = -value.Y;
            result.Z = -value.Z;
            result.W = -value.W;
        }

        /// <summary>
        /// Adds two vectors.
        /// </summary>
        /// <param name="value1">Source vector.</param><param name="value2">Source vector.</param>
        public static Vector4D Add(Vector4D value1, Vector4D value2)
        {
            Vector4D vector4;
            vector4.X = value1.X + value2.X;
            vector4.Y = value1.Y + value2.Y;
            vector4.Z = value1.Z + value2.Z;
            vector4.W = value1.W + value2.W;
            return vector4;
        }

        /// <summary>
        /// Adds two vectors.
        /// </summary>
        /// <param name="value1">Source vector.</param><param name="value2">Source vector.</param><param name="result">[OutAttribute] Sum of the source vectors.</param>
        public static void Add(ref Vector4D value1, ref Vector4D value2, out Vector4D result)
        {
            result.X = value1.X + value2.X;
            result.Y = value1.Y + value2.Y;
            result.Z = value1.Z + value2.Z;
            result.W = value1.W + value2.W;
        }

        /// <summary>
        /// Subtracts a vector from a vector.
        /// </summary>
        /// <param name="value1">Source vector.</param><param name="value2">Source vector.</param>
        public static Vector4 Subtract(Vector4 value1, Vector4 value2)
        {
            Vector4 vector4;
            vector4.X = value1.X - value2.X;
            vector4.Y = value1.Y - value2.Y;
            vector4.Z = value1.Z - value2.Z;
            vector4.W = value1.W - value2.W;
            return vector4;
        }

        /// <summary>
        /// Subtracts a vector from a vector.
        /// </summary>
        /// <param name="value1">Source vector.</param><param name="value2">Source vector.</param><param name="result">[OutAttribute] The result of the subtraction.</param>
        public static void Subtract(ref Vector4D value1, ref Vector4D value2, out Vector4D result)
        {
            result.X = value1.X - value2.X;
            result.Y = value1.Y - value2.Y;
            result.Z = value1.Z - value2.Z;
            result.W = value1.W - value2.W;
        }

        /// <summary>
        /// Multiplies the components of two vectors by each other.
        /// </summary>
        /// <param name="value1">Source vector.</param><param name="value2">Source vector.</param>
        public static Vector4D Multiply(Vector4D value1, Vector4D value2)
        {
            Vector4D vector4;
            vector4.X = value1.X * value2.X;
            vector4.Y = value1.Y * value2.Y;
            vector4.Z = value1.Z * value2.Z;
            vector4.W = value1.W * value2.W;
            return vector4;
        }

        /// <summary>
        /// Multiplies the components of two vectors by each other.
        /// </summary>
        /// <param name="value1">Source vector.</param><param name="value2">Source vector.</param><param name="result">[OutAttribute] The result of the multiplication.</param>
        public static void Multiply(ref Vector4 value1, ref Vector4 value2, out Vector4 result)
        {
            result.X = value1.X * value2.X;
            result.Y = value1.Y * value2.Y;
            result.Z = value1.Z * value2.Z;
            result.W = value1.W * value2.W;
        }

        /// <summary>
        /// Multiplies a vector by a scalar.
        /// </summary>
        /// <param name="value1">Source vector.</param><param name="scaleFactor">Scalar value.</param>
        public static Vector4D Multiply(Vector4D value1, double scaleFactor)
        {
            Vector4D vector4;
            vector4.X = value1.X * scaleFactor;
            vector4.Y = value1.Y * scaleFactor;
            vector4.Z = value1.Z * scaleFactor;
            vector4.W = value1.W * scaleFactor;
            return vector4;
        }

        /// <summary>
        /// Multiplies a vector by a scalar value.
        /// </summary>
        /// <param name="value1">Source vector.</param><param name="scaleFactor">Scalar value.</param><param name="result">[OutAttribute] The result of the multiplication.</param>
        public static void Multiply(ref Vector4D value1, double scaleFactor, out Vector4D result)
        {
            result.X = value1.X * scaleFactor;
            result.Y = value1.Y * scaleFactor;
            result.Z = value1.Z * scaleFactor;
            result.W = value1.W * scaleFactor;
        }

        /// <summary>
        /// Divides the components of a vector by the components of another vector.
        /// </summary>
        /// <param name="value1">Source vector.</param><param name="value2">Divisor vector.</param>
        public static Vector4D Divide(Vector4D value1, Vector4D value2)
        {
            Vector4D vector4;
            vector4.X = value1.X / value2.X;
            vector4.Y = value1.Y / value2.Y;
            vector4.Z = value1.Z / value2.Z;
            vector4.W = value1.W / value2.W;
            return vector4;
        }

        /// <summary>
        /// Divides the components of a vector by the components of another vector.
        /// </summary>
        /// <param name="value1">Source vector.</param><param name="value2">The divisor.</param><param name="result">[OutAttribute] The result of the division.</param>
        public static void Divide(ref Vector4D value1, ref Vector4D value2, out Vector4D result)
        {
            result.X = value1.X / value2.X;
            result.Y = value1.Y / value2.Y;
            result.Z = value1.Z / value2.Z;
            result.W = value1.W / value2.W;
        }

        /// <summary>
        /// Divides a vector by a scalar value.
        /// </summary>
        /// <param name="value1">Source vector.</param><param name="divider">The divisor.</param>
        public static Vector4D Divide(Vector4D value1, double divider)
        {
            double num = 1f / divider;
            Vector4D vector4;
            vector4.X = value1.X * num;
            vector4.Y = value1.Y * num;
            vector4.Z = value1.Z * num;
            vector4.W = value1.W * num;
            return vector4;
        }

        /// <summary>
        /// Divides a vector by a scalar value.
        /// </summary>
        /// <param name="value1">Source vector.</param><param name="divider">The divisor.</param><param name="result">[OutAttribute] The result of the division.</param>
        public static void Divide(ref Vector4D value1, double divider, out Vector4D result)
        {
            double num = 1 / divider;
            result.X = value1.X * num;
            result.Y = value1.Y * num;
            result.Z = value1.Z * num;
            result.W = value1.W * num;
        }

        public static implicit operator Vector4(Vector4D v)
        {
            return new Vector4((float)v.X, (float)v.Y, (float)v.Z, (float)v.W);
        }

        public static implicit operator Vector4D(Vector4 v)
        {
            return new Vector4D((double)v.X, (double)v.Y, (double)v.Z, (double)v.W);
        }
    }
}
