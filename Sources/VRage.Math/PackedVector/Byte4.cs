using System;
using System.Globalization;

namespace VRageMath.PackedVector
{
    /// <summary>
    /// Packed vector type containing four 8-bit unsigned integer values, ranging from 0 to 255.
    /// </summary>
    public struct Byte4 : IPackedVector<uint>, IPackedVector, IEquatable<Byte4>
    {
        private uint packedValue;

        /// <summary>
        /// Directly gets or sets the packed representation of the value.
        /// </summary>
        public uint PackedValue
        {
            get
            {
                return this.packedValue;
            }
            set
            {
                this.packedValue = value;
            }
        }

        /// <summary>
        /// Initializes a new instance of the Byte4 class.
        /// </summary>
        /// <param name="x">Initial value for the x component.</param><param name="y">Initial value for the y component.</param><param name="z">Initial value for the z component.</param><param name="w">Initial value for the w component.</param>
        public Byte4(float x, float y, float z, float w)
        {
            this.packedValue = Byte4.PackHelper(x, y, z, w);
        }

        /// <summary>
        /// Initializes a new instance of the Byte4 structure.
        /// </summary>
        /// <param name="vector">A vector containing the initial values for the components of the Byte4 structure.</param>
        public Byte4(Vector4 vector)
        {
            this.packedValue = Byte4.PackHelper(vector.X, vector.Y, vector.Z, vector.W);
        }

        public Byte4(uint packedValue)
        {
            this.packedValue = packedValue;
        }

        /// <summary>
        /// Compares the current instance of a class to another instance to determine whether they are the same.
        /// </summary>
        /// <param name="a">The object to the left of the equality operator.</param><param name="b">The object to the right of the equality operator.</param>
        public static bool operator ==(Byte4 a, Byte4 b)
        {
            return a.Equals(b);
        }

        /// <summary>
        /// Compares the current instance of a class to another instance to determine whether they are different.
        /// </summary>
        /// <param name="a">The object to the left of the equality operator.</param><param name="b">The object to the right of the equality operator.</param>
        public static bool operator !=(Byte4 a, Byte4 b)
        {
            return !a.Equals(b);
        }

        void IPackedVector.PackFromVector4(Vector4 vector)
        {
            this.packedValue = Byte4.PackHelper(vector.X, vector.Y, vector.Z, vector.W);
        }

        private static uint PackHelper(float vectorX, float vectorY, float vectorZ, float vectorW)
        {
            return PackUtils.PackUnsigned((float)byte.MaxValue, vectorX) | PackUtils.PackUnsigned((float)byte.MaxValue, vectorY) << 8 | PackUtils.PackUnsigned((float)byte.MaxValue, vectorZ) << 16 | PackUtils.PackUnsigned((float)byte.MaxValue, vectorW) << 24;
        }

        /// <summary>
        /// Expands the packed representation into a Vector4.
        /// </summary>
        public Vector4 ToVector4()
        {
            Vector4 vector4;
            vector4.X = (float)(this.packedValue & (uint)byte.MaxValue);
            vector4.Y = (float)(this.packedValue >> 8 & (uint)byte.MaxValue);
            vector4.Z = (float)(this.packedValue >> 16 & (uint)byte.MaxValue);
            vector4.W = (float)(this.packedValue >> 24 & (uint)byte.MaxValue);
            return vector4;
        }

        public Vector4UByte ToVector4UByte()
        {
            Vector4UByte vector4;
            vector4.X = (byte)(this.packedValue & (uint)byte.MaxValue);
            vector4.Y = (byte)(this.packedValue >> 8 & (uint)byte.MaxValue);
            vector4.Z = (byte)(this.packedValue >> 16 & (uint)byte.MaxValue);
            vector4.W = (byte)(this.packedValue >> 24 & (uint)byte.MaxValue);
            return vector4;
        }

        /// <summary>
        /// Returns a string representation of the current instance.
        /// </summary>
        public override string ToString()
        {
            return this.packedValue.ToString("X8", (IFormatProvider)CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Gets the hash code for the current instance.
        /// </summary>
        public override int GetHashCode()
        {
            return this.packedValue.GetHashCode();
        }

        /// <summary>
        /// Returns a value that indicates whether the current instance is equal to a specified object.
        /// </summary>
        /// <param name="obj">The object with which to make the comparison.</param>
        public override bool Equals(object obj)
        {
            if (obj is Byte4)
                return this.Equals((Byte4)obj);
            else
                return false;
        }

        /// <summary>
        /// Returns a value that indicates whether the current instance is equal to a specified object.
        /// </summary>
        /// <param name="other">The object with which to make the comparison.</param>
        public bool Equals(Byte4 other)
        {
            return this.packedValue.Equals(other.packedValue);
        }
    }
}
