using System;

namespace VRageMath.PackedVector
{
    /// <summary>
    /// Packed vector type containing four 16-bit floating-point values.
    /// </summary>
	[Unsharper.UnsharperDisableReflection()]
	public struct HalfVector4 : IPackedVector<ulong>, IPackedVector, IEquatable<HalfVector4>
    {
        public ulong PackedValue;

        ulong IPackedVector<ulong>.PackedValue
        {
            get { return PackedValue; }
            set { PackedValue = value; }
        }

        /// <summary>
        /// Initializes a new instance of the HalfVector4 class.
        /// </summary>
        /// <param name="x">Initial value for the x component.</param><param name="y">Initial value for the y component.</param><param name="z">Initial value for the z component.</param><param name="w">Initial value for the w component.</param>
        public HalfVector4(float x, float y, float z, float w)
        {
            this.PackedValue = HalfVector4.PackHelper(x, y, z, w);
        }

        /// <summary>
        /// Initializes a new instance of the HalfVector4 structure.
        /// </summary>
        /// <param name="vector">A vector containing the initial values for the components of the HalfVector4 structure.</param>
        public HalfVector4(Vector4 vector)
        {
            this.PackedValue = HalfVector4.PackHelper(vector.X, vector.Y, vector.Z, vector.W);
        }

        public HalfVector4(HalfVector3 vector3, ushort w)
        {
            this.PackedValue = vector3.ToHalfVector4().PackedValue | (ulong) w << 48;
        }

        /// <summary>
        /// Compares the current instance of a class to another instance to determine whether they are the same.
        /// </summary>
        /// <param name="a">The object to the left of the equality operator.</param><param name="b">The object to the right of the equality operator.</param>
        public static bool operator ==(HalfVector4 a, HalfVector4 b)
        {
            return a.Equals(b);
        }

        /// <summary>
        /// Compares the current instance of a class to another instance to determine whether they are different.
        /// </summary>
        /// <param name="a">The object to the left of the equality operator.</param><param name="b">The object to the right of the equality operator.</param>
        public static bool operator !=(HalfVector4 a, HalfVector4 b)
        {
            return !a.Equals(b);
        }

        void IPackedVector.PackFromVector4(Vector4 vector)
        {
            this.PackedValue = HalfVector4.PackHelper(vector.X, vector.Y, vector.Z, vector.W);
        }

        private static ulong PackHelper(float vectorX, float vectorY, float vectorZ, float vectorW)
        {
            return (ulong)HalfUtils.Pack(vectorX) | (ulong)HalfUtils.Pack(vectorY) << 16 | (ulong)HalfUtils.Pack(vectorZ) << 32 | (ulong)HalfUtils.Pack(vectorW) << 48;
        }

        /// <summary>
        /// Expands the packed representation into a Vector4.
        /// </summary>
        public Vector4 ToVector4()
        {
            Vector4 vector4;
            vector4.X = HalfUtils.Unpack((ushort)this.PackedValue);
            vector4.Y = HalfUtils.Unpack((ushort)(this.PackedValue >> 16));
            vector4.Z = HalfUtils.Unpack((ushort)(this.PackedValue >> 32));
            vector4.W = HalfUtils.Unpack((ushort)(this.PackedValue >> 48));
            return vector4;
        }

        /// <summary>
        /// Returns a string representation of the current instance.
        /// </summary>
        public override string ToString()
        {
            return this.ToVector4().ToString();
        }

        /// <summary>
        /// Gets the hash code for the current instance.
        /// </summary>
        public override int GetHashCode()
        {
            return this.PackedValue.GetHashCode();
        }

        /// <summary>
        /// Returns a value that indicates whether the current instance is equal to a specified object.
        /// </summary>
        /// <param name="obj">The object with which to make the comparison.</param>
        public override bool Equals(object obj)
        {
            if (obj is HalfVector4)
                return this.Equals((HalfVector4)obj);
            else
                return false;
        }

        /// <summary>
        /// Returns a value that indicates whether the current instance is equal to a specified object.
        /// </summary>
        /// <param name="other">The object with which to make the comparison.</param>
        public bool Equals(HalfVector4 other)
        {
            return this.PackedValue.Equals(other.PackedValue);
        }
    }
}
