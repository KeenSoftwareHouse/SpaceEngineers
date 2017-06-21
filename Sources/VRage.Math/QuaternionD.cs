using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace VRageMath
{
    /// <summary>
    /// Defines a four-dimensional vector (x,y,z,w), which is used to efficiently rotate an object about the (x, y, z) vector by the angle theta, where w = cos(theta/2).
    /// Uses double precision floating point numbers for calculation and storage
    /// </summary>
    [Serializable, ProtoBuf.ProtoContract]
    public struct QuaternionD
    {
        public static QuaternionD Identity = new QuaternionD(0.0, 0.0, 0.0, 1.0);
        /// <summary>
        /// Specifies the x-value of the vector component of the quaternion.
        /// </summary>
        [ProtoBuf.ProtoMember]
        public double X;
        /// <summary>
        /// Specifies the y-value of the vector component of the quaternion.
        /// </summary>
        [ProtoBuf.ProtoMember]
        public double Y;
        /// <summary>
        /// Specifies the z-value of the vector component of the quaternion.
        /// </summary>
        [ProtoBuf.ProtoMember]
        public double Z;
        /// <summary>
        /// Specifies the rotation component of the quaternion.
        /// </summary>
        [ProtoBuf.ProtoMember]
        public double W;

        static QuaternionD()
        {
        }

        /// <summary>
        /// Initializes a new instance of QuaternionD.
        /// </summary>
        /// <param name="x">The x-value of the quaternion.</param><param name="y">The y-value of the quaternion.</param><param name="z">The z-value of the quaternion.</param><param name="w">The w-value of the quaternion.</param>
        public QuaternionD(double x, double y, double z, double w)
        {
            this.X = x;
            this.Y = y;
            this.Z = z;
            this.W = w;
        }

        /// <summary>
        /// Initializes a new instance of QuaternionD.
        /// </summary>
        /// <param name="vectorPart">The vector component of the quaternion.</param><param name="scalarPart">The rotation component of the quaternion.</param>
        public QuaternionD(Vector3D vectorPart, double scalarPart)
        {
            this.X = vectorPart.X;
            this.Y = vectorPart.Y;
            this.Z = vectorPart.Z;
            this.W = scalarPart;
        }

        /// <summary>
        /// Flips the sign of each component of the quaternion.
        /// </summary>
        /// <param name="quaternion">Source quaternion.</param>
        public static QuaternionD operator -(QuaternionD quaternion)
        {
            QuaternionD quaternion1;
            quaternion1.X = -quaternion.X;
            quaternion1.Y = -quaternion.Y;
            quaternion1.Z = -quaternion.Z;
            quaternion1.W = -quaternion.W;
            return quaternion1;
        }

        /// <summary>
        /// Compares two Quaternions for equality.
        /// </summary>
        /// <param name="quaternion1">Source QuaternionD.</param><param name="quaternion2">Source QuaternionD.</param>
        public static bool operator ==(QuaternionD quaternion1, QuaternionD quaternion2)
        {
            if (quaternion1.X == quaternion2.X && quaternion1.Y == quaternion2.Y && quaternion1.Z == quaternion2.Z)
                return quaternion1.W == quaternion2.W;
            else
                return false;
        }

        /// <summary>
        /// Compare two Quaternions for inequality.
        /// </summary>
        /// <param name="quaternion1">Source QuaternionD.</param><param name="quaternion2">Source QuaternionD.</param>
        public static bool operator !=(QuaternionD quaternion1, QuaternionD quaternion2)
        {
            if (quaternion1.X == quaternion2.X && quaternion1.Y == quaternion2.Y && quaternion1.Z == quaternion2.Z)
                return quaternion1.W != quaternion2.W;
            else
                return true;
        }

        /// <summary>
        /// Adds two Quaternions.
        /// </summary>
        /// <param name="quaternion1">QuaternionD to add.</param><param name="quaternion2">QuaternionD to add.</param>
        public static QuaternionD operator +(QuaternionD quaternion1, QuaternionD quaternion2)
        {
            QuaternionD quaternion;
            quaternion.X = quaternion1.X + quaternion2.X;
            quaternion.Y = quaternion1.Y + quaternion2.Y;
            quaternion.Z = quaternion1.Z + quaternion2.Z;
            quaternion.W = quaternion1.W + quaternion2.W;
            return quaternion;
        }

        /// <summary>
        /// Subtracts a quaternion from another quaternion.
        /// </summary>
        /// <param name="quaternion1">Source quaternion.</param><param name="quaternion2">Source quaternion.</param>
        public static QuaternionD operator -(QuaternionD quaternion1, QuaternionD quaternion2)
        {
            QuaternionD quaternion;
            quaternion.X = quaternion1.X - quaternion2.X;
            quaternion.Y = quaternion1.Y - quaternion2.Y;
            quaternion.Z = quaternion1.Z - quaternion2.Z;
            quaternion.W = quaternion1.W - quaternion2.W;
            return quaternion;
        }

        /// <summary>
        /// Multiplies two quaternions.
        /// </summary>
        /// <param name="quaternion1">Source quaternion.</param><param name="quaternion2">Source quaternion.</param>
        public static QuaternionD operator *(QuaternionD quaternion1, QuaternionD quaternion2)
        {
            double num1 = quaternion1.X;
            double num2 = quaternion1.Y;
            double num3 = quaternion1.Z;
            double num4 = quaternion1.W;
            double num5 = quaternion2.X;
            double num6 = quaternion2.Y;
            double num7 = quaternion2.Z;
            double num8 = quaternion2.W;
            double num9 = (num2 * num7 - num3 * num6);
            double num10 = (num3 * num5 - num1 * num7);
            double num11 = (num1 * num6 - num2 * num5);
            double num12 = (num1 * num5 + num2 * num6 + num3 * num7);
            QuaternionD quaternion;
            quaternion.X = (num1 * num8 + num5 * num4) + num9;
            quaternion.Y = (num2 * num8 + num6 * num4) + num10;
            quaternion.Z = (num3 * num8 + num7 * num4) + num11;
            quaternion.W = num4 * num8 - num12;
            return quaternion;
        }

        /// <summary>
        /// Multiplies a quaternion by a scalar value.
        /// </summary>
        /// <param name="quaternion1">Source quaternion.</param><param name="scaleFactor">Scalar value.</param>
        public static QuaternionD operator *(QuaternionD quaternion1, double scaleFactor)
        {
            QuaternionD quaternion;
            quaternion.X = quaternion1.X * scaleFactor;
            quaternion.Y = quaternion1.Y * scaleFactor;
            quaternion.Z = quaternion1.Z * scaleFactor;
            quaternion.W = quaternion1.W * scaleFactor;
            return quaternion;
        }

        /// <summary>
        /// Divides a QuaternionD by another QuaternionD.
        /// </summary>
        /// <param name="quaternion1">Source QuaternionD.</param><param name="quaternion2">The divisor.</param>
        public static QuaternionD operator /(QuaternionD quaternion1, QuaternionD quaternion2)
        {
            double num1 = quaternion1.X;
            double num2 = quaternion1.Y;
            double num3 = quaternion1.Z;
            double num4 = quaternion1.W;
            double num5 = 1.0 / (quaternion2.X * quaternion2.X + quaternion2.Y * quaternion2.Y + quaternion2.Z * quaternion2.Z + quaternion2.W * quaternion2.W);
            double num6 = -quaternion2.X * num5;
            double num7 = -quaternion2.Y * num5;
            double num8 = -quaternion2.Z * num5;
            double num9 = quaternion2.W * num5;
            double num10 = (num2 * num8 - num3 * num7);
            double num11 = (num3 * num6 - num1 * num8);
            double num12 = (num1 * num7 - num2 * num6);
            double num13 = (num1 * num6 + num2 * num7 + num3 * num8);
            QuaternionD quaternion;
            quaternion.X = (num1 * num9 + num6 * num4) + num10;
            quaternion.Y = (num2 * num9 + num7 * num4) + num11;
            quaternion.Z = (num3 * num9 + num8 * num4) + num12;
            quaternion.W = num4 * num9 - num13;
            return quaternion;
        }

        /// <summary>
        /// Retireves a string representation of the current object.
        /// </summary>
        public override string ToString()
        {
            CultureInfo currentCulture = CultureInfo.CurrentCulture;
            return string.Format((IFormatProvider)currentCulture, "{{X:{0} Y:{1} Z:{2} W:{3}}}", (object)this.X.ToString((IFormatProvider)currentCulture), (object)this.Y.ToString((IFormatProvider)currentCulture), (object)this.Z.ToString((IFormatProvider)currentCulture), (object)this.W.ToString((IFormatProvider)currentCulture));
        }

        /// <summary>
        /// Determines whether the specified Object is equal to the QuaternionD.
        /// </summary>
        /// <param name="other">The QuaternionD to compare with the current QuaternionD.</param>
        public bool Equals(QuaternionD other)
        {
            if (this.X == other.X && this.Y == other.Y && this.Z == other.Z)
                return this.W == other.W;
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
            if (obj is QuaternionD)
                flag = this.Equals((QuaternionD)obj);
            return flag;
        }

        /// <summary>
        /// Get the hash code of this object.
        /// </summary>
        public override int GetHashCode()
        {
            return this.X.GetHashCode() + this.Y.GetHashCode() + this.Z.GetHashCode() + this.W.GetHashCode();
        }

        /// <summary>
        /// Calculates the length squared of a QuaternionD.
        /// </summary>
        public double LengthSquared()
        {
            return (this.X * this.X + this.Y * this.Y + this.Z * this.Z + this.W * this.W);
        }

        /// <summary>
        /// Calculates the length of a QuaternionD.
        /// </summary>
        public double Length()
        {
            return Math.Sqrt(this.X * this.X + this.Y * this.Y + this.Z * this.Z + this.W * this.W);
        }

        /// <summary>
        /// Divides each component of the quaternion by the length of the quaternion.
        /// </summary>
        public void Normalize()
        {
            double num = 1.0 / Math.Sqrt(this.X * this.X + this.Y * this.Y + this.Z * this.Z + this.W * this.W);
            this.X *= num;
            this.Y *= num;
            this.Z *= num;
            this.W *= num;
        }

        public void GetAxisAngle(out Vector3D axis, out double angle)
        {
            axis.X = X;
            axis.Y = Y;
            axis.Z = Z;
            double sinAngleHalf = axis.Length();
            double cosAngleHalf = W;
            if (sinAngleHalf != 0)
            {
                axis.X /= sinAngleHalf;
                axis.Y /= sinAngleHalf;
                axis.Z /= sinAngleHalf;
            }
            angle = Math.Atan2(sinAngleHalf, cosAngleHalf) * 2;
        }

        /// <summary>
        /// Divides each component of the quaternion by the length of the quaternion.
        /// </summary>
        /// <param name="quaternion">Source quaternion.</param>
        public static QuaternionD Normalize(QuaternionD quaternion)
        {
            double num = 1.0 / Math.Sqrt(quaternion.X * quaternion.X + quaternion.Y * quaternion.Y + quaternion.Z * quaternion.Z + quaternion.W * quaternion.W);
            QuaternionD quaternion1;
            quaternion1.X = quaternion.X * num;
            quaternion1.Y = quaternion.Y * num;
            quaternion1.Z = quaternion.Z * num;
            quaternion1.W = quaternion.W * num;
            return quaternion1;
        }

        /// <summary>
        /// Divides each component of the quaternion by the length of the quaternion.
        /// </summary>
        /// <param name="quaternion">Source quaternion.</param><param name="result">[OutAttribute] Normalized quaternion.</param>
        public static void Normalize(ref QuaternionD quaternion, out QuaternionD result)
        {
            double num = 1.0 / Math.Sqrt(quaternion.X * quaternion.X + quaternion.Y * quaternion.Y + quaternion.Z * quaternion.Z + quaternion.W * quaternion.W);
            result.X = quaternion.X * num;
            result.Y = quaternion.Y * num;
            result.Z = quaternion.Z * num;
            result.W = quaternion.W * num;
        }

        /// <summary>
        /// Transforms this QuaternionD into its conjugate.
        /// </summary>
        public void Conjugate()
        {
            this.X = -this.X;
            this.Y = -this.Y;
            this.Z = -this.Z;
        }

        /// <summary>
        /// Returns the conjugate of a specified QuaternionD.
        /// </summary>
        /// <param name="value">The QuaternionD of which to return the conjugate.</param>
        public static QuaternionD Conjugate(QuaternionD value)
        {
            QuaternionD quaternion;
            quaternion.X = -value.X;
            quaternion.Y = -value.Y;
            quaternion.Z = -value.Z;
            quaternion.W = value.W;
            return quaternion;
        }

        /// <summary>
        /// Returns the conjugate of a specified QuaternionD.
        /// </summary>
        /// <param name="value">The QuaternionD of which to return the conjugate.</param><param name="result">[OutAttribute] An existing QuaternionD filled in to be the conjugate of the specified one.</param>
        public static void Conjugate(ref QuaternionD value, out QuaternionD result)
        {
            result.X = -value.X;
            result.Y = -value.Y;
            result.Z = -value.Z;
            result.W = value.W;
        }

        /// <summary>
        /// Returns the inverse of a QuaternionD.
        /// </summary>
        /// <param name="quaternion">Source QuaternionD.</param>
        public static QuaternionD Inverse(QuaternionD quaternion)
        {
            double num = 1.0 / (quaternion.X * quaternion.X + quaternion.Y * quaternion.Y + quaternion.Z * quaternion.Z + quaternion.W * quaternion.W);
            QuaternionD quaternion1;
            quaternion1.X = -quaternion.X * num;
            quaternion1.Y = -quaternion.Y * num;
            quaternion1.Z = -quaternion.Z * num;
            quaternion1.W = quaternion.W * num;
            return quaternion1;
        }

        /// <summary>
        /// Returns the inverse of a QuaternionD.
        /// </summary>
        /// <param name="quaternion">Source QuaternionD.</param><param name="result">[OutAttribute] The inverse of the QuaternionD.</param>
        public static void Inverse(ref QuaternionD quaternion, out QuaternionD result)
        {
            double num = 1.0 / (quaternion.X * quaternion.X + quaternion.Y * quaternion.Y + quaternion.Z * quaternion.Z + quaternion.W * quaternion.W);
            result.X = -quaternion.X * num;
            result.Y = -quaternion.Y * num;
            result.Z = -quaternion.Z * num;
            result.W = quaternion.W * num;
        }

        /// <summary>
        /// Creates a QuaternionD from a vector and an angle to rotate about the vector.
        /// </summary>
        /// <param name="axis">The vector to rotate around.</param><param name="angle">The angle to rotate around the vector.</param>
        public static QuaternionD CreateFromAxisAngle(Vector3D axis, double angle)
        {
            double num1 = angle * 0.5;
            double num2 = Math.Sin(num1);
            double num3 = Math.Cos(num1);
            QuaternionD quaternion;
            quaternion.X = axis.X * num2;
            quaternion.Y = axis.Y * num2;
            quaternion.Z = axis.Z * num2;
            quaternion.W = num3;
            return quaternion;
        }

        /// <summary>
        /// Creates a QuaternionD from a vector and an angle to rotate about the vector.
        /// </summary>
        /// <param name="axis">The vector to rotate around.</param><param name="angle">The angle to rotate around the vector.</param><param name="result">[OutAttribute] The created QuaternionD.</param>
        public static void CreateFromAxisAngle(ref Vector3D axis, double angle, out QuaternionD result)
        {
            double num1 = angle * 0.5;
            double num2 = Math.Sin(num1);
            double num3 = Math.Cos(num1);
            result.X = axis.X * num2;
            result.Y = axis.Y * num2;
            result.Z = axis.Z * num2;
            result.W = num3;
        }

        /// <summary>
        /// Creates a new QuaternionD from specified yaw, pitch, and roll angles.
        /// </summary>
        /// <param name="yaw">The yaw angle, in radians, around the y-axis.</param><param name="pitch">The pitch angle, in radians, around the x-axis.</param><param name="roll">The roll angle, in radians, around the z-axis.</param>
        public static QuaternionD CreateFromYawPitchRoll(double yaw, double pitch, double roll)
        {
            double num1 = roll * 0.5;
            double num2 = Math.Sin(num1);
            double num3 = Math.Cos(num1);
            double num4 = pitch * 0.5;
            double num5 = Math.Sin(num4);
            double num6 = Math.Cos(num4);
            double num7 = yaw * 0.5;
            double num8 = Math.Sin(num7);
            double num9 = Math.Cos(num7);
            QuaternionD quaternion;
            quaternion.X = (num9 * num5 * num3 + num8 * num6 * num2);
            quaternion.Y = (num8 * num6 * num3 - num9 * num5 * num2);
            quaternion.Z = (num9 * num6 * num2 - num8 * num5 * num3);
            quaternion.W = (num9 * num6 * num3 + num8 * num5 * num2);
            return quaternion;
        }

        /// <summary>
        /// Creates a new QuaternionD from specified yaw, pitch, and roll angles.
        /// </summary>
        /// <param name="yaw">The yaw angle, in radians, around the y-axis.</param><param name="pitch">The pitch angle, in radians, around the x-axis.</param><param name="roll">The roll angle, in radians, around the z-axis.</param><param name="result">[OutAttribute] An existing QuaternionD filled in to express the specified yaw, pitch, and roll angles.</param>
        public static void CreateFromYawPitchRoll(double yaw, double pitch, double roll, out QuaternionD result)
        {
            double num1 = roll * 0.5;
            double num2 = Math.Sin(num1);
            double num3 = Math.Cos(num1);
            double num4 = pitch * 0.5;
            double num5 = Math.Sin(num4);
            double num6 = Math.Cos(num4);
            double num7 = yaw * 0.5;
            double num8 = Math.Sin(num7);
            double num9 = Math.Cos(num7);
            result.X = (num9 * num5 * num3 + num8 * num6 * num2);
            result.Y = (num8 * num6 * num3 - num9 * num5 * num2);
            result.Z = (num9 * num6 * num2 - num8 * num5 * num3);
            result.W = (num9 * num6 * num3 + num8 * num5 * num2);
        }

        /// <summary>
        /// Works for normalized vectors only
        /// </summary>
        public static QuaternionD CreateFromForwardUp(Vector3D forward, Vector3D up)
        {
            Vector3D vector = -forward;
            Vector3D vector2 = Vector3D.Cross(up, vector);
            Vector3D vector3 = Vector3D.Cross(vector, vector2);
            var m00 = vector2.X;
            var m01 = vector2.Y;
            var m02 = vector2.Z;
            var m10 = vector3.X;
            var m11 = vector3.Y;
            var m12 = vector3.Z;
            var m20 = vector.X;
            var m21 = vector.Y;
            var m22 = vector.Z;

            double num8 = (m00 + m11) + m22;
            var quaternion = new QuaternionD();
            if (num8 > 0)
            {
                var num = Math.Sqrt(num8 + 1.0);
                quaternion.W = num * 0.5;
                num = 0.5 / num;
                quaternion.X = (m12 - m21) * num;
                quaternion.Y = (m20 - m02) * num;
                quaternion.Z = (m01 - m10) * num;
                return quaternion;
            }
            if ((m00 >= m11) && (m00 >= m22))
            {
                var num7 = Math.Sqrt(((1.0 + m00) - m11) - m22);
                var num4 = 0.5 / num7;
                quaternion.X = 0.5 * num7;
                quaternion.Y = (m01 + m10) * num4;
                quaternion.Z = (m02 + m20) * num4;
                quaternion.W = (m12 - m21) * num4;
                return quaternion;
            }
            if (m11 > m22)
            {
                var num6 = Math.Sqrt(((1.0 + m11) - m00) - m22);
                var num3 = 0.5 / num6;
                quaternion.X = (m10 + m01) * num3;
                quaternion.Y = 0.5 * num6;
                quaternion.Z = (m21 + m12) * num3;
                quaternion.W = (m20 - m02) * num3;
                return quaternion;
            }
            var num5 = Math.Sqrt(((1.0 + m22) - m00) - m11);
            var num2 = 0.5 / num5;
            quaternion.X = (m20 + m02) * num2;
            quaternion.Y = (m21 + m12) * num2;
            quaternion.Z = 0.5 * num5;
            quaternion.W = (m01 - m10) * num2;
            return quaternion;
        }

        /// <summary>
        /// Creates a QuaternionD from a rotation MatrixD.
        /// </summary>
        /// <param name="matrix">The rotation MatrixD to create the QuaternionD from.</param>
        public static QuaternionD CreateFromRotationMatrix(MatrixD matrix)
        {
            double num1 = matrix.M11 + matrix.M22 + matrix.M33;
            QuaternionD quaternion = new QuaternionD();
            if (num1 > 0.0)
            {
                double num2 = Math.Sqrt(num1 + 1.0);
                quaternion.W = num2 * 0.5;
                double num3 = 0.5 / num2;
                quaternion.X = (matrix.M23 - matrix.M32) * num3;
                quaternion.Y = (matrix.M31 - matrix.M13) * num3;
                quaternion.Z = (matrix.M12 - matrix.M21) * num3;
            }
            else if (matrix.M11 >= matrix.M22 && matrix.M11 >= matrix.M33)
            {
                double num2 = Math.Sqrt(1.0 + matrix.M11 - matrix.M22 - matrix.M33);
                double num3 = 0.5 / num2;
                quaternion.X = 0.5 * num2;
                quaternion.Y = (matrix.M12 + matrix.M21) * num3;
                quaternion.Z = (matrix.M13 + matrix.M31) * num3;
                quaternion.W = (matrix.M23 - matrix.M32) * num3;
            }
            else if (matrix.M22 > matrix.M33)
            {
                double num2 = Math.Sqrt(1.0 + matrix.M22 - matrix.M11 - matrix.M33);
                double num3 = 0.5 / num2;
                quaternion.X = (matrix.M21 + matrix.M12) * num3;
                quaternion.Y = 0.5 * num2;
                quaternion.Z = (matrix.M32 + matrix.M23) * num3;
                quaternion.W = (matrix.M31 - matrix.M13) * num3;
            }
            else
            {
                double num2 = Math.Sqrt(1.0 + matrix.M33 - matrix.M11 - matrix.M22);
                double num3 = 0.5 / num2;
                quaternion.X = (matrix.M31 + matrix.M13) * num3;
                quaternion.Y = (matrix.M32 + matrix.M23) * num3;
                quaternion.Z = 0.5 * num2;
                quaternion.W = (matrix.M12 - matrix.M21) * num3;
            }
            return quaternion;
        }

        /// <summary>
        /// Creates a QuaternionD from a rotation MatrixD.
        /// </summary>
        /// <param name="matrix">The rotation MatrixD to create the QuaternionD from.</param><param name="result">[OutAttribute] The created QuaternionD.</param>
        public static void CreateFromRotationMatrix(ref MatrixD matrix, out QuaternionD result)
        {
            double num1 = matrix.M11 + matrix.M22 + matrix.M33;
            if (num1 > 0.0)
            {
                double num2 = Math.Sqrt(num1 + 1.0);
                result.W = num2 * 0.5;
                double num3 = 0.5 / num2;
                result.X = (matrix.M23 - matrix.M32) * num3;
                result.Y = (matrix.M31 - matrix.M13) * num3;
                result.Z = (matrix.M12 - matrix.M21) * num3;
            }
            else if (matrix.M11 >= matrix.M22 && matrix.M11 >= matrix.M33)
            {
                double num2 = Math.Sqrt(1.0 + matrix.M11 - matrix.M22 - matrix.M33);
                double num3 = 0.5 / num2;
                result.X = 0.5 * num2;
                result.Y = (matrix.M12 + matrix.M21) * num3;
                result.Z = (matrix.M13 + matrix.M31) * num3;
                result.W = (matrix.M23 - matrix.M32) * num3;
            }
            else if (matrix.M22 > matrix.M33)
            {
                double num2 = Math.Sqrt(1.0 + matrix.M22 - matrix.M11 - matrix.M33);
                double num3 = 0.5 / num2;
                result.X = (matrix.M21 + matrix.M12) * num3;
                result.Y = 0.5 * num2;
                result.Z = (matrix.M32 + matrix.M23) * num3;
                result.W = (matrix.M31 - matrix.M13) * num3;
            }
            else
            {
                double num2 = Math.Sqrt(1.0 + matrix.M33 - matrix.M11 - matrix.M22);
                double num3 = 0.5 / num2;
                result.X = (matrix.M31 + matrix.M13) * num3;
                result.Y = (matrix.M32 + matrix.M23) * num3;
                result.Z = 0.5 * num2;
                result.W = (matrix.M12 - matrix.M21) * num3;
            }
        }

        /// <summary>
        /// Calculates the dot product of two Quaternions.
        /// </summary>
        /// <param name="quaternion1">Source QuaternionD.</param><param name="quaternion2">Source QuaternionD.</param>
        public static double Dot(QuaternionD quaternion1, QuaternionD quaternion2)
        {
            return (quaternion1.X * quaternion2.X + quaternion1.Y * quaternion2.Y + quaternion1.Z * quaternion2.Z + quaternion1.W * quaternion2.W);
        }

        /// <summary>
        /// Calculates the dot product of two Quaternions.
        /// </summary>
        /// <param name="quaternion1">Source QuaternionD.</param><param name="quaternion2">Source QuaternionD.</param><param name="result">[OutAttribute] Dot product of the Quaternions.</param>
        public static void Dot(ref QuaternionD quaternion1, ref QuaternionD quaternion2, out double result)
        {
            result = (quaternion1.X * quaternion2.X + quaternion1.Y * quaternion2.Y + quaternion1.Z * quaternion2.Z + quaternion1.W * quaternion2.W);
        }

        /// <summary>
        /// Interpolates between two quaternions, using spherical linear interpolation.
        /// </summary>
        /// <param name="quaternion1">Source quaternion.</param><param name="quaternion2">Source quaternion.</param><param name="amount">Value that indicates how far to interpolate between the quaternions.</param>
        public static QuaternionD Slerp(QuaternionD quaternion1, QuaternionD quaternion2, double amount)
        {
            double num1 = amount;
            double num2 = (quaternion1.X * quaternion2.X + quaternion1.Y * quaternion2.Y + quaternion1.Z * quaternion2.Z + quaternion1.W * quaternion2.W);
            bool flag = false;
            if (num2 < 0.0)
            {
                flag = true;
                num2 = -num2;
            }
            double num3;
            double num4;
            if (num2 > 0.999998986721039)
            {
                num3 = 1.0 - num1;
                num4 = flag ? -num1 : num1;
            }
            else
            {
                double num5 = Math.Acos(num2);
                double num6 = (1.0 / Math.Sin(num5));
                num3 = Math.Sin((1.0 - num1) * num5) * num6;
                num4 = flag ? -Math.Sin(num1 * num5) * num6 : Math.Sin(num1 * num5) * num6;
            }
            QuaternionD quaternion;
            quaternion.X = (num3 * quaternion1.X + num4 * quaternion2.X);
            quaternion.Y = (num3 * quaternion1.Y + num4 * quaternion2.Y);
            quaternion.Z = (num3 * quaternion1.Z + num4 * quaternion2.Z);
            quaternion.W = (num3 * quaternion1.W + num4 * quaternion2.W);
            return quaternion;
        }

        /// <summary>
        /// Interpolates between two quaternions, using spherical linear interpolation.
        /// </summary>
        /// <param name="quaternion1">Source quaternion.</param><param name="quaternion2">Source quaternion.</param><param name="amount">Value that indicates how far to interpolate between the quaternions.</param><param name="result">[OutAttribute] Result of the interpolation.</param>
        public static void Slerp(ref QuaternionD quaternion1, ref QuaternionD quaternion2, double amount, out QuaternionD result)
        {
            double num1 = amount;
            double num2 = (quaternion1.X * quaternion2.X + quaternion1.Y * quaternion2.Y + quaternion1.Z * quaternion2.Z + quaternion1.W * quaternion2.W);
            bool flag = false;
            if (num2 < 0.0)
            {
                flag = true;
                num2 = -num2;
            }
            double num3;
            double num4;
            if (num2 > 0.999998986721039)
            {
                num3 = 1.0 - num1;
                num4 = flag ? -num1 : num1;
            }
            else
            {
                double num5 = Math.Acos(num2);
                double num6 = (1.0 / Math.Sin(num5));
                num3 = Math.Sin((1.0 - num1) * num5) * num6;
                num4 = flag ? -Math.Sin(num1 * num5) * num6 : Math.Sin(num1 * num5) * num6;
            }
            result.X = (num3 * quaternion1.X + num4 * quaternion2.X);
            result.Y = (num3 * quaternion1.Y + num4 * quaternion2.Y);
            result.Z = (num3 * quaternion1.Z + num4 * quaternion2.Z);
            result.W = (num3 * quaternion1.W + num4 * quaternion2.W);
        }

        /// <summary>
        /// Linearly interpolates between two quaternions.
        /// </summary>
        /// <param name="quaternion1">Source quaternion.</param><param name="quaternion2">Source quaternion.</param><param name="amount">Value indicating how far to interpolate between the quaternions.</param>
        public static QuaternionD Lerp(QuaternionD quaternion1, QuaternionD quaternion2, double amount)
        {
            double num1 = amount;
            double num2 = 1.0 - num1;
            QuaternionD quaternion = new QuaternionD();
            if (quaternion1.X * quaternion2.X + quaternion1.Y * quaternion2.Y + quaternion1.Z * quaternion2.Z + quaternion1.W * quaternion2.W >= 0.0)
            {
                quaternion.X = (num2 * quaternion1.X + num1 * quaternion2.X);
                quaternion.Y = (num2 * quaternion1.Y + num1 * quaternion2.Y);
                quaternion.Z = (num2 * quaternion1.Z + num1 * quaternion2.Z);
                quaternion.W = (num2 * quaternion1.W + num1 * quaternion2.W);
            }
            else
            {
                quaternion.X = (num2 * quaternion1.X - num1 * quaternion2.X);
                quaternion.Y = (num2 * quaternion1.Y - num1 * quaternion2.Y);
                quaternion.Z = (num2 * quaternion1.Z - num1 * quaternion2.Z);
                quaternion.W = (num2 * quaternion1.W - num1 * quaternion2.W);
            }
            double num3 = 1.0 / Math.Sqrt(quaternion.X * quaternion.X + quaternion.Y * quaternion.Y + quaternion.Z * quaternion.Z + quaternion.W * quaternion.W);
            quaternion.X *= num3;
            quaternion.Y *= num3;
            quaternion.Z *= num3;
            quaternion.W *= num3;
            return quaternion;
        }

        /// <summary>
        /// Linearly interpolates between two quaternions.
        /// </summary>
        /// <param name="quaternion1">Source quaternion.</param><param name="quaternion2">Source quaternion.</param><param name="amount">Value indicating how far to interpolate between the quaternions.</param><param name="result">[OutAttribute] The resulting quaternion.</param>
        public static void Lerp(ref QuaternionD quaternion1, ref QuaternionD quaternion2, double amount, out QuaternionD result)
        {
            double num1 = amount;
            double num2 = 1.0 - num1;
            if (quaternion1.X * quaternion2.X + quaternion1.Y * quaternion2.Y + quaternion1.Z * quaternion2.Z + quaternion1.W * quaternion2.W >= 0.0)
            {
                result.X = (num2 * quaternion1.X + num1 * quaternion2.X);
                result.Y = (num2 * quaternion1.Y + num1 * quaternion2.Y);
                result.Z = (num2 * quaternion1.Z + num1 * quaternion2.Z);
                result.W = (num2 * quaternion1.W + num1 * quaternion2.W);
            }
            else
            {
                result.X = (num2 * quaternion1.X - num1 * quaternion2.X);
                result.Y = (num2 * quaternion1.Y - num1 * quaternion2.Y);
                result.Z = (num2 * quaternion1.Z - num1 * quaternion2.Z);
                result.W = (num2 * quaternion1.W - num1 * quaternion2.W);
            }
            double num3 = 1.0 / Math.Sqrt(result.X * result.X + result.Y * result.Y + result.Z * result.Z + result.W * result.W);
            result.X *= num3;
            result.Y *= num3;
            result.Z *= num3;
            result.W *= num3;
        }

        /// <summary>
        /// Concatenates two Quaternions; the result represents the value1 rotation followed by the value2 rotation.
        /// </summary>
        /// <param name="value1">The first QuaternionD rotation in the series.</param><param name="value2">The second QuaternionD rotation in the series.</param>
        public static QuaternionD Concatenate(QuaternionD value1, QuaternionD value2)
        {
            double num1 = value2.X;
            double num2 = value2.Y;
            double num3 = value2.Z;
            double num4 = value2.W;
            double num5 = value1.X;
            double num6 = value1.Y;
            double num7 = value1.Z;
            double num8 = value1.W;
            double num9 = (num2 * num7 - num3 * num6);
            double num10 = (num3 * num5 - num1 * num7);
            double num11 = (num1 * num6 - num2 * num5);
            double num12 = (num1 * num5 + num2 * num6 + num3 * num7);
            QuaternionD quaternion;
            quaternion.X = (num1 * num8 + num5 * num4) + num9;
            quaternion.Y = (num2 * num8 + num6 * num4) + num10;
            quaternion.Z = (num3 * num8 + num7 * num4) + num11;
            quaternion.W = num4 * num8 - num12;
            return quaternion;
        }

        /// <summary>
        /// Concatenates two Quaternions; the result represents the value1 rotation followed by the value2 rotation.
        /// </summary>
        /// <param name="value1">The first QuaternionD rotation in the series.</param><param name="value2">The second QuaternionD rotation in the series.</param><param name="result">[OutAttribute] The QuaternionD rotation representing the concatenation of value1 followed by value2.</param>
        public static void Concatenate(ref QuaternionD value1, ref QuaternionD value2, out QuaternionD result)
        {
            double num1 = value2.X;
            double num2 = value2.Y;
            double num3 = value2.Z;
            double num4 = value2.W;
            double num5 = value1.X;
            double num6 = value1.Y;
            double num7 = value1.Z;
            double num8 = value1.W;
            double num9 = (num2 * num7 - num3 * num6);
            double num10 = (num3 * num5 - num1 * num7);
            double num11 = (num1 * num6 - num2 * num5);
            double num12 = (num1 * num5 + num2 * num6 + num3 * num7);
            result.X = (num1 * num8 + num5 * num4) + num9;
            result.Y = (num2 * num8 + num6 * num4) + num10;
            result.Z = (num3 * num8 + num7 * num4) + num11;
            result.W = num4 * num8 - num12;
        }

        /// <summary>
        /// Flips the sign of each component of the quaternion.
        /// </summary>
        /// <param name="quaternion">Source quaternion.</param>
        public static QuaternionD Negate(QuaternionD quaternion)
        {
            QuaternionD quaternion1;
            quaternion1.X = -quaternion.X;
            quaternion1.Y = -quaternion.Y;
            quaternion1.Z = -quaternion.Z;
            quaternion1.W = -quaternion.W;
            return quaternion1;
        }

        /// <summary>
        /// Flips the sign of each component of the quaternion.
        /// </summary>
        /// <param name="quaternion">Source quaternion.</param><param name="result">[OutAttribute] Negated quaternion.</param>
        public static void Negate(ref QuaternionD quaternion, out QuaternionD result)
        {
            result.X = -quaternion.X;
            result.Y = -quaternion.Y;
            result.Z = -quaternion.Z;
            result.W = -quaternion.W;
        }

        /// <summary>
        /// Adds two Quaternions.
        /// </summary>
        /// <param name="quaternion1">QuaternionD to add.</param><param name="quaternion2">QuaternionD to add.</param>
        public static QuaternionD Add(QuaternionD quaternion1, QuaternionD quaternion2)
        {
            QuaternionD quaternion;
            quaternion.X = quaternion1.X + quaternion2.X;
            quaternion.Y = quaternion1.Y + quaternion2.Y;
            quaternion.Z = quaternion1.Z + quaternion2.Z;
            quaternion.W = quaternion1.W + quaternion2.W;
            return quaternion;
        }

        /// <summary>
        /// Adds two Quaternions.
        /// </summary>
        /// <param name="quaternion1">QuaternionD to add.</param><param name="quaternion2">QuaternionD to add.</param><param name="result">[OutAttribute] Result of adding the Quaternions.</param>
        public static void Add(ref QuaternionD quaternion1, ref QuaternionD quaternion2, out QuaternionD result)
        {
            result.X = quaternion1.X + quaternion2.X;
            result.Y = quaternion1.Y + quaternion2.Y;
            result.Z = quaternion1.Z + quaternion2.Z;
            result.W = quaternion1.W + quaternion2.W;
        }

        /// <summary>
        /// Subtracts a quaternion from another quaternion.
        /// </summary>
        /// <param name="quaternion1">Source quaternion.</param><param name="quaternion2">Source quaternion.</param>
        public static QuaternionD Subtract(QuaternionD quaternion1, QuaternionD quaternion2)
        {
            QuaternionD quaternion;
            quaternion.X = quaternion1.X - quaternion2.X;
            quaternion.Y = quaternion1.Y - quaternion2.Y;
            quaternion.Z = quaternion1.Z - quaternion2.Z;
            quaternion.W = quaternion1.W - quaternion2.W;
            return quaternion;
        }

        /// <summary>
        /// Subtracts a quaternion from another quaternion.
        /// </summary>
        /// <param name="quaternion1">Source quaternion.</param><param name="quaternion2">Source quaternion.</param><param name="result">[OutAttribute] Result of the subtraction.</param>
        public static void Subtract(ref QuaternionD quaternion1, ref QuaternionD quaternion2, out QuaternionD result)
        {
            result.X = quaternion1.X - quaternion2.X;
            result.Y = quaternion1.Y - quaternion2.Y;
            result.Z = quaternion1.Z - quaternion2.Z;
            result.W = quaternion1.W - quaternion2.W;
        }

        /// <summary>
        /// Multiplies two quaternions.
        /// </summary>
        /// <param name="quaternion1">The quaternion on the left of the multiplication.</param><param name="quaternion2">The quaternion on the right of the multiplication.</param>
        public static QuaternionD Multiply(QuaternionD quaternion1, QuaternionD quaternion2)
        {
            double num1 = quaternion1.X;
            double num2 = quaternion1.Y;
            double num3 = quaternion1.Z;
            double num4 = quaternion1.W;
            double num5 = quaternion2.X;
            double num6 = quaternion2.Y;
            double num7 = quaternion2.Z;
            double num8 = quaternion2.W;
            double num9 = (num2 * num7 - num3 * num6);
            double num10 = (num3 * num5 - num1 * num7);
            double num11 = (num1 * num6 - num2 * num5);
            double num12 = (num1 * num5 + num2 * num6 + num3 * num7);
            QuaternionD quaternion;
            quaternion.X = (num1 * num8 + num5 * num4) + num9;
            quaternion.Y = (num2 * num8 + num6 * num4) + num10;
            quaternion.Z = (num3 * num8 + num7 * num4) + num11;
            quaternion.W = num4 * num8 - num12;
            return quaternion;
        }

        /// <summary>
        /// Multiplies two quaternions.
        /// </summary>
        /// <param name="quaternion1">The quaternion on the left of the multiplication.</param><param name="quaternion2">The quaternion on the right of the multiplication.</param><param name="result">[OutAttribute] The result of the multiplication.</param>
        public static void Multiply(ref QuaternionD quaternion1, ref QuaternionD quaternion2, out QuaternionD result)
        {
            double num1 = quaternion1.X;
            double num2 = quaternion1.Y;
            double num3 = quaternion1.Z;
            double num4 = quaternion1.W;
            double num5 = quaternion2.X;
            double num6 = quaternion2.Y;
            double num7 = quaternion2.Z;
            double num8 = quaternion2.W;
            double num9 = (num2 * num7 - num3 * num6);
            double num10 = (num3 * num5 - num1 * num7);
            double num11 = (num1 * num6 - num2 * num5);
            double num12 = (num1 * num5 + num2 * num6 + num3 * num7);
            result.X = (num1 * num8 + num5 * num4) + num9;
            result.Y = (num2 * num8 + num6 * num4) + num10;
            result.Z = (num3 * num8 + num7 * num4) + num11;
            result.W = num4 * num8 - num12;
        }

        /// <summary>
        /// Multiplies a quaternion by a scalar value.
        /// </summary>
        /// <param name="quaternion1">Source quaternion.</param><param name="scaleFactor">Scalar value.</param>
        public static QuaternionD Multiply(QuaternionD quaternion1, double scaleFactor)
        {
            QuaternionD quaternion;
            quaternion.X = quaternion1.X * scaleFactor;
            quaternion.Y = quaternion1.Y * scaleFactor;
            quaternion.Z = quaternion1.Z * scaleFactor;
            quaternion.W = quaternion1.W * scaleFactor;
            return quaternion;
        }

        /// <summary>
        /// Multiplies a quaternion by a scalar value.
        /// </summary>
        /// <param name="quaternion1">Source quaternion.</param><param name="scaleFactor">Scalar value.</param><param name="result">[OutAttribute] The result of the multiplication.</param>
        public static void Multiply(ref QuaternionD quaternion1, double scaleFactor, out QuaternionD result)
        {
            result.X = quaternion1.X * scaleFactor;
            result.Y = quaternion1.Y * scaleFactor;
            result.Z = quaternion1.Z * scaleFactor;
            result.W = quaternion1.W * scaleFactor;
        }

        /// <summary>
        /// Divides a QuaternionD by another QuaternionD.
        /// </summary>
        /// <param name="quaternion1">Source QuaternionD.</param><param name="quaternion2">The divisor.</param>
        public static QuaternionD Divide(QuaternionD quaternion1, QuaternionD quaternion2)
        {
            double num1 = quaternion1.X;
            double num2 = quaternion1.Y;
            double num3 = quaternion1.Z;
            double num4 = quaternion1.W;
            double num5 = 1.0 / (quaternion2.X * quaternion2.X + quaternion2.Y * quaternion2.Y + quaternion2.Z * quaternion2.Z + quaternion2.W * quaternion2.W);
            double num6 = -quaternion2.X * num5;
            double num7 = -quaternion2.Y * num5;
            double num8 = -quaternion2.Z * num5;
            double num9 = quaternion2.W * num5;
            double num10 = (num2 * num8 - num3 * num7);
            double num11 = (num3 * num6 - num1 * num8);
            double num12 = (num1 * num7 - num2 * num6);
            double num13 = (num1 * num6 + num2 * num7 + num3 * num8);
            QuaternionD quaternion;
            quaternion.X = (num1 * num9 + num6 * num4) + num10;
            quaternion.Y = (num2 * num9 + num7 * num4) + num11;
            quaternion.Z = (num3 * num9 + num8 * num4) + num12;
            quaternion.W = num4 * num9 - num13;
            return quaternion;
        }

        /// <summary>
        /// Divides a QuaternionD by another QuaternionD.
        /// </summary>
        /// <param name="quaternion1">Source QuaternionD.</param><param name="quaternion2">The divisor.</param><param name="result">[OutAttribute] Result of the division.</param>
        public static void Divide(ref QuaternionD quaternion1, ref QuaternionD quaternion2, out QuaternionD result)
        {
            double num1 = quaternion1.X;
            double num2 = quaternion1.Y;
            double num3 = quaternion1.Z;
            double num4 = quaternion1.W;
            double num5 = 1.0 / (quaternion2.X * quaternion2.X + quaternion2.Y * quaternion2.Y + quaternion2.Z * quaternion2.Z + quaternion2.W * quaternion2.W);
            double num6 = -quaternion2.X * num5;
            double num7 = -quaternion2.Y * num5;
            double num8 = -quaternion2.Z * num5;
            double num9 = quaternion2.W * num5;
            double num10 = (num2 * num8 - num3 * num7);
            double num11 = (num3 * num6 - num1 * num8);
            double num12 = (num1 * num7 - num2 * num6);
            double num13 = (num1 * num6 + num2 * num7 + num3 * num8);
            result.X = (num1 * num9 + num6 * num4) + num10;
            result.Y = (num2 * num9 + num7 * num4) + num11;
            result.Z = (num3 * num9 + num8 * num4) + num12;
            result.W = num4 * num9 - num13;
        }

        public static QuaternionD FromVector4(Vector4D v)
        {
            return new QuaternionD(v.X, v.Y, v.Z, v.W);
        }

        public Vector4D ToVector4()
        {
            return new Vector4D(X, Y, Z, W);
        }

        public static bool IsZero(QuaternionD value)
        {
            return IsZero(value, 0.0001);
        }

        // Per component IsZero, returns 1 for each component which equals to 0
        public static bool IsZero(QuaternionD value, double epsilon)
        {
            return Math.Abs(value.X) < epsilon && Math.Abs(value.Y) < epsilon && Math.Abs(value.Z) < epsilon && Math.Abs(value.W) < epsilon;
        }

        public static void CreateFromTwoVectors(ref Vector3D firstVector, ref Vector3D secondVector, out QuaternionD result)
        {
            Vector3D thirdVector;
            Vector3D.Cross(ref firstVector, ref secondVector, out thirdVector);
            result = new QuaternionD(thirdVector.X, thirdVector.Y, thirdVector.Z, Vector3.Dot(firstVector, secondVector));
            result.W += result.Length();
            result.Normalize();
        }

        public static QuaternionD CreateFromTwoVectors(Vector3D firstVector, Vector3D secondVector)
        {
            QuaternionD rtn;
            CreateFromTwoVectors(ref firstVector, ref secondVector, out rtn);
            return rtn;
        }
    }
}
