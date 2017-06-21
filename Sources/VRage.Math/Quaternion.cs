using System;
using System.Diagnostics;
using System.Globalization;

namespace VRageMath
{
    /// <summary>
    /// Defines a four-dimensional vector (x,y,z,w), which is used to efficiently rotate an object about the (x, y, z) vector by the angle theta, where w = cos(theta/2).
    /// </summary>
    [Serializable, ProtoBuf.ProtoContract]
    public struct Quaternion : IEquatable<Quaternion>
    {
        public static Quaternion Identity = new Quaternion(0.0f, 0.0f, 0.0f, 1f);
        public static Quaternion Zero = new Quaternion(0.0f, 0.0f, 0.0f, 0.0f);
        /// <summary>
        /// Specifies the x-value of the vector component of the quaternion.
        /// </summary>
        [ProtoBuf.ProtoMember]
        public float X;
        /// <summary>
        /// Specifies the y-value of the vector component of the quaternion.
        /// </summary>
        [ProtoBuf.ProtoMember]
        public float Y;
        /// <summary>
        /// Specifies the z-value of the vector component of the quaternion.
        /// </summary>
        [ProtoBuf.ProtoMember]
        public float Z;
        /// <summary>
        /// Specifies the rotation component of the quaternion.
        /// </summary>
        [ProtoBuf.ProtoMember]
        public float W;

        public Vector3 Forward
        {
            get
            {
                Vector3 r;
                GetForward(ref this, out r);
                return r;
            }
        }

        public Vector3 Right
        {
            get
            {
                Vector3 r;
                GetRight(ref this, out r);
                return r;
            }
        }

        public Vector3 Up
        {
            get
            {
                Vector3 r;
                GetUp(ref this, out r);
                return r;
            }
        }

        static Quaternion()
        {
        }

        /// <summary>
        /// Initializes a new instance of Quaternion.
        /// </summary>
        /// <param name="x">The x-value of the quaternion.</param><param name="y">The y-value of the quaternion.</param><param name="z">The z-value of the quaternion.</param><param name="w">The w-value of the quaternion.</param>
        public Quaternion(float x, float y, float z, float w)
        {
            this.X = x;
            this.Y = y;
            this.Z = z;
            this.W = w;
        }

        /// <summary>
        /// Initializes a new instance of Quaternion.
        /// </summary>
        /// <param name="vectorPart">The vector component of the quaternion.</param><param name="scalarPart">The rotation component of the quaternion.</param>
        public Quaternion(Vector3 vectorPart, float scalarPart)
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
        public static Quaternion operator -(Quaternion quaternion)
        {
            Quaternion quaternion1;
            quaternion1.X = -quaternion.X;
            quaternion1.Y = -quaternion.Y;
            quaternion1.Z = -quaternion.Z;
            quaternion1.W = -quaternion.W;
            return quaternion1;
        }

        /// <summary>
        /// Compares two Quaternions for equality.
        /// </summary>
        /// <param name="quaternion1">Source Quaternion.</param><param name="quaternion2">Source Quaternion.</param>
        public static bool operator ==(Quaternion quaternion1, Quaternion quaternion2)
        {
            if ((double)quaternion1.X == (double)quaternion2.X && (double)quaternion1.Y == (double)quaternion2.Y && (double)quaternion1.Z == (double)quaternion2.Z)
                return (double)quaternion1.W == (double)quaternion2.W;
            else
                return false;
        }

        /// <summary>
        /// Compare two Quaternions for inequality.
        /// </summary>
        /// <param name="quaternion1">Source Quaternion.</param><param name="quaternion2">Source Quaternion.</param>
        public static bool operator !=(Quaternion quaternion1, Quaternion quaternion2)
        {
            if ((double)quaternion1.X == (double)quaternion2.X && (double)quaternion1.Y == (double)quaternion2.Y && (double)quaternion1.Z == (double)quaternion2.Z)
                return (double)quaternion1.W != (double)quaternion2.W;
            else
                return true;
        }

        /// <summary>
        /// Adds two Quaternions.
        /// </summary>
        /// <param name="quaternion1">Quaternion to add.</param><param name="quaternion2">Quaternion to add.</param>
        public static Quaternion operator +(Quaternion quaternion1, Quaternion quaternion2)
        {
            Quaternion quaternion;
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
        public static Quaternion operator -(Quaternion quaternion1, Quaternion quaternion2)
        {
            Quaternion quaternion;
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
        public static Quaternion operator *(Quaternion quaternion1, Quaternion quaternion2)
        {
            float num1 = quaternion1.X;
            float num2 = quaternion1.Y;
            float num3 = quaternion1.Z;
            float num4 = quaternion1.W;
            float num5 = quaternion2.X;
            float num6 = quaternion2.Y;
            float num7 = quaternion2.Z;
            float num8 = quaternion2.W;
            float num9 = (float)((double)num2 * (double)num7 - (double)num3 * (double)num6);
            float num10 = (float)((double)num3 * (double)num5 - (double)num1 * (double)num7);
            float num11 = (float)((double)num1 * (double)num6 - (double)num2 * (double)num5);
            float num12 = (float)((double)num1 * (double)num5 + (double)num2 * (double)num6 + (double)num3 * (double)num7);
            Quaternion quaternion;
            quaternion.X = (float)((double)num1 * (double)num8 + (double)num5 * (double)num4) + num9;
            quaternion.Y = (float)((double)num2 * (double)num8 + (double)num6 * (double)num4) + num10;
            quaternion.Z = (float)((double)num3 * (double)num8 + (double)num7 * (double)num4) + num11;
            quaternion.W = num4 * num8 - num12;
            return quaternion;
        }

        /// <summary>
        /// Multiplies a quaternion by a scalar value.
        /// </summary>
        /// <param name="quaternion1">Source quaternion.</param><param name="scaleFactor">Scalar value.</param>
        public static Quaternion operator *(Quaternion quaternion1, float scaleFactor)
        {
            Quaternion quaternion;
            quaternion.X = quaternion1.X * scaleFactor;
            quaternion.Y = quaternion1.Y * scaleFactor;
            quaternion.Z = quaternion1.Z * scaleFactor;
            quaternion.W = quaternion1.W * scaleFactor;
            return quaternion;
        }

        /// <summary>
        /// Divides a Quaternion by another Quaternion.
        /// </summary>
        /// <param name="quaternion1">Source Quaternion.</param><param name="quaternion2">The divisor.</param>
        public static Quaternion operator /(Quaternion quaternion1, Quaternion quaternion2)
        {
            float num1 = quaternion1.X;
            float num2 = quaternion1.Y;
            float num3 = quaternion1.Z;
            float num4 = quaternion1.W;
            float num5 = 1f / (float)((double)quaternion2.X * (double)quaternion2.X + (double)quaternion2.Y * (double)quaternion2.Y + (double)quaternion2.Z * (double)quaternion2.Z + (double)quaternion2.W * (double)quaternion2.W);
            float num6 = -quaternion2.X * num5;
            float num7 = -quaternion2.Y * num5;
            float num8 = -quaternion2.Z * num5;
            float num9 = quaternion2.W * num5;
            float num10 = (float)((double)num2 * (double)num8 - (double)num3 * (double)num7);
            float num11 = (float)((double)num3 * (double)num6 - (double)num1 * (double)num8);
            float num12 = (float)((double)num1 * (double)num7 - (double)num2 * (double)num6);
            float num13 = (float)((double)num1 * (double)num6 + (double)num2 * (double)num7 + (double)num3 * (double)num8);
            Quaternion quaternion;
            quaternion.X = (float)((double)num1 * (double)num9 + (double)num6 * (double)num4) + num10;
            quaternion.Y = (float)((double)num2 * (double)num9 + (double)num7 * (double)num4) + num11;
            quaternion.Z = (float)((double)num3 * (double)num9 + (double)num8 * (double)num4) + num12;
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
        public string ToStringAxisAngle(string format = "G")
        {
            Vector3 axis;
            float angle;
            GetAxisAngle(out axis, out angle);
            CultureInfo currentCulture = CultureInfo.CurrentCulture;
            return string.Format((IFormatProvider) currentCulture, "{{{0}/{1}}}", axis.ToString(format), angle.ToString(format));
        }

        /// <summary>
        /// Determines whether the specified Object is equal to the Quaternion.
        /// </summary>
        /// <param name="other">The Quaternion to compare with the current Quaternion.</param>
        public bool Equals(Quaternion other)
        {
            if ((double)this.X == (double)other.X && (double)this.Y == (double)other.Y && (double)this.Z == (double)other.Z)
                return (double)this.W == (double)other.W;
            else
                return false;
        }
        public bool Equals(Quaternion value, float epsilon)
        {
            return Math.Abs(X - value.X) < epsilon && Math.Abs(Y - value.Y) < epsilon && Math.Abs(Z - value.Z) < epsilon && 
                Math.Abs(W - value.W) < epsilon;
        }

        /// <summary>
        /// Returns a value that indicates whether the current instance is equal to a specified object.
        /// </summary>
        /// <param name="obj">Object to make the comparison with.</param>
        public override bool Equals(object obj)
        {
            bool flag = false;
            if (obj is Quaternion)
                flag = this.Equals((Quaternion)obj);
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
        /// Calculates the length squared of a Quaternion.
        /// </summary>
        public float LengthSquared()
        {
            return (float)((double)this.X * (double)this.X + (double)this.Y * (double)this.Y + (double)this.Z * (double)this.Z + (double)this.W * (double)this.W);
        }

        /// <summary>
        /// Calculates the length of a Quaternion.
        /// </summary>
        public float Length()
        {
            return (float)Math.Sqrt((double)this.X * (double)this.X + (double)this.Y * (double)this.Y + (double)this.Z * (double)this.Z + (double)this.W * (double)this.W);
        }

        /// <summary>
        /// Divides each component of the quaternion by the length of the quaternion.
        /// </summary>
        public void Normalize()
        {
            float num = 1f / (float)Math.Sqrt((double)this.X * (double)this.X + (double)this.Y * (double)this.Y + (double)this.Z * (double)this.Z + (double)this.W * (double)this.W);
            this.X *= num;
            this.Y *= num;
            this.Z *= num;
            this.W *= num;
        }

        public void GetAxisAngle(out Vector3 axis, out float angle)
        {
            axis.X = X;
            axis.Y = Y;
            axis.Z = Z;
            float sinAngleHalf = axis.Length();
            float cosAngleHalf = W;
            if (sinAngleHalf != 0f)
            {
                axis.X /= sinAngleHalf;
                axis.Y /= sinAngleHalf;
                axis.Z /= sinAngleHalf;
            }
            angle = (float)Math.Atan2(sinAngleHalf, cosAngleHalf) * 2f;
        }

        /// <summary>
        /// Divides each component of the quaternion by the length of the quaternion.
        /// </summary>
        /// <param name="quaternion">Source quaternion.</param>
        public static Quaternion Normalize(Quaternion quaternion)
        {
            float num = 1f / (float)Math.Sqrt((double)quaternion.X * (double)quaternion.X + (double)quaternion.Y * (double)quaternion.Y + (double)quaternion.Z * (double)quaternion.Z + (double)quaternion.W * (double)quaternion.W);
            Quaternion quaternion1;
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
        public static void Normalize(ref Quaternion quaternion, out Quaternion result)
        {
            float num = 1f / (float)Math.Sqrt((double)quaternion.X * (double)quaternion.X + (double)quaternion.Y * (double)quaternion.Y + (double)quaternion.Z * (double)quaternion.Z + (double)quaternion.W * (double)quaternion.W);
            result.X = quaternion.X * num;
            result.Y = quaternion.Y * num;
            result.Z = quaternion.Z * num;
            result.W = quaternion.W * num;
        }

        /// <summary>
        /// Transforms this Quaternion into its conjugate.
        /// </summary>
        public void Conjugate()
        {
            this.X = -this.X;
            this.Y = -this.Y;
            this.Z = -this.Z;
        }

        /// <summary>
        /// Returns the conjugate of a specified Quaternion.
        /// </summary>
        /// <param name="value">The Quaternion of which to return the conjugate.</param>
        public static Quaternion Conjugate(Quaternion value)
        {
            Quaternion quaternion;
            quaternion.X = -value.X;
            quaternion.Y = -value.Y;
            quaternion.Z = -value.Z;
            quaternion.W = value.W;
            return quaternion;
        }

        /// <summary>
        /// Returns the conjugate of a specified Quaternion.
        /// </summary>
        /// <param name="value">The Quaternion of which to return the conjugate.</param><param name="result">[OutAttribute] An existing Quaternion filled in to be the conjugate of the specified one.</param>
        public static void Conjugate(ref Quaternion value, out Quaternion result)
        {
            result.X = -value.X;
            result.Y = -value.Y;
            result.Z = -value.Z;
            result.W = value.W;
        }

        /// <summary>
        /// Returns the inverse of a Quaternion.
        /// </summary>
        /// <param name="quaternion">Source Quaternion.</param>
        public static Quaternion Inverse(Quaternion quaternion)
        {
            float num = 1f / (float)((double)quaternion.X * (double)quaternion.X + (double)quaternion.Y * (double)quaternion.Y + (double)quaternion.Z * (double)quaternion.Z + (double)quaternion.W * (double)quaternion.W);
            Quaternion quaternion1;
            quaternion1.X = -quaternion.X * num;
            quaternion1.Y = -quaternion.Y * num;
            quaternion1.Z = -quaternion.Z * num;
            quaternion1.W = quaternion.W * num;
            return quaternion1;
        }

        /// <summary>
        /// Returns the inverse of a Quaternion.
        /// </summary>
        /// <param name="quaternion">Source Quaternion.</param><param name="result">[OutAttribute] The inverse of the Quaternion.</param>
        public static void Inverse(ref Quaternion quaternion, out Quaternion result)
        {
            float num = 1f / (float)((double)quaternion.X * (double)quaternion.X + (double)quaternion.Y * (double)quaternion.Y + (double)quaternion.Z * (double)quaternion.Z + (double)quaternion.W * (double)quaternion.W);
            result.X = -quaternion.X * num;
            result.Y = -quaternion.Y * num;
            result.Z = -quaternion.Z * num;
            result.W = quaternion.W * num;
        }

        /// <summary>
        /// Creates a Quaternion from a vector and an angle to rotate about the vector.
        /// </summary>
        /// <param name="axis">The vector to rotate around.</param><param name="angle">The angle to rotate around the vector.</param>
        public static Quaternion CreateFromAxisAngle(Vector3 axis, float angle)
        {
            float num1 = angle * 0.5f;
            float num2 = (float)Math.Sin((double)num1);
            float num3 = (float)Math.Cos((double)num1);
            Quaternion quaternion;
            quaternion.X = axis.X * num2;
            quaternion.Y = axis.Y * num2;
            quaternion.Z = axis.Z * num2;
            quaternion.W = num3;
            return quaternion;
        }

        /// <summary>
        /// Creates a Quaternion from a vector and an angle to rotate about the vector.
        /// </summary>
        /// <param name="axis">The vector to rotate around.</param><param name="angle">The angle to rotate around the vector.</param><param name="result">[OutAttribute] The created Quaternion.</param>
        public static void CreateFromAxisAngle(ref Vector3 axis, float angle, out Quaternion result)
        {
            float num1 = angle * 0.5f;
            float num2 = (float)Math.Sin((double)num1);
            float num3 = (float)Math.Cos((double)num1);
            result.X = axis.X * num2;
            result.Y = axis.Y * num2;
            result.Z = axis.Z * num2;
            result.W = num3;
        }

        /// <summary>
        /// Creates a new Quaternion from specified yaw, pitch, and roll angles.
        /// </summary>
        /// <param name="yaw">The yaw angle, in radians, around the y-axis.</param><param name="pitch">The pitch angle, in radians, around the x-axis.</param><param name="roll">The roll angle, in radians, around the z-axis.</param>
        public static Quaternion CreateFromYawPitchRoll(float yaw, float pitch, float roll)
        {
            float num1 = roll * 0.5f;
            float num2 = (float)Math.Sin((double)num1);
            float num3 = (float)Math.Cos((double)num1);
            float num4 = pitch * 0.5f;
            float num5 = (float)Math.Sin((double)num4);
            float num6 = (float)Math.Cos((double)num4);
            float num7 = yaw * 0.5f;
            float num8 = (float)Math.Sin((double)num7);
            float num9 = (float)Math.Cos((double)num7);
            Quaternion quaternion;
            quaternion.X = (float)((double)num9 * (double)num5 * (double)num3 + (double)num8 * (double)num6 * (double)num2);
            quaternion.Y = (float)((double)num8 * (double)num6 * (double)num3 - (double)num9 * (double)num5 * (double)num2);
            quaternion.Z = (float)((double)num9 * (double)num6 * (double)num2 - (double)num8 * (double)num5 * (double)num3);
            quaternion.W = (float)((double)num9 * (double)num6 * (double)num3 + (double)num8 * (double)num5 * (double)num2);
            return quaternion;
        }

        /// <summary>
        /// Creates a new Quaternion from specified yaw, pitch, and roll angles.
        /// </summary>
        /// <param name="yaw">The yaw angle, in radians, around the y-axis.</param><param name="pitch">The pitch angle, in radians, around the x-axis.</param><param name="roll">The roll angle, in radians, around the z-axis.</param><param name="result">[OutAttribute] An existing Quaternion filled in to express the specified yaw, pitch, and roll angles.</param>
        public static void CreateFromYawPitchRoll(float yaw, float pitch, float roll, out Quaternion result)
        {
            float num1 = roll * 0.5f;
            float num2 = (float)Math.Sin((double)num1);
            float num3 = (float)Math.Cos((double)num1);
            float num4 = pitch * 0.5f;
            float num5 = (float)Math.Sin((double)num4);
            float num6 = (float)Math.Cos((double)num4);
            float num7 = yaw * 0.5f;
            float num8 = (float)Math.Sin((double)num7);
            float num9 = (float)Math.Cos((double)num7);
            result.X = (float)((double)num9 * (double)num5 * (double)num3 + (double)num8 * (double)num6 * (double)num2);
            result.Y = (float)((double)num8 * (double)num6 * (double)num3 - (double)num9 * (double)num5 * (double)num2);
            result.Z = (float)((double)num9 * (double)num6 * (double)num2 - (double)num8 * (double)num5 * (double)num3);
            result.W = (float)((double)num9 * (double)num6 * (double)num3 + (double)num8 * (double)num5 * (double)num2);
        }

        /// <summary>
        /// Works for normalized vectors only
        /// </summary>
        public static Quaternion CreateFromForwardUp(Vector3 forward, Vector3 up)
        {
            Vector3 vector = -forward;
            Vector3 vector2 = Vector3.Cross(up, vector);
            Vector3 vector3 = Vector3.Cross(vector, vector2);
            var m00 = vector2.X;
            var m01 = vector2.Y;
            var m02 = vector2.Z;
            var m10 = vector3.X;
            var m11 = vector3.Y;
            var m12 = vector3.Z;
            var m20 = vector.X;
            var m21 = vector.Y;
            var m22 = vector.Z;

            float num8 = (m00 + m11) + m22;
            var quaternion = new Quaternion();
            if (num8 > 0f)
            {
                var num = (float)Math.Sqrt(num8 + 1f);
                quaternion.W = num * 0.5f;
                num = 0.5f / num;
                quaternion.X = (m12 - m21) * num;
                quaternion.Y = (m20 - m02) * num;
                quaternion.Z = (m01 - m10) * num;
                return quaternion;
            }
            if ((m00 >= m11) && (m00 >= m22))
            {
                var num7 = (float)Math.Sqrt(((1f + m00) - m11) - m22);
                var num4 = 0.5f / num7;
                quaternion.X = 0.5f * num7;
                quaternion.Y = (m01 + m10) * num4;
                quaternion.Z = (m02 + m20) * num4;
                quaternion.W = (m12 - m21) * num4;
                return quaternion;
            }
            if (m11 > m22)
            {
                var num6 = (float)Math.Sqrt(((1f + m11) - m00) - m22);
                var num3 = 0.5f / num6;
                quaternion.X = (m10 + m01) * num3;
                quaternion.Y = 0.5f * num6;
                quaternion.Z = (m21 + m12) * num3;
                quaternion.W = (m20 - m02) * num3;
                return quaternion;
            }
            var num5 = (float)Math.Sqrt(((1f + m22) - m00) - m11);
            var num2 = 0.5f / num5;
            quaternion.X = (m20 + m02) * num2;
            quaternion.Y = (m21 + m12) * num2;
            quaternion.Z = 0.5f * num5;
            quaternion.W = (m01 - m10) * num2;
            return quaternion;
        }

        public static Quaternion CreateFromRotationMatrix(MatrixD matrix)
        {
            return CreateFromRotationMatrix((Matrix)matrix);
        }
        /// <summary>
        /// Creates a Quaternion from a rotation Matrix.
        /// </summary>
        /// <param name="matrix">The rotation Matrix to create the Quaternion from.</param>
        public static Quaternion CreateFromRotationMatrix(Matrix matrix)
        {
            float num1 = matrix.M11 + matrix.M22 + matrix.M33;
            Quaternion quaternion = new Quaternion();
            if ((double)num1 > 0.0)
            {
                float num2 = (float)Math.Sqrt((double)num1 + 1.0);
                quaternion.W = num2 * 0.5f;
                float num3 = 0.5f / num2;
                quaternion.X = (matrix.M23 - matrix.M32) * num3;
                quaternion.Y = (matrix.M31 - matrix.M13) * num3;
                quaternion.Z = (matrix.M12 - matrix.M21) * num3;
            }
            else if ((double)matrix.M11 >= (double)matrix.M22 && (double)matrix.M11 >= (double)matrix.M33)
            {
                float num2 = (float)Math.Sqrt(1.0 + (double)matrix.M11 - (double)matrix.M22 - (double)matrix.M33);
                float num3 = 0.5f / num2;
                quaternion.X = 0.5f * num2;
                quaternion.Y = (matrix.M12 + matrix.M21) * num3;
                quaternion.Z = (matrix.M13 + matrix.M31) * num3;
                quaternion.W = (matrix.M23 - matrix.M32) * num3;
            }
            else if ((double)matrix.M22 > (double)matrix.M33)
            {
                float num2 = (float)Math.Sqrt(1.0 + (double)matrix.M22 - (double)matrix.M11 - (double)matrix.M33);
                float num3 = 0.5f / num2;
                quaternion.X = (matrix.M21 + matrix.M12) * num3;
                quaternion.Y = 0.5f * num2;
                quaternion.Z = (matrix.M32 + matrix.M23) * num3;
                quaternion.W = (matrix.M31 - matrix.M13) * num3;
            }
            else
            {
                float num2 = (float)Math.Sqrt(1.0 + (double)matrix.M33 - (double)matrix.M11 - (double)matrix.M22);
                float num3 = 0.5f / num2;
                quaternion.X = (matrix.M31 + matrix.M13) * num3;
                quaternion.Y = (matrix.M32 + matrix.M23) * num3;
                quaternion.Z = 0.5f * num2;
                quaternion.W = (matrix.M12 - matrix.M21) * num3;
            }
            return quaternion;
        }

        public static void CreateFromRotationMatrix(ref MatrixD matrix, out Quaternion result)
        {
            Matrix m = (Matrix)matrix;
            CreateFromRotationMatrix(ref m, out result);
        }

        public static void CreateFromTwoVectors(ref Vector3 firstVector, ref Vector3 secondVector, out Quaternion result)
        {
            Vector3 thirdVector;
            Vector3.Cross(ref firstVector, ref secondVector, out thirdVector);
            result = new Quaternion(thirdVector.X, thirdVector.Y, thirdVector.Z, Vector3.Dot(firstVector, secondVector));
            result.W += result.Length();
            result.Normalize();
        }

        public static Quaternion CreateFromTwoVectors(Vector3 firstVector, Vector3 secondVector)
        {
            Quaternion rtn;
            CreateFromTwoVectors(ref firstVector, ref secondVector, out rtn);
            return rtn;
        }

        /// <summary>
        /// Creates a Quaternion from a rotation Matrix.
        /// </summary>
        /// <param name="matrix">The rotation Matrix to create the Quaternion from.</param><param name="result">[OutAttribute] The created Quaternion.</param>
        public static void CreateFromRotationMatrix(ref Matrix matrix, out Quaternion result)
        {
            float num1 = matrix.M11 + matrix.M22 + matrix.M33;
            if ((double)num1 > 0.0)
            {
                float num2 = (float)Math.Sqrt((double)num1 + 1.0);
                result.W = num2 * 0.5f;
                float num3 = 0.5f / num2;
                result.X = (matrix.M23 - matrix.M32) * num3;
                result.Y = (matrix.M31 - matrix.M13) * num3;
                result.Z = (matrix.M12 - matrix.M21) * num3;
            }
            else if ((double)matrix.M11 >= (double)matrix.M22 && (double)matrix.M11 >= (double)matrix.M33)
            {
                float num2 = (float)Math.Sqrt(1.0 + (double)matrix.M11 - (double)matrix.M22 - (double)matrix.M33);
                float num3 = 0.5f / num2;
                result.X = 0.5f * num2;
                result.Y = (matrix.M12 + matrix.M21) * num3;
                result.Z = (matrix.M13 + matrix.M31) * num3;
                result.W = (matrix.M23 - matrix.M32) * num3;
            }
            else if ((double)matrix.M22 > (double)matrix.M33)
            {
                float num2 = (float)Math.Sqrt(1.0 + (double)matrix.M22 - (double)matrix.M11 - (double)matrix.M33);
                float num3 = 0.5f / num2;
                result.X = (matrix.M21 + matrix.M12) * num3;
                result.Y = 0.5f * num2;
                result.Z = (matrix.M32 + matrix.M23) * num3;
                result.W = (matrix.M31 - matrix.M13) * num3;
            }
            else
            {
                float num2 = (float)Math.Sqrt(1.0 + (double)matrix.M33 - (double)matrix.M11 - (double)matrix.M22);
                float num3 = 0.5f / num2;
                result.X = (matrix.M31 + matrix.M13) * num3;
                result.Y = (matrix.M32 + matrix.M23) * num3;
                result.Z = 0.5f * num2;
                result.W = (matrix.M12 - matrix.M21) * num3;
            }
        }

        /// <summary>
        /// Calculates the dot product of two Quaternions.
        /// </summary>
        /// <param name="quaternion1">Source Quaternion.</param><param name="quaternion2">Source Quaternion.</param>
        public static float Dot(Quaternion quaternion1, Quaternion quaternion2)
        {
            return (float)((double)quaternion1.X * (double)quaternion2.X + (double)quaternion1.Y * (double)quaternion2.Y + (double)quaternion1.Z * (double)quaternion2.Z + (double)quaternion1.W * (double)quaternion2.W);
        }

        /// <summary>
        /// Calculates the dot product of two Quaternions.
        /// </summary>
        /// <param name="quaternion1">Source Quaternion.</param><param name="quaternion2">Source Quaternion.</param><param name="result">[OutAttribute] Dot product of the Quaternions.</param>
        public static void Dot(ref Quaternion quaternion1, ref Quaternion quaternion2, out float result)
        {
            result = (float)((double)quaternion1.X * (double)quaternion2.X + (double)quaternion1.Y * (double)quaternion2.Y + (double)quaternion1.Z * (double)quaternion2.Z + (double)quaternion1.W * (double)quaternion2.W);
        }

        /// <summary>
        /// Interpolates between two quaternions, using spherical linear interpolation.
        /// </summary>
        /// <param name="quaternion1">Source quaternion.</param><param name="quaternion2">Source quaternion.</param><param name="amount">Value that indicates how far to interpolate between the quaternions.</param>
        public static Quaternion Slerp(Quaternion quaternion1, Quaternion quaternion2, float amount)
        {
            float num1 = amount;
            float num2 = (float)((double)quaternion1.X * (double)quaternion2.X + (double)quaternion1.Y * (double)quaternion2.Y + (double)quaternion1.Z * (double)quaternion2.Z + (double)quaternion1.W * (double)quaternion2.W);
            bool flag = false;
            if ((double)num2 < 0.0)
            {
                flag = true;
                num2 = -num2;
            }
            float num3;
            float num4;
            if ((double)num2 > 0.999998986721039)
            {
                num3 = 1f - num1;
                num4 = flag ? -num1 : num1;
            }
            else
            {
                float num5 = (float)Math.Acos((double)num2);
                float num6 = (float)(1.0 / Math.Sin((double)num5));
                num3 = (float)Math.Sin((1.0 - (double)num1) * (double)num5) * num6;
                num4 = flag ? (float)-Math.Sin((double)num1 * (double)num5) * num6 : (float)Math.Sin((double)num1 * (double)num5) * num6;
            }
            Quaternion quaternion;
            quaternion.X = (float)((double)num3 * (double)quaternion1.X + (double)num4 * (double)quaternion2.X);
            quaternion.Y = (float)((double)num3 * (double)quaternion1.Y + (double)num4 * (double)quaternion2.Y);
            quaternion.Z = (float)((double)num3 * (double)quaternion1.Z + (double)num4 * (double)quaternion2.Z);
            quaternion.W = (float)((double)num3 * (double)quaternion1.W + (double)num4 * (double)quaternion2.W);
            return quaternion;
        }

        /// <summary>
        /// Interpolates between two quaternions, using spherical linear interpolation.
        /// </summary>
        /// <param name="quaternion1">Source quaternion.</param><param name="quaternion2">Source quaternion.</param><param name="amount">Value that indicates how far to interpolate between the quaternions.</param><param name="result">[OutAttribute] Result of the interpolation.</param>
        public static void Slerp(ref Quaternion quaternion1, ref Quaternion quaternion2, float amount, out Quaternion result)
        {
            float num1 = amount;
            float num2 = (float)((double)quaternion1.X * (double)quaternion2.X + (double)quaternion1.Y * (double)quaternion2.Y + (double)quaternion1.Z * (double)quaternion2.Z + (double)quaternion1.W * (double)quaternion2.W);
            bool flag = false;
            if ((double)num2 < 0.0)
            {
                flag = true;
                num2 = -num2;
            }
            float num3;
            float num4;
            if ((double)num2 > 0.999998986721039)
            {
                num3 = 1f - num1;
                num4 = flag ? -num1 : num1;
            }
            else
            {
                float num5 = (float)Math.Acos((double)num2);
                float num6 = (float)(1.0 / Math.Sin((double)num5));
                num3 = (float)Math.Sin((1.0 - (double)num1) * (double)num5) * num6;
                num4 = flag ? (float)-Math.Sin((double)num1 * (double)num5) * num6 : (float)Math.Sin((double)num1 * (double)num5) * num6;
            }
            result.X = (float)((double)num3 * (double)quaternion1.X + (double)num4 * (double)quaternion2.X);
            result.Y = (float)((double)num3 * (double)quaternion1.Y + (double)num4 * (double)quaternion2.Y);
            result.Z = (float)((double)num3 * (double)quaternion1.Z + (double)num4 * (double)quaternion2.Z);
            result.W = (float)((double)num3 * (double)quaternion1.W + (double)num4 * (double)quaternion2.W);
        }

        /// <summary>
        /// Linearly interpolates between two quaternions.
        /// </summary>
        /// <param name="quaternion1">Source quaternion.</param><param name="quaternion2">Source quaternion.</param><param name="amount">Value indicating how far to interpolate between the quaternions.</param>
        public static Quaternion Lerp(Quaternion quaternion1, Quaternion quaternion2, float amount)
        {
            float num1 = amount;
            float num2 = 1f - num1;
            Quaternion quaternion = new Quaternion();
            if ((double)quaternion1.X * (double)quaternion2.X + (double)quaternion1.Y * (double)quaternion2.Y + (double)quaternion1.Z * (double)quaternion2.Z + (double)quaternion1.W * (double)quaternion2.W >= 0.0)
            {
                quaternion.X = (float)((double)num2 * (double)quaternion1.X + (double)num1 * (double)quaternion2.X);
                quaternion.Y = (float)((double)num2 * (double)quaternion1.Y + (double)num1 * (double)quaternion2.Y);
                quaternion.Z = (float)((double)num2 * (double)quaternion1.Z + (double)num1 * (double)quaternion2.Z);
                quaternion.W = (float)((double)num2 * (double)quaternion1.W + (double)num1 * (double)quaternion2.W);
            }
            else
            {
                quaternion.X = (float)((double)num2 * (double)quaternion1.X - (double)num1 * (double)quaternion2.X);
                quaternion.Y = (float)((double)num2 * (double)quaternion1.Y - (double)num1 * (double)quaternion2.Y);
                quaternion.Z = (float)((double)num2 * (double)quaternion1.Z - (double)num1 * (double)quaternion2.Z);
                quaternion.W = (float)((double)num2 * (double)quaternion1.W - (double)num1 * (double)quaternion2.W);
            }
            float num3 = 1f / (float)Math.Sqrt((double)quaternion.X * (double)quaternion.X + (double)quaternion.Y * (double)quaternion.Y + (double)quaternion.Z * (double)quaternion.Z + (double)quaternion.W * (double)quaternion.W);
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
        public static void Lerp(ref Quaternion quaternion1, ref Quaternion quaternion2, float amount, out Quaternion result)
        {
            float num1 = amount;
            float num2 = 1f - num1;
            if ((double)quaternion1.X * (double)quaternion2.X + (double)quaternion1.Y * (double)quaternion2.Y + (double)quaternion1.Z * (double)quaternion2.Z + (double)quaternion1.W * (double)quaternion2.W >= 0.0)
            {
                result.X = (float)((double)num2 * (double)quaternion1.X + (double)num1 * (double)quaternion2.X);
                result.Y = (float)((double)num2 * (double)quaternion1.Y + (double)num1 * (double)quaternion2.Y);
                result.Z = (float)((double)num2 * (double)quaternion1.Z + (double)num1 * (double)quaternion2.Z);
                result.W = (float)((double)num2 * (double)quaternion1.W + (double)num1 * (double)quaternion2.W);
            }
            else
            {
                result.X = (float)((double)num2 * (double)quaternion1.X - (double)num1 * (double)quaternion2.X);
                result.Y = (float)((double)num2 * (double)quaternion1.Y - (double)num1 * (double)quaternion2.Y);
                result.Z = (float)((double)num2 * (double)quaternion1.Z - (double)num1 * (double)quaternion2.Z);
                result.W = (float)((double)num2 * (double)quaternion1.W - (double)num1 * (double)quaternion2.W);
            }
            float num3 = 1f / (float)Math.Sqrt((double)result.X * (double)result.X + (double)result.Y * (double)result.Y + (double)result.Z * (double)result.Z + (double)result.W * (double)result.W);
            result.X *= num3;
            result.Y *= num3;
            result.Z *= num3;
            result.W *= num3;
        }

        /// <summary>
        /// Concatenates two Quaternions; the result represents the value1 rotation followed by the value2 rotation.
        /// </summary>
        /// <param name="value1">The first Quaternion rotation in the series.</param><param name="value2">The second Quaternion rotation in the series.</param>
        public static Quaternion Concatenate(Quaternion value1, Quaternion value2)
        {
            float num1 = value2.X;
            float num2 = value2.Y;
            float num3 = value2.Z;
            float num4 = value2.W;
            float num5 = value1.X;
            float num6 = value1.Y;
            float num7 = value1.Z;
            float num8 = value1.W;
            float num9 = (float)((double)num2 * (double)num7 - (double)num3 * (double)num6);
            float num10 = (float)((double)num3 * (double)num5 - (double)num1 * (double)num7);
            float num11 = (float)((double)num1 * (double)num6 - (double)num2 * (double)num5);
            float num12 = (float)((double)num1 * (double)num5 + (double)num2 * (double)num6 + (double)num3 * (double)num7);
            Quaternion quaternion;
            quaternion.X = (float)((double)num1 * (double)num8 + (double)num5 * (double)num4) + num9;
            quaternion.Y = (float)((double)num2 * (double)num8 + (double)num6 * (double)num4) + num10;
            quaternion.Z = (float)((double)num3 * (double)num8 + (double)num7 * (double)num4) + num11;
            quaternion.W = num4 * num8 - num12;
            return quaternion;
        }

        /// <summary>
        /// Concatenates two Quaternions; the result represents the value1 rotation followed by the value2 rotation.
        /// </summary>
        /// <param name="value1">The first Quaternion rotation in the series.</param><param name="value2">The second Quaternion rotation in the series.</param><param name="result">[OutAttribute] The Quaternion rotation representing the concatenation of value1 followed by value2.</param>
        public static void Concatenate(ref Quaternion value1, ref Quaternion value2, out Quaternion result)
        {
            float num1 = value2.X;
            float num2 = value2.Y;
            float num3 = value2.Z;
            float num4 = value2.W;
            float num5 = value1.X;
            float num6 = value1.Y;
            float num7 = value1.Z;
            float num8 = value1.W;
            float num9 = (float)((double)num2 * (double)num7 - (double)num3 * (double)num6);
            float num10 = (float)((double)num3 * (double)num5 - (double)num1 * (double)num7);
            float num11 = (float)((double)num1 * (double)num6 - (double)num2 * (double)num5);
            float num12 = (float)((double)num1 * (double)num5 + (double)num2 * (double)num6 + (double)num3 * (double)num7);
            result.X = (float)((double)num1 * (double)num8 + (double)num5 * (double)num4) + num9;
            result.Y = (float)((double)num2 * (double)num8 + (double)num6 * (double)num4) + num10;
            result.Z = (float)((double)num3 * (double)num8 + (double)num7 * (double)num4) + num11;
            result.W = num4 * num8 - num12;
        }

        /// <summary>
        /// Flips the sign of each component of the quaternion.
        /// </summary>
        /// <param name="quaternion">Source quaternion.</param>
        public static Quaternion Negate(Quaternion quaternion)
        {
            Quaternion quaternion1;
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
        public static void Negate(ref Quaternion quaternion, out Quaternion result)
        {
            result.X = -quaternion.X;
            result.Y = -quaternion.Y;
            result.Z = -quaternion.Z;
            result.W = -quaternion.W;
        }

        /// <summary>
        /// Adds two Quaternions.
        /// </summary>
        /// <param name="quaternion1">Quaternion to add.</param><param name="quaternion2">Quaternion to add.</param>
        public static Quaternion Add(Quaternion quaternion1, Quaternion quaternion2)
        {
            Quaternion quaternion;
            quaternion.X = quaternion1.X + quaternion2.X;
            quaternion.Y = quaternion1.Y + quaternion2.Y;
            quaternion.Z = quaternion1.Z + quaternion2.Z;
            quaternion.W = quaternion1.W + quaternion2.W;
            return quaternion;
        }

        /// <summary>
        /// Adds two Quaternions.
        /// </summary>
        /// <param name="quaternion1">Quaternion to add.</param><param name="quaternion2">Quaternion to add.</param><param name="result">[OutAttribute] Result of adding the Quaternions.</param>
        public static void Add(ref Quaternion quaternion1, ref Quaternion quaternion2, out Quaternion result)
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
        public static Quaternion Subtract(Quaternion quaternion1, Quaternion quaternion2)
        {
            Quaternion quaternion;
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
        public static void Subtract(ref Quaternion quaternion1, ref Quaternion quaternion2, out Quaternion result)
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
        public static Quaternion Multiply(Quaternion quaternion1, Quaternion quaternion2)
        {
            float num1 = quaternion1.X;
            float num2 = quaternion1.Y;
            float num3 = quaternion1.Z;
            float num4 = quaternion1.W;
            float num5 = quaternion2.X;
            float num6 = quaternion2.Y;
            float num7 = quaternion2.Z;
            float num8 = quaternion2.W;
            float num9 = (float)((double)num2 * (double)num7 - (double)num3 * (double)num6);
            float num10 = (float)((double)num3 * (double)num5 - (double)num1 * (double)num7);
            float num11 = (float)((double)num1 * (double)num6 - (double)num2 * (double)num5);
            float num12 = (float)((double)num1 * (double)num5 + (double)num2 * (double)num6 + (double)num3 * (double)num7);
            Quaternion quaternion;
            quaternion.X = (float)((double)num1 * (double)num8 + (double)num5 * (double)num4) + num9;
            quaternion.Y = (float)((double)num2 * (double)num8 + (double)num6 * (double)num4) + num10;
            quaternion.Z = (float)((double)num3 * (double)num8 + (double)num7 * (double)num4) + num11;
            quaternion.W = num4 * num8 - num12;
            return quaternion;
        }

        /// <summary>
        /// Multiplies two quaternions.
        /// </summary>
        /// <param name="quaternion1">The quaternion on the left of the multiplication.</param><param name="quaternion2">The quaternion on the right of the multiplication.</param><param name="result">[OutAttribute] The result of the multiplication.</param>
        public static void Multiply(ref Quaternion quaternion1, ref Quaternion quaternion2, out Quaternion result)
        {
            float num1 = quaternion1.X;
            float num2 = quaternion1.Y;
            float num3 = quaternion1.Z;
            float num4 = quaternion1.W;
            float num5 = quaternion2.X;
            float num6 = quaternion2.Y;
            float num7 = quaternion2.Z;
            float num8 = quaternion2.W;
            float num9 = (float)((double)num2 * (double)num7 - (double)num3 * (double)num6);
            float num10 = (float)((double)num3 * (double)num5 - (double)num1 * (double)num7);
            float num11 = (float)((double)num1 * (double)num6 - (double)num2 * (double)num5);
            float num12 = (float)((double)num1 * (double)num5 + (double)num2 * (double)num6 + (double)num3 * (double)num7);
            result.X = (float)((double)num1 * (double)num8 + (double)num5 * (double)num4) + num9;
            result.Y = (float)((double)num2 * (double)num8 + (double)num6 * (double)num4) + num10;
            result.Z = (float)((double)num3 * (double)num8 + (double)num7 * (double)num4) + num11;
            result.W = num4 * num8 - num12;
        }

        /// <summary>
        /// Multiplies a quaternion by a scalar value.
        /// </summary>
        /// <param name="quaternion1">Source quaternion.</param><param name="scaleFactor">Scalar value.</param>
        public static Quaternion Multiply(Quaternion quaternion1, float scaleFactor)
        {
            Quaternion quaternion;
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
        public static void Multiply(ref Quaternion quaternion1, float scaleFactor, out Quaternion result)
        {
            result.X = quaternion1.X * scaleFactor;
            result.Y = quaternion1.Y * scaleFactor;
            result.Z = quaternion1.Z * scaleFactor;
            result.W = quaternion1.W * scaleFactor;
        }

        /// <summary>
        /// Divides a Quaternion by another Quaternion.
        /// </summary>
        /// <param name="quaternion1">Source Quaternion.</param><param name="quaternion2">The divisor.</param>
        public static Quaternion Divide(Quaternion quaternion1, Quaternion quaternion2)
        {
            float num1 = quaternion1.X;
            float num2 = quaternion1.Y;
            float num3 = quaternion1.Z;
            float num4 = quaternion1.W;
            float num5 = 1f / (float)((double)quaternion2.X * (double)quaternion2.X + (double)quaternion2.Y * (double)quaternion2.Y + (double)quaternion2.Z * (double)quaternion2.Z + (double)quaternion2.W * (double)quaternion2.W);
            float num6 = -quaternion2.X * num5;
            float num7 = -quaternion2.Y * num5;
            float num8 = -quaternion2.Z * num5;
            float num9 = quaternion2.W * num5;
            float num10 = (float)((double)num2 * (double)num8 - (double)num3 * (double)num7);
            float num11 = (float)((double)num3 * (double)num6 - (double)num1 * (double)num8);
            float num12 = (float)((double)num1 * (double)num7 - (double)num2 * (double)num6);
            float num13 = (float)((double)num1 * (double)num6 + (double)num2 * (double)num7 + (double)num3 * (double)num8);
            Quaternion quaternion;
            quaternion.X = (float)((double)num1 * (double)num9 + (double)num6 * (double)num4) + num10;
            quaternion.Y = (float)((double)num2 * (double)num9 + (double)num7 * (double)num4) + num11;
            quaternion.Z = (float)((double)num3 * (double)num9 + (double)num8 * (double)num4) + num12;
            quaternion.W = num4 * num9 - num13;
            return quaternion;
        }

        /// <summary>
        /// Divides a Quaternion by another Quaternion.
        /// </summary>
        /// <param name="quaternion1">Source Quaternion.</param><param name="quaternion2">The divisor.</param><param name="result">[OutAttribute] Result of the division.</param>
        public static void Divide(ref Quaternion quaternion1, ref Quaternion quaternion2, out Quaternion result)
        {
            float num1 = quaternion1.X;
            float num2 = quaternion1.Y;
            float num3 = quaternion1.Z;
            float num4 = quaternion1.W;
            float num5 = 1f / (float)((double)quaternion2.X * (double)quaternion2.X + (double)quaternion2.Y * (double)quaternion2.Y + (double)quaternion2.Z * (double)quaternion2.Z + (double)quaternion2.W * (double)quaternion2.W);
            float num6 = -quaternion2.X * num5;
            float num7 = -quaternion2.Y * num5;
            float num8 = -quaternion2.Z * num5;
            float num9 = quaternion2.W * num5;
            float num10 = (float)((double)num2 * (double)num8 - (double)num3 * (double)num7);
            float num11 = (float)((double)num3 * (double)num6 - (double)num1 * (double)num8);
            float num12 = (float)((double)num1 * (double)num7 - (double)num2 * (double)num6);
            float num13 = (float)((double)num1 * (double)num6 + (double)num2 * (double)num7 + (double)num3 * (double)num8);
            result.X = (float)((double)num1 * (double)num9 + (double)num6 * (double)num4) + num10;
            result.Y = (float)((double)num2 * (double)num9 + (double)num7 * (double)num4) + num11;
            result.Z = (float)((double)num3 * (double)num9 + (double)num8 * (double)num4) + num12;
            result.W = num4 * num9 - num13;
        }

        public static Quaternion FromVector4(Vector4 v)
        {
            return new Quaternion(v.X, v.Y, v.Z, v.W);
        }

        public Vector4 ToVector4()
        {
            return new Vector4(X, Y, Z, W);
        }

        public static bool IsZero(Quaternion value)
        {
            return IsZero(value, 0.0001f);
        }

        // Per component IsZero, returns 1 for each component which equals to 0
        public static bool IsZero(Quaternion value, float epsilon)
        {
            return Math.Abs(value.X) < epsilon && Math.Abs(value.Y) < epsilon && Math.Abs(value.Z) < epsilon && Math.Abs(value.W) < epsilon;
        }

        /// <summary>
        /// Gets forward vector (0,0,-1) transformed by quaternion.
        /// </summary>
        public static void GetForward(ref Quaternion q, out Vector3 result)
        {
            float num1 = q.X + q.X;
            float num2 = q.Y + q.Y;
            float num3 = q.Z + q.Z;
            float num4 = q.W * num1;
            float num5 = q.W * num2;
            float num7 = q.X * num1;
            float num9 = q.X * num3;
            float num10 = q.Y * num2;
            float num11 = q.Y * num3;
            result.X = -num9 - num5;
            result.Y = num4 - num11;
            result.Z = num7 + num10 - 1.0f;
        }

        /// <summary>
        /// Gets right vector (1,0,0) transformed by quaternion.
        /// </summary>
        public static void GetRight(ref Quaternion q, out Vector3 result)
        {
            float num1 = q.X + q.X;
            float num2 = q.Y + q.Y;
            float num3 = q.Z + q.Z;
            float num5 = q.W * num2;
            float num6 = q.W * num3;
            float num8 = q.X * num2;
            float num9 = q.X * num3;
            float num10 = q.Y * num2;
            float num12 = q.Z * num3;
            result.X = 1.0f - num10 - num12;
            result.Y = num8 + num6;
            result.Z = num9 - num5;
        }

        /// <summary>
        /// Gets up vector (0,1,0) transformed by quaternion.
        /// </summary>
        public static void GetUp(ref Quaternion q, out Vector3 result)
        {
            float num1 = q.X + q.X;
            float num2 = q.Y + q.Y;
            float num3 = q.Z + q.Z;
            float num4 = q.W * num1;
            float num6 = q.W * num3;
            float num7 = q.X * num1;
            float num8 = q.X * num2;
            float num11 = q.Y * num3;
            float num12 = q.Z * num3;
            result.X = num8 - num6;
            result.Y = 1.0f - num7 - num12;
            result.Z = num11 + num4;
        }

        public float GetComponent(int index)
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
                    Debug.Assert(false);
                    return 0;
            }
        }
        public void SetComponent(int index, float value)
        {
            switch (index)
            {
                case 0:
                    X = value; break;
                case 1:
                    Y = value; break;
                case 2:
                    Z = value; break;
                case 3:
                    W = value; break;
                default:
                    Debug.Assert(false); break;
            }
        }
        public int FindLargestIndex()
        {
            int largestIndex = 0;
            float largest = X;
            for (int i = 1; i < 4; i++)
            {
                float v = Math.Abs(GetComponent(i));
                if (v > largest)
                {
                    largestIndex = i;
                    largest = v;
                }
            }
            return largestIndex;
        }
    }
}
