
namespace VRageMath.PackedVector
{
    /// <summary>
    /// Interface that converts packed vector types to and from Vector4 values, allowing multiple encodings to be manipulated in a generic way.
    /// </summary>
    public interface IPackedVector
    {
        /// <summary>
        /// Expands the packed representation into a Vector4.
        /// </summary>
        Vector4 ToVector4();

        /// <summary>
        /// Sets the packed representation from a Vector4.
        /// </summary>
        /// <param name="vector">The vector to create the packed representation from.</param>
        void PackFromVector4(Vector4 vector);
    }

    /// <summary>
    /// Converts packed vector types to and from Vector4 values.
    /// </summary>
    public interface IPackedVector<TPacked> : IPackedVector
    {
        /// <summary>
        /// Directly gets or sets the packed representation of the value.
        /// </summary>
        TPacked PackedValue { get; set; }
    }
}

