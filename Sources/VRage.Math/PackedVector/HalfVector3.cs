using System;

namespace VRageMath.PackedVector
{
    /// <summary>
    /// Packed vector type containing four 16-bit floating-point values.
    /// </summary>
    public struct HalfVector3
    {
        public ushort X, Y, Z;

        /// <summary>
        /// Initializes a new instance of the HalfVector3 class.
        /// </summary>
        /// <param name="x">Initial value for the x component.</param><param name="y">Initial value for the y component.</param><param name="z">Initial value for the z component.</param><param name="w">Initial value for the w component.</param>
        public HalfVector3(float x, float y, float z)
        {
            X = HalfUtils.Pack(x);
            Y = HalfUtils.Pack(y);
            Z = HalfUtils.Pack(z);
        }

        /// <summary>
        /// Initializes a new instance of the HalfVector3 structure.
        /// </summary>
        /// <param name="vector">A vector containing the initial values for the components of the HalfVector3 structure.</param>
        public HalfVector3(Vector3 vector)
            : this(vector.X, vector.Y, vector.Z)
        {
        }

        /// <summary>
        /// Expands the packed representation into a Vector4.
        /// </summary>
        public Vector3 ToVector3()
        {
            Vector3 vector3;
            vector3.X = HalfUtils.Unpack(X);
            vector3.Y = HalfUtils.Unpack(Y);
            vector3.Z = HalfUtils.Unpack(Z);
            return vector3;
        }

        public HalfVector4 ToHalfVector4()
        {
            HalfVector4 v4;
            v4.PackedValue = ((ulong)X) | ((ulong)Y << 16) | ((ulong)Z << 32);
            return v4;
        }

        public static implicit operator HalfVector3(Vector3 v)
        {
            return new HalfVector3(v);
        }

        public static implicit operator Vector3(HalfVector3 v)
        {
            return v.ToVector3();
        }

        /// <summary>
        /// Returns a string representation of the current instance.
        /// </summary>
        public override string ToString()
        {
            return this.ToVector3().ToString();
        }
    }
}
