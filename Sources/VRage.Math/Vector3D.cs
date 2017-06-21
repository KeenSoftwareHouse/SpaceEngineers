// Vector3 with double floating point precision

using System;
using System.Diagnostics;
using System.Globalization;

namespace VRageMath
{
    /// <summary>
    /// Defines a vector with three components.
    /// </summary>
    [ProtoBuf.ProtoContract, Serializable]
    public struct Vector3D : IEquatable<Vector3D>
    {
        public static Vector3D Zero = new Vector3D();
        public static Vector3D One = new Vector3D(1, 1, 1);
        public static Vector3D Half = new Vector3D(0.5, 0.5, 0.5);
        public static Vector3D PositiveInfinity = new Vector3D(double.PositiveInfinity);
        public static Vector3D NegativeInfinity = new Vector3D(double.NegativeInfinity);
        public static Vector3D UnitX = new Vector3D(1, 0.0, 0.0);
        public static Vector3D UnitY = new Vector3D(0.0, 1, 0.0);
        public static Vector3D UnitZ = new Vector3D(0.0, 0.0, 1);
        public static Vector3D Up = new Vector3D(0.0, 1, 0.0);
        public static Vector3D Down = new Vector3D(0.0, -1, 0.0);
        public static Vector3D Right = new Vector3D(1, 0.0, 0.0);
        public static Vector3D Left = new Vector3D(-1, 0.0, 0.0);
        public static Vector3D Forward = new Vector3D(0.0, 0.0, -1);
        public static Vector3D Backward = new Vector3D(0.0, 0.0, 1);
        public static Vector3D MaxValue = new Vector3D(double.MaxValue, double.MaxValue, double.MaxValue);
        public static Vector3D MinValue = new Vector3D(double.MinValue, double.MinValue, double.MinValue);
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


        static Vector3D()
        {
        }

        /// <summary>
        /// Initializes a new instance of Vector3.
        /// </summary>
        /// <param name="x">Initial value for the x-component of the vector.</param><param name="y">Initial value for the y-component of the vector.</param><param name="z">Initial value for the z-component of the vector.</param>
        public Vector3D(double x, double y, double z)
        {
            this.X = x;
            this.Y = y;
            this.Z = z;
        }

        /// <summary>
        /// Creates a new instance of Vector3.
        /// </summary>
        /// <param name="value">Value to initialize each component to.</param>
        public Vector3D(double value)
        {
            this.X = this.Y = this.Z = value;
        }

        /// <summary>
        /// Initializes a new instance of Vector3.
        /// </summary>
        /// <param name="value">A vector containing the values to initialize x and y components with.</param><param name="z">Initial value for the z-component of the vector.</param>
        public Vector3D(Vector2 value, double z)
        {
            this.X = value.X;
            this.Y = value.Y;
            this.Z = z;
        }

        public Vector3D(Vector2D value, double z)
        {
            this.X = value.X;
            this.Y = value.Y;
            this.Z = z;
        }

        public Vector3D(Vector4 xyz)
        {
            this.X = xyz.X;
            this.Y = xyz.Y;
            this.Z = xyz.Z;
        }

        public Vector3D(Vector4D xyz)
        {
            this.X = xyz.X;
            this.Y = xyz.Y;
            this.Z = xyz.Z;
        } 

        public Vector3D(Vector3 value)
        {
            X = value.X;
            Y = value.Y;
            Z = value.Z;
        }

        public Vector3D(ref Vector3I value)
        {
            X = value.X;
            Y = value.Y;
            Z = value.Z;
        }

        public Vector3D(Vector3I value)
        {
            X = value.X;
            Y = value.Y;
            Z = value.Z;
        }

        /// <summary>
        /// Returns a vector pointing in the opposite direction.
        /// </summary>
        /// <param name="value">Source vector.</param>
        public static Vector3D operator -(Vector3D value)
        {
            Vector3D vector3;
            vector3.X = -value.X;
            vector3.Y = -value.Y;
            vector3.Z = -value.Z;
            return vector3;
        }


        /// <summary>
        /// Tests vectors for equality.
        /// </summary>
        /// <param name="value1">Source vector.</param><param name="value2">Source vector.</param>
        public static bool operator ==(Vector3D value1, Vector3D value2)
        {
            if ((double)value1.X == (double)value2.X && (double)value1.Y == (double)value2.Y)
                return (double)value1.Z == (double)value2.Z;
            else
                return false;
        }
        public static bool operator ==(Vector3 value1, Vector3D value2)
        {
            if ((double)value1.X == (double)value2.X && (double)value1.Y == (double)value2.Y)
                return (double)value1.Z == (double)value2.Z;
            else
                return false;
        }
        public static bool operator ==(Vector3D value1, Vector3 value2)
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
        public static bool operator !=(Vector3D value1, Vector3D value2)
        {
            if ((double)value1.X == (double)value2.X && (double)value1.Y == (double)value2.Y)
                return (double)value1.Z != (double)value2.Z;
            else
                return true;
        }
        public static bool operator !=(Vector3 value1, Vector3D value2)
        {
            if ((double)value1.X == (double)value2.X && (double)value1.Y == (double)value2.Y)
                return (double)value1.Z != (double)value2.Z;
            else
                return true;
        }
        public static bool operator !=(Vector3D value1, Vector3 value2)
        {
            if ((double)value1.X == (double)value2.X && (double)value1.Y == (double)value2.Y)
                return (double)value1.Z != (double)value2.Z;
            else
                return true;
        }

        public static Vector3D operator %(Vector3D value1, double value2)
        {
            Vector3D vector3;
            vector3.X = value1.X % value2;
            vector3.Y = value1.Y % value2;
            vector3.Z = value1.Z % value2;
            return vector3;
        }

        /// <summary>
        /// Modulo division of two vectors.
        /// </summary>
        /// <param name="value1"></param>
        /// <param name="value2"></param>
        /// <returns></returns>
        public static Vector3D operator %(Vector3D value1, Vector3D value2)
        {
            Vector3D vector3;
            vector3.X = value1.X % value2.X;
            vector3.Y = value1.Y % value2.Y;
            vector3.Z = value1.Z % value2.Z;
            return vector3;
        }

        /// <summary>
        /// Adds two vectors.
        /// </summary>
        /// <param name="value1">Source vector.</param><param name="value2">Source vector.</param>
        public static Vector3D operator +(Vector3D value1, Vector3D value2)
        {
            Vector3D vector3;
            vector3.X = value1.X + value2.X;
            vector3.Y = value1.Y + value2.Y;
            vector3.Z = value1.Z + value2.Z;
            return vector3;
        }

        public static Vector3D operator +(Vector3D value1, Vector3 value2)
        {
            Vector3D vector3;
            vector3.X = value1.X + value2.X;
            vector3.Y = value1.Y + value2.Y;
            vector3.Z = value1.Z + value2.Z;
            return vector3;
        }

        public static Vector3D operator +(Vector3 value1, Vector3D value2)
        {
            Vector3D vector3;
            vector3.X = value1.X + value2.X;
            vector3.Y = value1.Y + value2.Y;
            vector3.Z = value1.Z + value2.Z;
            return vector3;
        }

        public static Vector3D operator +(Vector3D value1, Vector3I value2)
        {
            Vector3D vector3;
            vector3.X = value1.X + value2.X;
            vector3.Y = value1.Y + value2.Y;
            vector3.Z = value1.Z + value2.Z;
            return vector3;
        }

        public static Vector3D operator +(Vector3D value1, double value2)
        {
            Vector3D vector3;
            vector3.X = value1.X + value2;
            vector3.Y = value1.Y + value2;
            vector3.Z = value1.Z + value2;
            return vector3;
        }

        public static Vector3D operator +(Vector3D value1, float value2)
        {
            Vector3D vector3;
            vector3.X = value1.X + value2;
            vector3.Y = value1.Y + value2;
            vector3.Z = value1.Z + value2;
            return vector3;
        }

        /// <summary>
        /// Subtracts a vector from a vector.
        /// </summary>
        /// <param name="value1">Source vector.</param><param name="value2">Source vector.</param>
        public static Vector3D operator -(Vector3D value1, Vector3D value2)
        {
            Vector3D vector3;
            vector3.X = value1.X - value2.X;
            vector3.Y = value1.Y - value2.Y;
            vector3.Z = value1.Z - value2.Z;
            return vector3;
        }

        public static Vector3D operator -(Vector3D value1, Vector3 value2)
        {
            Vector3D vector3;
            vector3.X = value1.X - value2.X;
            vector3.Y = value1.Y - value2.Y;
            vector3.Z = value1.Z - value2.Z;
            return vector3;
        }

        public static Vector3D operator -(Vector3 value1, Vector3D value2)
        {
            Vector3D vector3;
            vector3.X = value1.X - value2.X;
            vector3.Y = value1.Y - value2.Y;
            vector3.Z = value1.Z - value2.Z;
            return vector3;
        }


        public static Vector3D operator -(Vector3D value1, double value2)
        {
            Vector3D vector3;
            vector3.X = value1.X - value2;
            vector3.Y = value1.Y - value2;
            vector3.Z = value1.Z - value2;
            return vector3;
        }

        /// <summary>
        /// Multiplies the components of two vectors by each other.
        /// </summary>
        /// <param name="value1">Source vector.</param><param name="value2">Source vector.</param>
        public static Vector3D operator *(Vector3D value1, Vector3 value2)
        {
            Vector3D vector3;
            vector3.X = value1.X * value2.X;
            vector3.Y = value1.Y * value2.Y;
            vector3.Z = value1.Z * value2.Z;
            return vector3;
        }

        /// <summary>
        /// Multiplies a vector by a scalar value.
        /// </summary>
        /// <param name="value">Source vector.</param><param name="scaleFactor">Scalar value.</param>
        public static Vector3D operator *(Vector3D value, double scaleFactor)
        {
            Vector3D vector3;
            vector3.X = value.X * scaleFactor;
            vector3.Y = value.Y * scaleFactor;
            vector3.Z = value.Z * scaleFactor;
            return vector3;
        }

        /// <summary>
        /// Multiplies a vector by a scalar value.
        /// </summary>
        /// <param name="scaleFactor">Scalar value.</param><param name="value">Source vector.</param>
        public static Vector3D operator *(double scaleFactor, Vector3D value)
        {
            Vector3D vector3;
            vector3.X = value.X * scaleFactor;
            vector3.Y = value.Y * scaleFactor;
            vector3.Z = value.Z * scaleFactor;
            return vector3;
        }

        /// <summary>
        /// Divides the components of a vector by the components of another vector.
        /// </summary>
        /// <param name="value1">Source vector.</param><param name="value2">Divisor vector.</param>
        public static Vector3D operator /(Vector3D value1, Vector3D value2)
        {
            Vector3D vector3;
            vector3.X = value1.X / value2.X;
            vector3.Y = value1.Y / value2.Y;
            vector3.Z = value1.Z / value2.Z;
            return vector3;
        }

        /// <summary>
        /// Divides a vector by a scalar value.
        /// </summary>
        /// <param name="value">Source vector.</param><param name="divider">The divisor.</param>
        public static Vector3D operator /(Vector3D value, double divider)
        {
            double num = 1 / divider;
            Vector3D vector3;
            vector3.X = value.X * num;
            vector3.Y = value.Y * num;
            vector3.Z = value.Z * num;
            return vector3;
        }

        public static Vector3D operator /(double value, Vector3D divider)
        {
            Vector3D vector3;
            vector3.X = value / divider.X;
            vector3.Y = value / divider.Y;
            vector3.Z = value / divider.Z;
            return vector3;
        }

        public static Vector3D Abs(Vector3D value)
        {
            return new Vector3D(value.X < 0 ? -value.X : value.X, value.Y < 0 ? -value.Y : value.Y, value.Z < 0 ? -value.Z : value.Z);
        }

        public static Vector3D Sign(Vector3D value)
        {
            return new Vector3D(Math.Sign(value.X), Math.Sign(value.Y), Math.Sign(value.Z));
        }

        /// <summary>
        /// Returns per component sign, never returns zero.
        /// For zero component returns sign 1.
        /// Faster than Sign.
        /// </summary>
        public static Vector3D SignNonZero(Vector3D value)
        {
            return new Vector3D(value.X < 0 ? -1 : 1, value.Y < 0 ? -1 : 1, value.Z < 0 ? -1 : 1);
        }

        public void Interpolate3(Vector3D v0, Vector3D v1, double rt)
        {
            double s = 1.0 - rt;
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

        public static bool IsUnit(ref Vector3D value)
        {
            var length = value.LengthSquared();
            return length >= 0.9999f && length < 1.0001;
        }

        public static bool ArePerpendicular(ref Vector3D a, ref Vector3D b)
        {
            double dot = a.Dot(b);
            // We want to calculate |D_normalized| < Epsilon, which is equivalent to: D^2 < Epsilon^2 * l1^2 * l2^2
            return dot * dot < 0.00000001 * a.LengthSquared() * b.LengthSquared();
        }

        // Per component IsZero, returns 1 for each component which equals to 0
        public static bool IsZero(Vector3D value)
        {
            return IsZero(value, 0.0001);
        }

        // Per component IsZero, returns 1 for each component which equals to 0
        public static bool IsZero(Vector3D value, double epsilon)
        {
            return Math.Abs(value.X) < epsilon && Math.Abs(value.Y) < epsilon && Math.Abs(value.Z) < epsilon;
        }

        // Per component IsZero, returns 1 for each component which equals to 0
        public static Vector3D IsZeroVector(Vector3D value)
        {
            return new Vector3D(value.X == 0 ? 1 : 0, value.Y == 0 ? 1 : 0, value.Z == 0 ? 1 : 0);
        }

        // Per component IsZero, returns 1 for each component which equals to 0
        public static Vector3D IsZeroVector(Vector3D value, double epsilon)
        {
            return new Vector3D(Math.Abs(value.X) < epsilon ? 1 : 0, Math.Abs(value.Y) < epsilon ? 1 : 0, Math.Abs(value.Z) < epsilon ? 1 : 0);
        }

         // Per component Step (returns 0, 1 or -1 for each component)
        public static Vector3D Step(Vector3D value)
        {
            return new Vector3D(value.X > 0 ? 1 : value.X < 0 ? -1 : 0, value.Y > 0 ? 1 : value.Y < 0 ? -1 : 0, value.Z > 0 ? 1 : value.Z < 0 ? -1 : 0);
        }
        
        /// <summary>
        /// Retrieves a string representation of the current object.
        /// </summary>
        public override string ToString()
        {
            CultureInfo currentCulture = CultureInfo.CurrentCulture;
            return string.Format((IFormatProvider)currentCulture, "{{X:{0} Y:{1} Z:{2}}}", (object)this.X.ToString((IFormatProvider)currentCulture), (object)this.Y.ToString((IFormatProvider)currentCulture), (object)this.Z.ToString((IFormatProvider)currentCulture));
        }

        public static bool TryParse(string str, out Vector3D retval)
        {
            retval = Vector3D.Zero;
            if (str == null) return false;

            int openBraces = 0;
            int start = 0;
            int parsedValues = 0;
            bool success = true;

            for (int i = 0; i < str.Length; ++i)
            {
                if (str[i] == '{')
                {
                    openBraces++;
                }
                else if (str[i] == ':')
                {
                    if (openBraces == 1)
                    {
                        start = i + 1;
                    }
                    else
                    {
                        success = false;
                    }
                }
                else if (str[i] == ' ')
                {
                    if (openBraces == 1)
                    {
                        int len = i - start;
                        string substr = str.Substring(start, len);
                        double val = 0.0f;
                        if (!double.TryParse(substr, out val)) success = false;

                        if (parsedValues == 0)
                        {
                            retval.X = val;
                        }
                        else if (parsedValues == 1)
                        {
                            retval.Y = val;
                        }
                        else if (parsedValues == 2)
                        {
                            retval.Z = val;
                        }
                        else
                        {
                            success = false;
                        }

                        parsedValues++;
                    }
                }
                else if (str[i] == '}')
                {
                    openBraces--;

                    if (openBraces != 0)
                    {
                        success = false;
                    }
                    else
                    {
                        int len = i - start;
                        string substr = str.Substring(start, len);
                        double val = 0.0f;
                        if (!double.TryParse(substr, out val)) success = false;

                        if (parsedValues == 0)
                        {
                            retval.X = val;
                        }
                        else if (parsedValues == 1)
                        {
                            retval.Y = val;
                        }
                        else if (parsedValues == 2)
                        {
                            retval.Z = val;
                        }
                        else
                        {
                            success = false;
                        }

                        parsedValues++;
                    }
                }
            }
            if (openBraces != 0) success = false;

            return success;
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
        public bool Equals(Vector3D other)
        {
            if (this.X == (double)other.X && (double)this.Y == (double)other.Y)
                return (double)this.Z == (double)other.Z;
            else
                return false;
        }

        public bool Equals(Vector3D other, double epsilon)
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
            if (obj is Vector3D)
                flag = this.Equals((Vector3D)obj);
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
        public double Length()
        {
            return (double)Math.Sqrt((double)this.X * (double)this.X + (double)this.Y * (double)this.Y + (double)this.Z * (double)this.Z);
        }

        /// <summary>
        /// Calculates the length of the vector squared.
        /// </summary>
        public double LengthSquared()
        {
            return (double)((double)this.X * (double)this.X + (double)this.Y * (double)this.Y + (double)this.Z * (double)this.Z);
        }

        /// <summary>
        /// Calculates the distance between two vectors.
        /// </summary>
        /// <param name="value1">Source vector.</param><param name="value2">Source vector.</param>
        public static double Distance(Vector3D value1, Vector3D value2)
        {
            double num1 = value1.X - value2.X;
            double num2 = value1.Y - value2.Y;
            double num3 = value1.Z - value2.Z;
            return (double)Math.Sqrt((double)num1 * (double)num1 + (double)num2 * (double)num2 + (double)num3 * (double)num3);
        }

        public static double Distance(Vector3D value1, Vector3 value2)
        {
            double num1 = value1.X - value2.X;
            double num2 = value1.Y - value2.Y;
            double num3 = value1.Z - value2.Z;
            return (double)Math.Sqrt((double)num1 * (double)num1 + (double)num2 * (double)num2 + (double)num3 * (double)num3);
        }

        public static double Distance(Vector3 value1, Vector3D value2)
        {
            double num1 = value1.X - value2.X;
            double num2 = value1.Y - value2.Y;
            double num3 = value1.Z - value2.Z;
            return (double)Math.Sqrt((double)num1 * (double)num1 + (double)num2 * (double)num2 + (double)num3 * (double)num3);
        }
        /// <summary>
        /// Calculates the distance between two vectors.
        /// </summary>
        /// <param name="value1">Source vector.</param><param name="value2">Source vector.</param><param name="result">[OutAttribute] The distance between the vectors.</param>
        public static void Distance(ref Vector3D value1, ref Vector3D value2, out double result)
        {
            double num1 = value1.X - value2.X;
            double num2 = value1.Y - value2.Y;
            double num3 = value1.Z - value2.Z;
            double num4 = (double)((double)num1 * (double)num1 + (double)num2 * (double)num2 + (double)num3 * (double)num3);
            result = (double)Math.Sqrt((double)num4);
        }

        /// <summary>
        /// Calculates the distance between two vectors squared.
        /// </summary>
        /// <param name="value1">Source vector.</param><param name="value2">Source vector.</param>
        public static double DistanceSquared(Vector3D value1, Vector3D value2)
        {
            double num1 = value1.X - value2.X;
            double num2 = value1.Y - value2.Y;
            double num3 = value1.Z - value2.Z;
            return (double)((double)num1 * (double)num1 + (double)num2 * (double)num2 + (double)num3 * (double)num3);
        }

        /// <summary>
        /// Calculates the distance between two vectors squared.
        /// </summary>
        /// <param name="value1">Source vector.</param><param name="value2">Source vector.</param><param name="result">[OutAttribute] The distance between the two vectors squared.</param>
        public static void DistanceSquared(ref Vector3D value1, ref Vector3D value2, out double result)
        {
            double num1 = value1.X - value2.X;
            double num2 = value1.Y - value2.Y;
            double num3 = value1.Z - value2.Z;
            result = (double)((double)num1 * (double)num1 + (double)num2 * (double)num2 + (double)num3 * (double)num3);
        }

        /// <summary>
        /// Calculates rectangular distance (a.k.a. Manhattan distance or Chessboard distace) between two vectors.
        /// </summary>
        /// <param name="value1">Source vector.</param><param name="value2">Source vector.</param>
        public static double RectangularDistance(Vector3D value1, Vector3D value2)
        {
            Vector3D dv = Vector3D.Abs(value1 - value2);
            return dv.X + dv.Y + dv.Z;
        }

        /// <summary>
        /// Calculates rectangular distance (a.k.a. Manhattan distance or Chessboard distace) between two vectors.
        /// </summary>
        /// <param name="value1">Source vector.</param><param name="value2">Source vector.</param>
        public static double RectangularDistance(ref Vector3D value1, ref Vector3D value2)
        {
            Vector3D dv = Vector3D.Abs(value1 - value2);
            return dv.X + dv.Y + dv.Z;
        }

        /// <summary>
        /// Calculates the dot product of two vectors. If the two vectors are unit vectors, the dot product returns a doubleing point value between -1 and 1 that can be used to determine some properties of the angle between two vectors. For example, it can show whether the vectors are orthogonal, parallel, or have an acute or obtuse angle between them.
        /// </summary>
        /// <param name="vector1">Source vector.</param><param name="vector2">Source vector.</param>
        public static double Dot(Vector3D vector1, Vector3D vector2)
        {
            return (double)((double)vector1.X * (double)vector2.X + (double)vector1.Y * (double)vector2.Y + (double)vector1.Z * (double)vector2.Z);
        }
        public static double Dot(Vector3D vector1, Vector3 vector2)
        {
            return (double)((double)vector1.X * (double)vector2.X + (double)vector1.Y * (double)vector2.Y + (double)vector1.Z * (double)vector2.Z);
        }

        /// <summary>
        /// Calculates the dot product of two vectors and writes the result to a user-specified variable. If the two vectors are unit vectors, the dot product returns a doubleing point value between -1 and 1 that can be used to determine some properties of the angle between two vectors. For example, it can show whether the vectors are orthogonal, parallel, or have an acute or obtuse angle between them.
        /// </summary>
        /// <param name="vector1">Source vector.</param><param name="vector2">Source vector.</param><param name="result">[OutAttribute] The dot product of the two vectors.</param>
        public static void Dot(ref Vector3D vector1, ref Vector3D vector2, out double result)
        {
            result = (double)((double)vector1.X * (double)vector2.X + (double)vector1.Y * (double)vector2.Y + (double)vector1.Z * (double)vector2.Z);
        }
        public static void Dot(ref Vector3D vector1, ref Vector3 vector2, out double result)
        {
            result = (double)((double)vector1.X * (double)vector2.X + (double)vector1.Y * (double)vector2.Y + (double)vector1.Z * (double)vector2.Z);
        }
        public static void Dot(ref Vector3 vector1, ref Vector3D vector2, out double result)
        {
            result = (double)((double)vector1.X * (double)vector2.X + (double)vector1.Y * (double)vector2.Y + (double)vector1.Z * (double)vector2.Z);
        }

        public double Dot(Vector3D v)
        {
            return Vector3D.Dot(this, v);
        }

        public double Dot(Vector3 v)
        {
            return X * v.X + Y * v.Y + Z * v.Z;
        }


        public double Dot(ref Vector3D v)
        {
            return X * v.X + Y * v.Y + Z * v.Z;
        }

        public Vector3D Cross(Vector3D v)
        {
            return Vector3D.Cross(this, v);
        }

        /// <summary>
        /// Turns the current vector into a unit vector. The result is a vector one unit in length pointing in the same direction as the original vector.
        /// </summary>
        /// returns length
        public double Normalize()
        {
            double length = (double)Math.Sqrt((double)this.X * (double)this.X + (double)this.Y * (double)this.Y + (double)this.Z * (double)this.Z);
            double num = 1 / length;
            this.X *= num;
            this.Y *= num;
            this.Z *= num;
            return length;
        }

        /// <summary>
        /// Creates a unit vector from the specified vector. The result is a vector one unit in length pointing in the same direction as the original vector.
        /// </summary>
        /// <param name="value">The source Vector3.</param>
        public static Vector3D Normalize(Vector3D value)
        {
            double num = 1 / (double)Math.Sqrt((double)value.X * (double)value.X + (double)value.Y * (double)value.Y + (double)value.Z * (double)value.Z);
            Vector3D vector3;
            vector3.X = value.X * num;
            vector3.Y = value.Y * num;
            vector3.Z = value.Z * num;
            return vector3;
        }

        /// <summary>
        /// Creates a unit vector from the specified vector, writing the result to a user-specified variable. The result is a vector one unit in length pointing in the same direction as the original vector.
        /// </summary>
        /// <param name="value">Source vector.</param><param name="result">[OutAttribute] The normalized vector.</param>
        public static void Normalize(ref Vector3D value, out Vector3D result)
        {
            double num = 1 / (double)Math.Sqrt((double)value.X * (double)value.X + (double)value.Y * (double)value.Y + (double)value.Z * (double)value.Z);
            result.X = value.X * num;
            result.Y = value.Y * num;
            result.Z = value.Z * num;
        }

        /// <summary>
        /// Calculates the cross product of two vectors.
        /// </summary>
        /// <param name="vector1">Source vector.</param><param name="vector2">Source vector.</param>
        public static Vector3D Cross(Vector3D vector1, Vector3D vector2)
        {
            Vector3D vector3;
            vector3.X = (double)((double)vector1.Y * (double)vector2.Z - (double)vector1.Z * (double)vector2.Y);
            vector3.Y = (double)((double)vector1.Z * (double)vector2.X - (double)vector1.X * (double)vector2.Z);
            vector3.Z = (double)((double)vector1.X * (double)vector2.Y - (double)vector1.Y * (double)vector2.X);
            return vector3;
        }

        /// <summary>
        /// Calculates the cross product of two vectors.
        /// </summary>
        /// <param name="vector1">Source vector.</param><param name="vector2">Source vector.</param><param name="result">[OutAttribute] The cross product of the vectors.</param>
        public static void Cross(ref Vector3D vector1, ref Vector3D vector2, out Vector3D result)
        {
            double num1 = (double)((double)vector1.Y * (double)vector2.Z - (double)vector1.Z * (double)vector2.Y);
            double num2 = (double)((double)vector1.Z * (double)vector2.X - (double)vector1.X * (double)vector2.Z);
            double num3 = (double)((double)vector1.X * (double)vector2.Y - (double)vector1.Y * (double)vector2.X);
            result.X = num1;
            result.Y = num2;
            result.Z = num3;
        }

        /// <summary>
        /// Returns the reflection of a vector off a surface that has the specified normal.  Reference page contains code sample.
        /// </summary>
        /// <param name="vector">Source vector.</param><param name="normal">Normal of the surface.</param>
        public static Vector3D Reflect(Vector3D vector, Vector3D normal)
        {
            double num = (double)((double)vector.X * (double)normal.X + (double)vector.Y * (double)normal.Y + (double)vector.Z * (double)normal.Z);
            Vector3D vector3;
            vector3.X = vector.X - 2f * num * normal.X;
            vector3.Y = vector.Y - 2f * num * normal.Y;
            vector3.Z = vector.Z - 2f * num * normal.Z;
            return vector3;
        }

        /// <summary>
        /// Returns the reflection of a vector off a surface that has the specified normal.  Reference page contains code sample.
        /// </summary>
        /// <param name="vector">Source vector.</param><param name="normal">Normal of the surface.</param><param name="result">[OutAttribute] The reflected vector.</param>
        public static void Reflect(ref Vector3D vector, ref Vector3D normal, out Vector3D result)
        {
            double num = (double)((double)vector.X * (double)normal.X + (double)vector.Y * (double)normal.Y + (double)vector.Z * (double)normal.Z);
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
        public static Vector3D Reject(Vector3D vector, Vector3D direction)
        {
            Vector3D result;
            Reject(ref vector, ref direction, out result);
            return result;
        }

        /// <summary>
        /// Returns the rejection of vector from direction, i.e. projection of vector onto the plane defined by origin and direction
        /// </summary>
        /// <param name="vector">Vector which is to be rejected</param>
        /// <param name="direction">Direction from which the input vector will be rejected</param>
        /// <param name="result">Rejection of the vector from the given direction</param>
        public static void Reject(ref Vector3D vector, ref Vector3D direction, out Vector3D result)
        {
            //  Optimized: float inv_denom = 1.0f / Vector3.Dot(normal, normal);
            double invDenom;
            Vector3D.Dot(ref direction, ref direction, out invDenom);
            invDenom = 1.0 / invDenom;

            //  Optimized: float d = Vector3.Dot(normal, p) * inv_denom;
            double d;
            Vector3D.Dot(ref direction, ref vector, out d);
            d = d * invDenom;

            //  Optimized: Vector3 n = normal * inv_denom;
            Vector3D n;
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
        public double Min()
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
        public double AbsMin()
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
        public double Max()
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
        /// Returns the component of the vector, whose absolute value is largest of all the three components.
        /// </summary>
        public double AbsMax()
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

        public int AbsMaxComponent()
        {
            if (Math.Abs(X) > Math.Abs(Y))
            {
                if (Math.Abs(X) > Math.Abs(Z)) 
                    return 0;
                else 
                    return 2;
            }
            else
            {
                if (Math.Abs(Y) > Math.Abs(Z)) 
                    return 1;
                else 
                    return 2;
            }
        }

        /// <summary>
        /// Returns a vector that contains the lowest value from each matching pair of components.
        /// </summary>
        /// <param name="value1">Source vector.</param><param name="value2">Source vector.</param>
        public static Vector3D Min(Vector3D value1, Vector3D value2)
        {
            Vector3D vector3;
            vector3.X = (double)value1.X < (double)value2.X ? value1.X : value2.X;
            vector3.Y = (double)value1.Y < (double)value2.Y ? value1.Y : value2.Y;
            vector3.Z = (double)value1.Z < (double)value2.Z ? value1.Z : value2.Z;
            return vector3;
        }

        /// <summary>
        /// Returns a vector that contains the lowest value from each matching pair of components.
        /// </summary>
        /// <param name="value1">Source vector.</param><param name="value2">Source vector.</param><param name="result">[OutAttribute] The minimized vector.</param>
        public static void Min(ref Vector3D value1, ref Vector3D value2, out Vector3D result)
        {
            result.X = (double)value1.X < (double)value2.X ? value1.X : value2.X;
            result.Y = (double)value1.Y < (double)value2.Y ? value1.Y : value2.Y;
            result.Z = (double)value1.Z < (double)value2.Z ? value1.Z : value2.Z;
        }

        /// <summary>
        /// Returns a vector that contains the highest value from each matching pair of components.
        /// </summary>
        /// <param name="value1">Source vector.</param><param name="value2">Source vector.</param>
        public static Vector3D Max(Vector3D value1, Vector3D value2)
        {
            Vector3D vector3;
            vector3.X = (double)value1.X > (double)value2.X ? value1.X : value2.X;
            vector3.Y = (double)value1.Y > (double)value2.Y ? value1.Y : value2.Y;
            vector3.Z = (double)value1.Z > (double)value2.Z ? value1.Z : value2.Z;
            return vector3;
        }

        /// <summary>
        /// Returns a vector that contains the highest value from each matching pair of components.
        /// </summary>
        /// <param name="value1">Source vector.</param><param name="value2">Source vector.</param><param name="result">[OutAttribute] The maximized vector.</param>
        public static void Max(ref Vector3D value1, ref Vector3D value2, out Vector3D result)
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
        public static void MinMax(ref Vector3D min, ref Vector3D max)
        {
            double tmp;
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
        public static Vector3D DominantAxisProjection(Vector3D value1)
        {
            if (Math.Abs(value1.X) > Math.Abs(value1.Y))
            {
                value1.Y = 0.0;
                if (Math.Abs(value1.X) > Math.Abs(value1.Z))
                    value1.Z = 0.0;
                else
                    value1.X = 0.0;
            }
            else
            {
                value1.X = 0.0;
                if (Math.Abs(value1.Y) > Math.Abs(value1.Z))
                    value1.Z = 0.0;
                else
                    value1.Y = 0.0;
            }
            return value1;
        }

        /// <summary>
        /// Calculates a vector that is equal to the projection of the input vector to the coordinate axis that corresponds
        /// to the original vector's largest value. The result is saved into a user-specified variable.
        /// </summary>
        /// <param name="value1">Source vector.</param><param name="result">[OutAttribute] The projected vector.</param>
        public static void DominantAxisProjection(ref Vector3D value1, out Vector3D result)
        {
            if (Math.Abs(value1.X) > Math.Abs(value1.Y))
            {
                if (Math.Abs(value1.X) > Math.Abs(value1.Z))
                    result = new Vector3D(value1.X, 0.0, 0.0);
                else
                    result = new Vector3D(0.0, 0.0, value1.Z);
            }
            else
            {
                if (Math.Abs(value1.Y) > Math.Abs(value1.Z))
                    result = new Vector3D(0.0, value1.Y, 0.0);
                else
                    result = new Vector3D(0.0, 0.0, value1.Z);
            }
        }

        /// <summary>
        /// Restricts a value to be within a specified range.
        /// </summary>
        /// <param name="value1">The value to clamp.</param><param name="min">The minimum value.</param><param name="max">The maximum value.</param>
        public static Vector3D Clamp(Vector3D value1, Vector3D min, Vector3D max)
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
            Vector3D vector3;
            vector3.X = num3;
            vector3.Y = num6;
            vector3.Z = num9;
            return vector3;
        }

        /// <summary>
        /// Restricts a value to be within a specified range.
        /// </summary>
        /// <param name="value1">The value to clamp.</param><param name="min">The minimum value.</param><param name="max">The maximum value.</param><param name="result">[OutAttribute] The clamped value.</param>
        public static void Clamp(ref Vector3D value1, ref Vector3D min, ref Vector3D max, out Vector3D result)
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
            result.X = num3;
            result.Y = num6;
            result.Z = num9;
        }

		[Unsharper.UnsharperDisableReflection()]
		public static Vector3D ClampToSphere(Vector3D vector, double radius)
        {
            double lsq = vector.LengthSquared();
            double rsq = radius * radius;
            if (lsq > rsq)
            {
                return vector * (double)Math.Sqrt(rsq / lsq);
            }
            return vector;
        }

		[Unsharper.UnsharperDisableReflection()]
		public static void ClampToSphere(ref Vector3D vector, double radius)
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
        public static Vector3D Lerp(Vector3D value1, Vector3D value2, double amount)
        {
            Vector3D vector3;
            vector3.X = value1.X + (value2.X - value1.X) * amount;
            vector3.Y = value1.Y + (value2.Y - value1.Y) * amount;
            vector3.Z = value1.Z + (value2.Z - value1.Z) * amount;
            return vector3;
        }

        /// <summary>
        /// Performs a linear interpolation between two vectors.
        /// </summary>
        /// <param name="value1">Source vector.</param><param name="value2">Source vector.</param><param name="amount">Value between 0 and 1 indicating the weight of value2.</param><param name="result">[OutAttribute] The result of the interpolation.</param>
        public static void Lerp(ref Vector3D value1, ref Vector3D value2, double amount, out Vector3D result)
        {
            result.X = value1.X + (value2.X - value1.X) * amount;
            result.Y = value1.Y + (value2.Y - value1.Y) * amount;
            result.Z = value1.Z + (value2.Z - value1.Z) * amount;
        }

        /// <summary>
        /// Returns a Vector3 containing the 3D Cartesian coordinates of a point specified in Barycentric coordinates relative to a 3D triangle.
        /// </summary>
        /// <param name="value1">A Vector3 containing the 3D Cartesian coordinates of vertex 1 of the triangle.</param><param name="value2">A Vector3 containing the 3D Cartesian coordinates of vertex 2 of the triangle.</param><param name="value3">A Vector3 containing the 3D Cartesian coordinates of vertex 3 of the triangle.</param><param name="amount1">Barycentric coordinate b2, which expresses the weighting factor toward vertex 2 (specified in value2).</param><param name="amount2">Barycentric coordinate b3, which expresses the weighting factor toward vertex 3 (specified in value3).</param>
        public static Vector3D Barycentric(Vector3D value1, Vector3D value2, Vector3D value3, double amount1, double amount2)
        {
            Vector3D vector3;
            vector3.X = (double)((double)value1.X + (double)amount1 * ((double)value2.X - (double)value1.X) + (double)amount2 * ((double)value3.X - (double)value1.X));
            vector3.Y = (double)((double)value1.Y + (double)amount1 * ((double)value2.Y - (double)value1.Y) + (double)amount2 * ((double)value3.Y - (double)value1.Y));
            vector3.Z = (double)((double)value1.Z + (double)amount1 * ((double)value2.Z - (double)value1.Z) + (double)amount2 * ((double)value3.Z - (double)value1.Z));
            return vector3;
        }

        /// <summary>
        /// Returns a Vector3 containing the 3D Cartesian coordinates of a point specified in barycentric (areal) coordinates relative to a 3D triangle.
        /// </summary>
        /// <param name="value1">A Vector3 containing the 3D Cartesian coordinates of vertex 1 of the triangle.</param><param name="value2">A Vector3 containing the 3D Cartesian coordinates of vertex 2 of the triangle.</param><param name="value3">A Vector3 containing the 3D Cartesian coordinates of vertex 3 of the triangle.</param><param name="amount1">Barycentric coordinate b2, which expresses the weighting factor toward vertex 2 (specified in value2).</param><param name="amount2">Barycentric coordinate b3, which expresses the weighting factor toward vertex 3 (specified in value3).</param><param name="result">[OutAttribute] The 3D Cartesian coordinates of the specified point are placed in this Vector3 on exit.</param>
        public static void Barycentric(ref Vector3D value1, ref Vector3D value2, ref Vector3D value3, double amount1, double amount2, out Vector3D result)
        {
            result.X = (double)((double)value1.X + (double)amount1 * ((double)value2.X - (double)value1.X) + (double)amount2 * ((double)value3.X - (double)value1.X));
            result.Y = (double)((double)value1.Y + (double)amount1 * ((double)value2.Y - (double)value1.Y) + (double)amount2 * ((double)value3.Y - (double)value1.Y));
            result.Z = (double)((double)value1.Z + (double)amount1 * ((double)value2.Z - (double)value1.Z) + (double)amount2 * ((double)value3.Z - (double)value1.Z));
        }

        /// <summary>
        /// Compute barycentric coordinates (u, v, w) for point p with respect to triangle (a, b, c)
        /// From : Real-Time Collision Detection, Christer Ericson, CRC Press
        /// 3.4 Barycentric Coordinates
        /// </summary>
        public static void Barycentric(Vector3D p, Vector3D a, Vector3D b, Vector3D c, out double u, out double v, out double w)
        {
            Vector3D v0 = b - a, v1 = c - a, v2 = p - a;
            double d00 = Dot(v0, v0);
            double d01 = Dot(v0, v1);
            double d11 = Dot(v1, v1);
            double d20 = Dot(v2, v0);
            double d21 = Dot(v2, v1);
            double denom = d00 * d11 - d01 * d01;
            v = (d11 * d20 - d01 * d21) / denom;
            w = (d00 * d21 - d01 * d20) / denom;
            u = 1.0f - v - w;
        }

        /// <summary>
        /// Interpolates between two values using a cubic equation.
        /// </summary>
        /// <param name="value1">Source value.</param><param name="value2">Source value.</param><param name="amount">Weighting value.</param>
        public static Vector3D SmoothStep(Vector3D value1, Vector3D value2, double amount)
        {
            amount = (double)amount > 1.0 ? 1 : ((double)amount < 0.0 ? 0.0 : amount);
            amount = (double)((double)amount * (double)amount * (3.0 - 2.0 * (double)amount));
            Vector3D vector3;
            vector3.X = value1.X + (value2.X - value1.X) * amount;
            vector3.Y = value1.Y + (value2.Y - value1.Y) * amount;
            vector3.Z = value1.Z + (value2.Z - value1.Z) * amount;
            return vector3;
        }

        /// <summary>
        /// Interpolates between two values using a cubic equation.
        /// </summary>
        /// <param name="value1">Source vector.</param><param name="value2">Source vector.</param><param name="amount">Weighting value.</param><param name="result">[OutAttribute] The interpolated value.</param>
        public static void SmoothStep(ref Vector3D value1, ref Vector3D value2, double amount, out Vector3D result)
        {
            amount = (double)amount > 1.0 ? 1 : ((double)amount < 0.0 ? 0.0 : amount);
            amount = (double)((double)amount * (double)amount * (3.0 - 2.0 * (double)amount));
            result.X = value1.X + (value2.X - value1.X) * amount;
            result.Y = value1.Y + (value2.Y - value1.Y) * amount;
            result.Z = value1.Z + (value2.Z - value1.Z) * amount;
        }

        /// <summary>
        /// Performs a Catmull-Rom interpolation using the specified positions.
        /// </summary>
        /// <param name="value1">The first position in the interpolation.</param><param name="value2">The second position in the interpolation.</param><param name="value3">The third position in the interpolation.</param><param name="value4">The fourth position in the interpolation.</param><param name="amount">Weighting factor.</param>
        public static Vector3D CatmullRom(Vector3D value1, Vector3D value2, Vector3D value3, Vector3D value4, double amount)
        {
            double num1 = amount * amount;
            double num2 = amount * num1;
            Vector3D vector3;
            vector3.X = (double)(0.5 * (2.0 * (double)value2.X + (-(double)value1.X + (double)value3.X) * (double)amount + (2.0 * (double)value1.X - 5.0 * (double)value2.X + 4.0 * (double)value3.X - (double)value4.X) * (double)num1 + (-(double)value1.X + 3.0 * (double)value2.X - 3.0 * (double)value3.X + (double)value4.X) * (double)num2));
            vector3.Y = (double)(0.5 * (2.0 * (double)value2.Y + (-(double)value1.Y + (double)value3.Y) * (double)amount + (2.0 * (double)value1.Y - 5.0 * (double)value2.Y + 4.0 * (double)value3.Y - (double)value4.Y) * (double)num1 + (-(double)value1.Y + 3.0 * (double)value2.Y - 3.0 * (double)value3.Y + (double)value4.Y) * (double)num2));
            vector3.Z = (double)(0.5 * (2.0 * (double)value2.Z + (-(double)value1.Z + (double)value3.Z) * (double)amount + (2.0 * (double)value1.Z - 5.0 * (double)value2.Z + 4.0 * (double)value3.Z - (double)value4.Z) * (double)num1 + (-(double)value1.Z + 3.0 * (double)value2.Z - 3.0 * (double)value3.Z + (double)value4.Z) * (double)num2));
            return vector3;
        }

        /// <summary>
        /// Performs a Catmull-Rom interpolation using the specified positions.
        /// </summary>
        /// <param name="value1">The first position in the interpolation.</param><param name="value2">The second position in the interpolation.</param><param name="value3">The third position in the interpolation.</param><param name="value4">The fourth position in the interpolation.</param><param name="amount">Weighting factor.</param><param name="result">[OutAttribute] A vector that is the result of the Catmull-Rom interpolation.</param>
        public static void CatmullRom(ref Vector3D value1, ref Vector3D value2, ref Vector3D value3, ref Vector3D value4, double amount, out Vector3D result)
        {
            double num1 = amount * amount;
            double num2 = amount * num1;
            result.X = (double)(0.5 * (2.0 * (double)value2.X + (-(double)value1.X + (double)value3.X) * (double)amount + (2.0 * (double)value1.X - 5.0 * (double)value2.X + 4.0 * (double)value3.X - (double)value4.X) * (double)num1 + (-(double)value1.X + 3.0 * (double)value2.X - 3.0 * (double)value3.X + (double)value4.X) * (double)num2));
            result.Y = (double)(0.5 * (2.0 * (double)value2.Y + (-(double)value1.Y + (double)value3.Y) * (double)amount + (2.0 * (double)value1.Y - 5.0 * (double)value2.Y + 4.0 * (double)value3.Y - (double)value4.Y) * (double)num1 + (-(double)value1.Y + 3.0 * (double)value2.Y - 3.0 * (double)value3.Y + (double)value4.Y) * (double)num2));
            result.Z = (double)(0.5 * (2.0 * (double)value2.Z + (-(double)value1.Z + (double)value3.Z) * (double)amount + (2.0 * (double)value1.Z - 5.0 * (double)value2.Z + 4.0 * (double)value3.Z - (double)value4.Z) * (double)num1 + (-(double)value1.Z + 3.0 * (double)value2.Z - 3.0 * (double)value3.Z + (double)value4.Z) * (double)num2));
        }

        /// <summary>
        /// Performs a Hermite spline interpolation.
        /// </summary>
        /// <param name="value1">Source position vector.</param><param name="tangent1">Source tangent vector.</param><param name="value2">Source position vector.</param><param name="tangent2">Source tangent vector.</param><param name="amount">Weighting factor.</param>
        public static Vector3D Hermite(Vector3D value1, Vector3D tangent1, Vector3D value2, Vector3D tangent2, double amount)
        {
            double num1 = amount * amount;
            double num2 = amount * num1;
            double num3 = (double)(2.0 * (double)num2 - 3.0 * (double)num1 + 1.0);
            double num4 = (double)(-2.0 * (double)num2 + 3.0 * (double)num1);
            double num5 = num2 - 2f * num1 + amount;
            double num6 = num2 - num1;
            Vector3D vector3;
            vector3.X = (double)((double)value1.X * (double)num3 + (double)value2.X * (double)num4 + (double)tangent1.X * (double)num5 + (double)tangent2.X * (double)num6);
            vector3.Y = (double)((double)value1.Y * (double)num3 + (double)value2.Y * (double)num4 + (double)tangent1.Y * (double)num5 + (double)tangent2.Y * (double)num6);
            vector3.Z = (double)((double)value1.Z * (double)num3 + (double)value2.Z * (double)num4 + (double)tangent1.Z * (double)num5 + (double)tangent2.Z * (double)num6);
            return vector3;
        }

        /// <summary>
        /// Performs a Hermite spline interpolation.
        /// </summary>
        /// <param name="value1">Source position vector.</param><param name="tangent1">Source tangent vector.</param><param name="value2">Source position vector.</param><param name="tangent2">Source tangent vector.</param><param name="amount">Weighting factor.</param><param name="result">[OutAttribute] The result of the Hermite spline interpolation.</param>
        public static void Hermite(ref Vector3D value1, ref Vector3D tangent1, ref Vector3D value2, ref Vector3D tangent2, double amount, out Vector3D result)
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
        }

        /// <summary>
        /// Transforms a 3D vector by the given matrix.
        /// </summary>
        /// <param name="position">The source vector.</param><param name="matrix">The transformation matrix.</param>
        public static Vector3D Transform(Vector3D position, MatrixD matrix)
        {
            double num1 = (double)((double)position.X * (double)matrix.M11 + (double)position.Y * (double)matrix.M21 + (double)position.Z * (double)matrix.M31) + matrix.M41;
            double num2 = (double)((double)position.X * (double)matrix.M12 + (double)position.Y * (double)matrix.M22 + (double)position.Z * (double)matrix.M32) + matrix.M42;
            double num3 = (double)((double)position.X * (double)matrix.M13 + (double)position.Y * (double)matrix.M23 + (double)position.Z * (double)matrix.M33) + matrix.M43;
            double num4 = 1 / ((((position.X * matrix.M14) + (position.Y * matrix.M24)) + (position.Z * matrix.M34)) + matrix.M44);
            Vector3D vector3;
            vector3.X = num1 * num4;
            vector3.Y = num2 * num4;
            vector3.Z = num3 * num4;
            return vector3;
        }
        public static Vector3D Transform(Vector3 position, MatrixD matrix)
        {
            double num1 = (double)((double)position.X * (double)matrix.M11 + (double)position.Y * (double)matrix.M21 + (double)position.Z * (double)matrix.M31) + matrix.M41;
            double num2 = (double)((double)position.X * (double)matrix.M12 + (double)position.Y * (double)matrix.M22 + (double)position.Z * (double)matrix.M32) + matrix.M42;
            double num3 = (double)((double)position.X * (double)matrix.M13 + (double)position.Y * (double)matrix.M23 + (double)position.Z * (double)matrix.M33) + matrix.M43;
            double num4 = 1 / ((((position.X * matrix.M14) + (position.Y * matrix.M24)) + (position.Z * matrix.M34)) + matrix.M44);
            Vector3D vector3;
            vector3.X = num1 * num4;
            vector3.Y = num2 * num4;
            vector3.Z = num3 * num4;
            return vector3;
        }

        /// <summary>
        /// Transforms a 3D vector by the given matrix.
        /// </summary>
        /// <param name="position">The source vector.</param><param name="matrix">The transformation matrix.</param>
        public static Vector3D Transform(Vector3D position, Matrix matrix)
        {
            double num1 = (double)((double)position.X * (double)matrix.M11 + (double)position.Y * (double)matrix.M21 + (double)position.Z * (double)matrix.M31) + matrix.M41;
            double num2 = (double)((double)position.X * (double)matrix.M12 + (double)position.Y * (double)matrix.M22 + (double)position.Z * (double)matrix.M32) + matrix.M42;
            double num3 = (double)((double)position.X * (double)matrix.M13 + (double)position.Y * (double)matrix.M23 + (double)position.Z * (double)matrix.M33) + matrix.M43;
            double num4 = 1 / ((((position.X * matrix.M14) + (position.Y * matrix.M24)) + (position.Z * matrix.M34)) + matrix.M44);
            Vector3D vector3;
            vector3.X = num1 * num4;
            vector3.Y = num2 * num4;
            vector3.Z = num3 * num4;
            return vector3;
        }

        public static Vector3D Transform(Vector3D position, ref MatrixD matrix)
        {
            Transform(ref position, ref matrix, out position);
            return position;
        }

        /// <summary>
        /// Transforms a Vector3 by the given Matrix.
        /// </summary>
        /// <param name="position">The source Vector3.</param><param name="matrix">The transformation Matrix.</param><param name="result">[OutAttribute] The transformed vector.</param>
        public static void Transform(ref Vector3D position, ref MatrixD matrix, out Vector3D result)
        {
            double num1 = (double)((double)position.X * (double)matrix.M11 + (double)position.Y * (double)matrix.M21 + (double)position.Z * (double)matrix.M31) + matrix.M41;
            double num2 = (double)((double)position.X * (double)matrix.M12 + (double)position.Y * (double)matrix.M22 + (double)position.Z * (double)matrix.M32) + matrix.M42;
            double num3 = (double)((double)position.X * (double)matrix.M13 + (double)position.Y * (double)matrix.M23 + (double)position.Z * (double)matrix.M33) + matrix.M43;
            double num4 = 1 / ((((position.X * matrix.M14) + (position.Y * matrix.M24)) + (position.Z * matrix.M34)) + matrix.M44);
            result.X = num1 * num4;
            result.Y = num2 * num4;
            result.Z = num3 * num4;
        }

        public static void Transform(ref Vector3 position, ref MatrixD matrix, out Vector3D result)
        {
            double num1 = (double)position.X * (double)matrix.M11 + (double)position.Y * (double)matrix.M21 + (double)position.Z * (double)matrix.M31 + matrix.M41;
            double num2 = (double)((double)position.X * (double)matrix.M12 + (double)position.Y * (double)matrix.M22 + (double)position.Z * (double)matrix.M32) + matrix.M42;
            double num3 = (double)((double)position.X * (double)matrix.M13 + (double)position.Y * (double)matrix.M23 + (double)position.Z * (double)matrix.M33) + matrix.M43;
            double num4 = 1 / ((((position.X * matrix.M14) + (position.Y * matrix.M24)) + (position.Z * matrix.M34)) + matrix.M44);
            result.X = num1 * num4;
            result.Y = num2 * num4;
            result.Z = num3 * num4;
        }

        /**
         * Transform the provided vector only about the rotation, scale and translation terms of a matrix.
         * 
         * This effectively treats the matrix as a 3x4 matrix and the input vector as a 4 dimensional vector with unit W coordinate.
         */
        public static void TransformNoProjection(ref Vector3D vector, ref MatrixD matrix, out Vector3D result)
        {
            double x = (vector.X * matrix.M11 + vector.Y * matrix.M21 + vector.Z * matrix.M31) + matrix.M41;
            double y = (vector.X * matrix.M12 + vector.Y * matrix.M22 + vector.Z * matrix.M32) + matrix.M42;
            double z = (vector.X * matrix.M13 + vector.Y * matrix.M23 + vector.Z * matrix.M33) + matrix.M43;

            result.X = x;
            result.Y = y;
            result.Z = z;
        }

        /**
         * Transform the provided vector only about the rotation and scale terms of a matrix.
         */
        public static void RotateAndScale(ref Vector3D vector, ref MatrixD matrix, out Vector3D result)
        {
            double x = (vector.X * matrix.M11 + vector.Y * matrix.M21 + vector.Z * matrix.M31);
            double y = (vector.X * matrix.M12 + vector.Y * matrix.M22 + vector.Z * matrix.M32);
            double z = (vector.X * matrix.M13 + vector.Y * matrix.M23 + vector.Z * matrix.M33);

            result.X = x;
            result.Y = y;
            result.Z = z;
        }

        public static void Transform(ref Vector3D position, ref MatrixI matrix, out Vector3D result)
        {
            result = position.X * new Vector3D(Base6Directions.GetVector(matrix.Right)) +
                     position.Y * new Vector3D(Base6Directions.GetVector(matrix.Up)) +
                     position.Z * new Vector3D(Base6Directions.GetVector(matrix.Backward)) +
                     new Vector3D(matrix.Translation);
        }


        /// <summary>
        /// Transforms a 3D vector normal by a matrix.
        /// </summary>
        /// <param name="normal">The source vector.</param><param name="matrix">The transformation matrix.</param>
        public static Vector3D TransformNormal(Vector3D normal, Matrix matrix)
        {
            double num1 = (double)((double)normal.X * (double)matrix.M11 + (double)normal.Y * (double)matrix.M21 + (double)normal.Z * (double)matrix.M31);
            double num2 = (double)((double)normal.X * (double)matrix.M12 + (double)normal.Y * (double)matrix.M22 + (double)normal.Z * (double)matrix.M32);
            double num3 = (double)((double)normal.X * (double)matrix.M13 + (double)normal.Y * (double)matrix.M23 + (double)normal.Z * (double)matrix.M33);
            Vector3D vector3;
            vector3.X = num1;
            vector3.Y = num2;
            vector3.Z = num3;
            return vector3;
        }

        /// <summary>
        /// Transforms a 3D vector normal by a matrix.
        /// </summary>
        /// <param name="normal">The source vector.</param><param name="matrix">The transformation matrix.</param>
        public static Vector3D TransformNormal(Vector3 normal, MatrixD matrix)
        {
            double num1 = (double)((double)normal.X * (double)matrix.M11 + (double)normal.Y * (double)matrix.M21 + (double)normal.Z * (double)matrix.M31);
            double num2 = (double)((double)normal.X * (double)matrix.M12 + (double)normal.Y * (double)matrix.M22 + (double)normal.Z * (double)matrix.M32);
            double num3 = (double)((double)normal.X * (double)matrix.M13 + (double)normal.Y * (double)matrix.M23 + (double)normal.Z * (double)matrix.M33);
            Vector3D vector3;
            vector3.X = num1;
            vector3.Y = num2;
            vector3.Z = num3;
            return vector3;
        }

        /// <summary>
        /// Transforms a 3D vector normal by a matrix.
        /// </summary>
        /// <param name="normal">The source vector.</param><param name="matrix">The transformation matrix.</param>
        public static Vector3D TransformNormal(Vector3D normal, MatrixD matrix)
        {
            double num1 = (double)((double)normal.X * (double)matrix.M11 + (double)normal.Y * (double)matrix.M21 + (double)normal.Z * (double)matrix.M31);
            double num2 = (double)((double)normal.X * (double)matrix.M12 + (double)normal.Y * (double)matrix.M22 + (double)normal.Z * (double)matrix.M32);
            double num3 = (double)((double)normal.X * (double)matrix.M13 + (double)normal.Y * (double)matrix.M23 + (double)normal.Z * (double)matrix.M33);
            Vector3D vector3;
            vector3.X = num1;
            vector3.Y = num2;
            vector3.Z = num3;
            return vector3;
        }

        /// <summary>
        /// Transforms a vector normal by a matrix.
        /// </summary>
        /// <param name="normal">The source vector.</param><param name="matrix">The transformation Matrix.</param><param name="result">[OutAttribute] The Vector3 resulting from the transformation.</param>
        public static void TransformNormal(ref Vector3D normal, ref MatrixD matrix, out Vector3D result)
        {
            double num1 = (double)((double)normal.X * (double)matrix.M11 + (double)normal.Y * (double)matrix.M21 + (double)normal.Z * (double)matrix.M31);
            double num2 = (double)((double)normal.X * (double)matrix.M12 + (double)normal.Y * (double)matrix.M22 + (double)normal.Z * (double)matrix.M32);
            double num3 = (double)((double)normal.X * (double)matrix.M13 + (double)normal.Y * (double)matrix.M23 + (double)normal.Z * (double)matrix.M33);
            result.X = num1;
            result.Y = num2;
            result.Z = num3;
        }

        public static void TransformNormal(ref Vector3 normal, ref MatrixD matrix, out Vector3D result)
        {
            double num1 = (double)((double)normal.X * (double)matrix.M11 + (double)normal.Y * (double)matrix.M21 + (double)normal.Z * (double)matrix.M31);
            double num2 = (double)((double)normal.X * (double)matrix.M12 + (double)normal.Y * (double)matrix.M22 + (double)normal.Z * (double)matrix.M32);
            double num3 = (double)((double)normal.X * (double)matrix.M13 + (double)normal.Y * (double)matrix.M23 + (double)normal.Z * (double)matrix.M33);
            result.X = num1;
            result.Y = num2;
            result.Z = num3;
        }

        public static void TransformNormal(ref Vector3D normal, ref MatrixI matrix, out Vector3D result)
        {
            result = normal.X * new Vector3D(Base6Directions.GetVector(matrix.Right)) +
                     normal.Y * new Vector3D(Base6Directions.GetVector(matrix.Up)) +
                     normal.Z * new Vector3D(Base6Directions.GetVector(matrix.Backward));
        }

        public static Vector3D TransformNormal(Vector3D normal, MyBlockOrientation orientation)
        {
            Vector3D retval;
            TransformNormal(ref normal, orientation, out retval);
            return retval;
        }

        public static void TransformNormal(ref Vector3D normal, MyBlockOrientation orientation, out Vector3D result)
        {
            result = - normal.X * new Vector3D(Base6Directions.GetVector(orientation.Left))
                     + normal.Y * new Vector3D(Base6Directions.GetVector(orientation.Up))
                     - normal.Z * new Vector3D(Base6Directions.GetVector(orientation.Forward));
        }

        public static Vector3D TransformNormal(Vector3D normal, ref MatrixD matrix)
        {
            TransformNormal(ref normal, ref matrix, out normal);
            return normal;
        }

        /// <summary>
        /// Transforms a Vector3 by a specified Quaternion rotation.
        /// </summary>
        /// <param name="value">The Vector3 to rotate.</param><param name="rotation">The Quaternion rotation to apply.</param>
        public static Vector3D Transform(Vector3D value, Quaternion rotation)
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
            Vector3D vector3;
            vector3.X = num13;
            vector3.Y = num14;
            vector3.Z = num15;
            return vector3;
        }

        /// <summary>
        /// Transforms a Vector3 by a specified Quaternion rotation.
        /// </summary>
        /// <param name="value">The Vector3 to rotate.</param><param name="rotation">The Quaternion rotation to apply.</param><param name="result">[OutAttribute] An existing Vector3 filled in with the results of the rotation.</param>
        public static void Transform(ref Vector3D value, ref Quaternion rotation, out Vector3D result)
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
        }

        public static void Rotate(ref Vector3D vector, ref MatrixD rotationMatrix, out Vector3D result)
        {
            double num1 = (double)((double)vector.X * (double)rotationMatrix.M11 + (double)vector.Y * (double)rotationMatrix.M21 + (double)vector.Z * (double)rotationMatrix.M31);
            double num2 = (double)((double)vector.X * (double)rotationMatrix.M12 + (double)vector.Y * (double)rotationMatrix.M22 + (double)vector.Z * (double)rotationMatrix.M32);
            double num3 = (double)((double)vector.X * (double)rotationMatrix.M13 + (double)vector.Y * (double)rotationMatrix.M23 + (double)vector.Z * (double)rotationMatrix.M33);
            result.X = num1;
            result.Y = num2;
            result.Z = num3;
        }

        public static Vector3D Rotate(Vector3D vector, MatrixD rotationMatrix)
        {
            Vector3D result;
            Rotate(ref vector, ref rotationMatrix, out result);
            return result;
        }

        /// <summary>
        /// Transforms a source array of Vector3s by a specified Matrix and writes the results to an existing destination array.
        /// </summary>
        /// <param name="sourceArray">The source array.</param><param name="matrix">The transform Matrix to apply.</param><param name="destinationArray">An existing destination array into which the transformed Vector3s are written.</param>
        public static void Transform(Vector3D[] sourceArray, ref MatrixD matrix, Vector3D[] destinationArray)
        {
            for (int index = 0; index < sourceArray.Length; ++index)
            {
                double num1 = sourceArray[index].X;
                double num2 = sourceArray[index].Y;
                double num3 = sourceArray[index].Z;
                destinationArray[index].X = (double)((double)num1 * (double)matrix.M11 + (double)num2 * (double)matrix.M21 + (double)num3 * (double)matrix.M31) + matrix.M41;
                destinationArray[index].Y = (double)((double)num1 * (double)matrix.M12 + (double)num2 * (double)matrix.M22 + (double)num3 * (double)matrix.M32) + matrix.M42;
                destinationArray[index].Z = (double)((double)num1 * (double)matrix.M13 + (double)num2 * (double)matrix.M23 + (double)num3 * (double)matrix.M33) + matrix.M43;
            }
        }

        /// <summary>
        /// Transforms a source array of Vector3s by a specified Matrix and writes the results to an existing destination array.
        /// </summary>
        /// <param name="sourceArray">The source array.</param>
        /// <param name="matrix">The transform Matrix to apply.</param>
        /// <param name="destinationArray">An existing destination array into which the transformed Vector3s are written.</param>
        public static unsafe void Transform(Vector3D[] sourceArray, ref MatrixD matrix, Vector3D* destinationArray)
        {
            for (int index = 0; index < sourceArray.Length; ++index)
            {
                double num1 = sourceArray[index].X;
                double num2 = sourceArray[index].Y;
                double num3 = sourceArray[index].Z;
                destinationArray[index].X = (double)((double)num1 * (double)matrix.M11 + (double)num2 * (double)matrix.M21 + (double)num3 * (double)matrix.M31) + matrix.M41;
                destinationArray[index].Y = (double)((double)num1 * (double)matrix.M12 + (double)num2 * (double)matrix.M22 + (double)num3 * (double)matrix.M32) + matrix.M42;
                destinationArray[index].Z = (double)((double)num1 * (double)matrix.M13 + (double)num2 * (double)matrix.M23 + (double)num3 * (double)matrix.M33) + matrix.M43;
            }
        }

        /// <summary>
        /// Applies a specified transform Matrix to a specified range of an array of Vector3s and writes the results into a specified range of a destination array.
        /// </summary>
        /// <param name="sourceArray">The source array.</param><param name="sourceIndex">The index in the source array at which to start.</param><param name="matrix">The transform Matrix to apply.</param><param name="destinationArray">The existing destination array.</param><param name="destinationIndex">The index in the destination array at which to start.</param><param name="length">The number of Vector3s to transform.</param>
        public static void Transform(Vector3D[] sourceArray, int sourceIndex, ref Matrix matrix, Vector3D[] destinationArray, int destinationIndex, int length)
        {
            for (; length > 0; --length)
            {
                double num1 = sourceArray[sourceIndex].X;
                double num2 = sourceArray[sourceIndex].Y;
                double num3 = sourceArray[sourceIndex].Z;
                destinationArray[destinationIndex].X = (double)((double)num1 * (double)matrix.M11 + (double)num2 * (double)matrix.M21 + (double)num3 * (double)matrix.M31) + matrix.M41;
                destinationArray[destinationIndex].Y = (double)((double)num1 * (double)matrix.M12 + (double)num2 * (double)matrix.M22 + (double)num3 * (double)matrix.M32) + matrix.M42;
                destinationArray[destinationIndex].Z = (double)((double)num1 * (double)matrix.M13 + (double)num2 * (double)matrix.M23 + (double)num3 * (double)matrix.M33) + matrix.M43;
                ++sourceIndex;
                ++destinationIndex;
            }
        }

        /// <summary>
        /// Transforms an array of 3D vector normals by a specified Matrix.
        /// </summary>
        /// <param name="sourceArray">The array of Vector3 normals to transform.</param><param name="matrix">The transform matrix to apply.</param><param name="destinationArray">An existing Vector3 array into which the results of the transforms are written.</param>
        public static void TransformNormal(Vector3D[] sourceArray, ref Matrix matrix, Vector3D[] destinationArray)
        {
            for (int index = 0; index < sourceArray.Length; ++index)
            {
                double num1 = sourceArray[index].X;
                double num2 = sourceArray[index].Y;
                double num3 = sourceArray[index].Z;
                destinationArray[index].X = (double)((double)num1 * (double)matrix.M11 + (double)num2 * (double)matrix.M21 + (double)num3 * (double)matrix.M31);
                destinationArray[index].Y = (double)((double)num1 * (double)matrix.M12 + (double)num2 * (double)matrix.M22 + (double)num3 * (double)matrix.M32);
                destinationArray[index].Z = (double)((double)num1 * (double)matrix.M13 + (double)num2 * (double)matrix.M23 + (double)num3 * (double)matrix.M33);
            }
        }

        /// <summary>
        /// Transforms an array of 3D vector normals by a specified Matrix.
        /// </summary>
        /// <param name="sourceArray">The array of Vector3 normals to transform.</param>
        /// <param name="matrix">The transform matrix to apply.</param>
        /// <param name="destinationArray">An existing Vector3 array into which the results of the transforms are written.</param>
        public static unsafe void TransformNormal(Vector3D[] sourceArray, ref Matrix matrix, Vector3D* destinationArray)
        {
            for (int index = 0; index < sourceArray.Length; ++index)
            {
                double num1 = sourceArray[index].X;
                double num2 = sourceArray[index].Y;
                double num3 = sourceArray[index].Z;
                destinationArray[index].X = (double)((double)num1 * (double)matrix.M11 + (double)num2 * (double)matrix.M21 + (double)num3 * (double)matrix.M31);
                destinationArray[index].Y = (double)((double)num1 * (double)matrix.M12 + (double)num2 * (double)matrix.M22 + (double)num3 * (double)matrix.M32);
                destinationArray[index].Z = (double)((double)num1 * (double)matrix.M13 + (double)num2 * (double)matrix.M23 + (double)num3 * (double)matrix.M33);
            }
        }

        /// <summary>
        /// Transforms a specified range in an array of 3D vector normals by a specified Matrix and writes the results to a specified range in a destination array.
        /// </summary>
        /// <param name="sourceArray">The source array of Vector3 normals.</param><param name="sourceIndex">The starting index in the source array.</param><param name="matrix">The transform Matrix to apply.</param><param name="destinationArray">The destination Vector3 array.</param><param name="destinationIndex">The starting index in the destination array.</param><param name="length">The number of vectors to transform.</param>
        public static void TransformNormal(Vector3D[] sourceArray, int sourceIndex, ref Matrix matrix, Vector3D[] destinationArray, int destinationIndex, int length)
        {
            for (; length > 0; --length)
            {
                double num1 = sourceArray[sourceIndex].X;
                double num2 = sourceArray[sourceIndex].Y;
                double num3 = sourceArray[sourceIndex].Z;
                destinationArray[destinationIndex].X = (double)((double)num1 * (double)matrix.M11 + (double)num2 * (double)matrix.M21 + (double)num3 * (double)matrix.M31);
                destinationArray[destinationIndex].Y = (double)((double)num1 * (double)matrix.M12 + (double)num2 * (double)matrix.M22 + (double)num3 * (double)matrix.M32);
                destinationArray[destinationIndex].Z = (double)((double)num1 * (double)matrix.M13 + (double)num2 * (double)matrix.M23 + (double)num3 * (double)matrix.M33);
                ++sourceIndex;
                ++destinationIndex;
            }
        }

        /// <summary>
        /// Transforms a source array of Vector3s by a specified Quaternion rotation and writes the results to an existing destination array.
        /// </summary>
        /// <param name="sourceArray">The source array.</param><param name="rotation">The Quaternion rotation to apply.</param><param name="destinationArray">An existing destination array into which the transformed Vector3s are written.</param>
        public static void Transform(Vector3D[] sourceArray, ref Quaternion rotation, Vector3D[] destinationArray)
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
            double num13 = 1 - num10 - num12;
            double num14 = num8 - num6;
            double num15 = num9 + num5;
            double num16 = num8 + num6;
            double num17 = 1 - num7 - num12;
            double num18 = num11 - num4;
            double num19 = num9 - num5;
            double num20 = num11 + num4;
            double num21 = 1 - num7 - num10;
            for (int index = 0; index < sourceArray.Length; ++index)
            {
                double num22 = sourceArray[index].X;
                double num23 = sourceArray[index].Y;
                double num24 = sourceArray[index].Z;
                destinationArray[index].X = (double)((double)num22 * (double)num13 + (double)num23 * (double)num14 + (double)num24 * (double)num15);
                destinationArray[index].Y = (double)((double)num22 * (double)num16 + (double)num23 * (double)num17 + (double)num24 * (double)num18);
                destinationArray[index].Z = (double)((double)num22 * (double)num19 + (double)num23 * (double)num20 + (double)num24 * (double)num21);
            }
        }

        /// <summary>
        /// Applies a specified Quaternion rotation to a specified range of an array of Vector3s and writes the results into a specified range of a destination array.
        /// </summary>
        /// <param name="sourceArray">The source array.</param><param name="sourceIndex">The index in the source array at which to start.</param><param name="rotation">The Quaternion rotation to apply.</param><param name="destinationArray">The existing destination array.</param><param name="destinationIndex">The index in the destination array at which to start.</param><param name="length">The number of Vector3s to transform.</param>
        public static void Transform(Vector3D[] sourceArray, int sourceIndex, ref Quaternion rotation, Vector3D[] destinationArray, int destinationIndex, int length)
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
            double num13 = 1 - num10 - num12;
            double num14 = num8 - num6;
            double num15 = num9 + num5;
            double num16 = num8 + num6;
            double num17 = 1 - num7 - num12;
            double num18 = num11 - num4;
            double num19 = num9 - num5;
            double num20 = num11 + num4;
            double num21 = 1 - num7 - num10;
            for (; length > 0; --length)
            {
                double num22 = sourceArray[sourceIndex].X;
                double num23 = sourceArray[sourceIndex].Y;
                double num24 = sourceArray[sourceIndex].Z;
                destinationArray[destinationIndex].X = (double)((double)num22 * (double)num13 + (double)num23 * (double)num14 + (double)num24 * (double)num15);
                destinationArray[destinationIndex].Y = (double)((double)num22 * (double)num16 + (double)num23 * (double)num17 + (double)num24 * (double)num18);
                destinationArray[destinationIndex].Z = (double)((double)num22 * (double)num19 + (double)num23 * (double)num20 + (double)num24 * (double)num21);
                ++sourceIndex;
                ++destinationIndex;
            }
        }

        /// <summary>
        /// Returns a vector pointing in the opposite direction.
        /// </summary>
        /// <param name="value">Source vector.</param>
        public static Vector3D Negate(Vector3D value)
        {
            Vector3D vector3;
            vector3.X = -value.X;
            vector3.Y = -value.Y;
            vector3.Z = -value.Z;
            return vector3;
        }

        /// <summary>
        /// Returns a vector pointing in the opposite direction.
        /// </summary>
        /// <param name="value">Source vector.</param><param name="result">[OutAttribute] Vector pointing in the opposite direction.</param>
        public static void Negate(ref Vector3D value, out Vector3D result)
        {
            result.X = -value.X;
            result.Y = -value.Y;
            result.Z = -value.Z;
        }

        /// <summary>
        /// Adds two vectors.
        /// </summary>
        /// <param name="value1">Source vector.</param><param name="value2">Source vector.</param>
        public static Vector3D Add(Vector3D value1, Vector3D value2)
        {
            Vector3D vector3;
            vector3.X = value1.X + value2.X;
            vector3.Y = value1.Y + value2.Y;
            vector3.Z = value1.Z + value2.Z;
            return vector3;
        }

        /// <summary>
        /// Adds two vectors.
        /// </summary>
        /// <param name="value1">Source vector.</param><param name="value2">Source vector.</param><param name="result">[OutAttribute] Sum of the source vectors.</param>
        public static void Add(ref Vector3D value1, ref Vector3D value2, out Vector3D result)
        {
            result.X = value1.X + value2.X;
            result.Y = value1.Y + value2.Y;
            result.Z = value1.Z + value2.Z;
        }

        /// <summary>
        /// Subtracts a vector from a vector.
        /// </summary>
        /// <param name="value1">Source vector.</param><param name="value2">Source vector.</param>
        public static Vector3D Subtract(Vector3D value1, Vector3D value2)
        {
            Vector3D vector3;
            vector3.X = value1.X - value2.X;
            vector3.Y = value1.Y - value2.Y;
            vector3.Z = value1.Z - value2.Z;
            return vector3;
        }

        /// <summary>
        /// Subtracts a vector from a vector.
        /// </summary>
        /// <param name="value1">Source vector.</param><param name="value2">Source vector.</param><param name="result">[OutAttribute] The result of the subtraction.</param>
        public static void Subtract(ref Vector3D value1, ref Vector3D value2, out Vector3D result)
        {
            result.X = value1.X - value2.X;
            result.Y = value1.Y - value2.Y;
            result.Z = value1.Z - value2.Z;
        }

        /// <summary>
        /// Multiplies the components of two vectors by each other.
        /// </summary>
        /// <param name="value1">Source vector.</param><param name="value2">Source vector.</param>
        public static Vector3D Multiply(Vector3D value1, Vector3D value2)
        {
            Vector3D vector3;
            vector3.X = value1.X * value2.X;
            vector3.Y = value1.Y * value2.Y;
            vector3.Z = value1.Z * value2.Z;
            return vector3;
        }

        /// <summary>
        /// Multiplies the components of two vectors by each other.
        /// </summary>
        /// <param name="value1">Source vector.</param><param name="value2">Source vector.</param><param name="result">[OutAttribute] The result of the multiplication.</param>
        public static void Multiply(ref Vector3D value1, ref Vector3D value2, out Vector3D result)
        {
            result.X = value1.X * value2.X;
            result.Y = value1.Y * value2.Y;
            result.Z = value1.Z * value2.Z;
        }

        /// <summary>
        /// Multiplies a vector by a scalar value.
        /// </summary>
        /// <param name="value1">Source vector.</param><param name="scaleFactor">Scalar value.</param>
        public static Vector3D Multiply(Vector3D value1, double scaleFactor)
        {
            Vector3D vector3;
            vector3.X = value1.X * scaleFactor;
            vector3.Y = value1.Y * scaleFactor;
            vector3.Z = value1.Z * scaleFactor;
            return vector3;
        }

        /// <summary>
        /// Multiplies a vector by a scalar value.
        /// </summary>
        /// <param name="value1">Source vector.</param><param name="scaleFactor">Scalar value.</param><param name="result">[OutAttribute] The result of the multiplication.</param>
        public static void Multiply(ref Vector3D value1, double scaleFactor, out Vector3D result)
        {
            result.X = value1.X * scaleFactor;
            result.Y = value1.Y * scaleFactor;
            result.Z = value1.Z * scaleFactor;
        }

        /// <summary>
        /// Divides the components of a vector by the components of another vector.
        /// </summary>
        /// <param name="value1">Source vector.</param><param name="value2">Divisor vector.</param>
        public static Vector3D Divide(Vector3D value1, Vector3D value2)
        {
            Vector3D vector3;
            vector3.X = value1.X / value2.X;
            vector3.Y = value1.Y / value2.Y;
            vector3.Z = value1.Z / value2.Z;
            return vector3;
        }

        /// <summary>
        /// Divides the components of a vector by the components of another vector.
        /// </summary>
        /// <param name="value1">Source vector.</param><param name="value2">The divisor.</param><param name="result">[OutAttribute] The result of the division.</param>
        public static void Divide(ref Vector3D value1, ref Vector3D value2, out Vector3D result)
        {
            result.X = value1.X / value2.X;
            result.Y = value1.Y / value2.Y;
            result.Z = value1.Z / value2.Z;
        }

        /// <summary>
        /// Divides a vector by a scalar value.
        /// </summary>
        /// <param name="value1">Source vector.</param><param name="value2">The divisor.</param>
        public static Vector3D Divide(Vector3D value1, double value2)
        {
            double num = 1 / value2;
            Vector3D vector3;
            vector3.X = value1.X * num;
            vector3.Y = value1.Y * num;
            vector3.Z = value1.Z * num;
            return vector3;
        }

        /// <summary>
        /// Divides a vector by a scalar value.
        /// </summary>
        /// <param name="value1">Source vector.</param><param name="value2">The divisor.</param><param name="result">[OutAttribute] The result of the division.</param>
        public static void Divide(ref Vector3D value1, double value2, out Vector3D result)
        {
            double num = 1 / value2;
            result.X = value1.X * num;
            result.Y = value1.Y * num;
            result.Z = value1.Z * num;
        }

		[Unsharper.UnsharperDisableReflection()]
		public static Vector3D CalculatePerpendicularVector(Vector3D v)
        {
            Vector3D result;
            v.CalculatePerpendicularVector(out result);
            return result;
        }

		[Unsharper.UnsharperDisableReflection()]
		public void CalculatePerpendicularVector(out Vector3D result)
        {
            const double threshold = 0.0001f;
            Debug.Assert(Math.Abs(1f - this.Length()) < threshold, "Input must be unit length vector.");
            if (Math.Abs(Y + Z) > threshold || Math.Abs(X) > threshold)
                result = new Vector3D(-(Y + Z), X, X);
            else
                result = new Vector3D(Z, Z, -(X + Y));
            Vector3D.Normalize(ref result, out result);
            Debug.Assert(Math.Abs(result.Length() - this.Length()) < threshold);
            Debug.Assert(Math.Abs(Vector3D.Dot(this, result)) < threshold);
            Debug.Assert(Math.Abs(Vector3D.Dot(this, Vector3D.Cross(result, this))) < threshold);
        }

        public static void GetAzimuthAndElevation(Vector3D v, out double azimuth, out double elevation)
        {
            double elevationSin, azimuthCos;
            Vector3D.Dot(ref v, ref Vector3D.Up, out elevationSin);
            v.Y = 0f;
            v.Normalize();
            Vector3D.Dot(ref v, ref Vector3D.Forward, out azimuthCos);
            elevation = Math.Asin(elevationSin);
            if (v.X >= 0)
            {
                azimuth = -Math.Acos(azimuthCos);
            }
            else
            {
                azimuth = Math.Acos(azimuthCos);
            }
        }

        public static void CreateFromAzimuthAndElevation(double azimuth, double elevation, out Vector3D direction)
        {
            var yRot = MatrixD.CreateRotationY(azimuth);
            var xRot = MatrixD.CreateRotationX(elevation);
            direction = Vector3D.Forward;
            Vector3D.TransformNormal(ref direction, ref xRot, out direction);
            Vector3D.TransformNormal(ref direction, ref yRot, out direction);
        }

        public double Sum
        {
            get
            {
                return X + Y + Z;
            }
        }

        public double Volume
        {
            get
            {
                return X * Y * Z;
            }
        }

        public long VolumeInt(double multiplier)
        {
            return (long)(X * multiplier) * (long)(Y * multiplier) * (long)(Z * multiplier);
        }

        public bool IsInsideInclusive(ref Vector3D min, ref Vector3D max)
        {
            return
                min.X <= this.X && this.X <= max.X &&
                min.Y <= this.Y && this.Y <= max.Y &&
                min.Z <= this.Z && this.Z <= max.Z;
        }

        public static Vector3D SwapYZCoordinates(Vector3D v)
        {
            return new Vector3D(v.X, v.Z, -v.Y);
        }

        public double GetDim(int i)
        {
            switch (i)
            {
                case 0: return X;
                case 1: return Y;
                case 2: return Z;
                default: return GetDim((i % 3 + 3) % 3);  // reduce to 0..2
            }
        }

        public void SetDim(int i, double value)
        {
            switch (i)
            {
                case 0: X = value; break;
                case 1: Y = value; break;
                case 2: Z = value; break;
                default: SetDim((i % 3 + 3) % 3, value); break;  // reduce to 0..2
            }
        }

        public static implicit operator Vector3(Vector3D v)
        {
            return new Vector3((float)v.X,(float)v.Y,(float)v.Z);
        }

        public static implicit operator Vector3D(Vector3 v)
        {
            return new Vector3D((double)v.X, (double)v.Y, (double)v.Z);
        }

        public static Vector3I Round(Vector3D vect3d)
        {
            return new Vector3I((vect3d + .5));
        }

        public static Vector3I Floor(Vector3D vect3d)
        {
            return new Vector3I((int)Math.Floor(vect3d.X), (int)Math.Floor(vect3d.Y), (int)Math.Floor(vect3d.Z));
        }

        public static void Fract(ref Vector3D o, out Vector3D r)
        {
            r.X = o.X - Math.Floor(o.X);
            r.Y = o.Y - Math.Floor(o.Y);
            r.Z = o.Z - Math.Floor(o.Z);
        }

        public static Vector3D Round(Vector3D v, int numDecimals)
        {
            return new Vector3D(Math.Round(v.X, numDecimals), Math.Round(v.Y, numDecimals), Math.Round(v.Z, numDecimals));
        }

        public static void Abs(ref Vector3D vector3D, out Vector3D abs)
        {
            abs.X = Math.Abs(vector3D.X);
            abs.Y = Math.Abs(vector3D.Y);
            abs.Z = Math.Abs(vector3D.Z);
        }
    }

    public static class NullableVector3DExtensions
    {
        public static bool IsValid(this Vector3D? value)
        {
            return !value.HasValue || value.Value.IsValid();
        }

        [Conditional("DEBUG")]
        public static void AssertIsValid(this Vector3D? value)
        {
            Debug.Assert(value.IsValid());
        }

    }

}
