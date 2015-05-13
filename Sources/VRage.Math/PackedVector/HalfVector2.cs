using System;

namespace VRageMath.PackedVector
{
    /// <summary>
    /// Packed vector type containing two 16-bit floating-point values.
    /// </summary>
    public struct HalfVector2 : IPackedVector<uint>, IPackedVector, IEquatable<HalfVector2>
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
        /// Initializes a new instance of the HalfVector2 structure.
        /// </summary>
        /// <param name="x">Initial value for the x component.</param><param name="y">Initial value for the y component.</param>
        public HalfVector2(float x, float y)
        {
            this.packedValue = HalfVector2.PackHelper(x, y);
        }

        /// <summary>
        /// Initializes a new instance of the HalfVector2 structure.
        /// </summary>
        /// <param name="vector">A vector containing the initial values for the components of the HalfVector2 structure.</param>
        public HalfVector2(Vector2 vector)
        {
            this.packedValue = HalfVector2.PackHelper(vector.X, vector.Y);
        }

        /// <summary>
        /// Compares the current instance of a class to another instance to determine whether they are the same.
        /// </summary>
        /// <param name="a">The object to the left of the equality operator.</param><param name="b">The object to the right of the equality operator.</param>
        public static bool operator ==(HalfVector2 a, HalfVector2 b)
        {
            return a.Equals(b);
        }

        /// <summary>
        /// Compares the current instance of a class to another instance to determine whether they are different.
        /// </summary>
        /// <param name="a">The object to the left of the equality operator.</param><param name="b">The object to the right of the equality operator.</param>
        public static bool operator !=(HalfVector2 a, HalfVector2 b)
        {
            return !a.Equals(b);
        }

        void IPackedVector.PackFromVector4(Vector4 vector)
        {
            this.packedValue = HalfVector2.PackHelper(vector.X, vector.Y);
        }

        private static uint PackHelper(float vectorX, float vectorY)
        {
            return (uint)HalfUtils.Pack(vectorX) | (uint)HalfUtils.Pack(vectorY) << 16;
        }

        /// <summary>
        /// Expands the HalfVector2 to a Vector2.
        /// </summary>
        public Vector2 ToVector2()
        {
            Vector2 vector2;
            vector2.X = HalfUtils.Unpack((ushort)this.packedValue);
            vector2.Y = HalfUtils.Unpack((ushort)(this.packedValue >> 16));
            return vector2;
        }

        Vector4 IPackedVector.ToVector4()
        {
            Vector2 vector2 = this.ToVector2();
            return new Vector4(vector2.X, vector2.Y, 0.0f, 1f);
        }

        /// <summary>
        /// Returns a string representation of the current instance.
        /// </summary>
        public override string ToString()
        {
            return this.ToVector2().ToString();
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
            if (obj is HalfVector2)
                return this.Equals((HalfVector2)obj);
            else
                return false;
        }

        /// <summary>
        /// Returns a value that indicates whether the current instance is equal to a specified object.
        /// </summary>
        /// <param name="other">The object with which to make the comparison.</param>
        public bool Equals(HalfVector2 other)
        {
            return this.packedValue.Equals(other.packedValue);
        }
    }
}
