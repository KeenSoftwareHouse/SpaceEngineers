// Matrix with double precision floating point support

using System;
using VRageMath.PackedVector;

namespace VRageMath
{
    /// <summary>
    /// Defines a matrix.
    /// </summary>
    [ProtoBuf.ProtoContract, Serializable]
    public struct CompressedPositionOrientation
    {
        public Vector3 Position;
        public HalfVector4 Orientation;

        public Matrix Matrix
        {
            get { Matrix m; ToMatrix(out m); return m; }
            set { FromMatrix(ref value); }
        }

        public CompressedPositionOrientation(ref Matrix matrix)
        {
            Position = matrix.Translation;
            Quaternion q;
            Quaternion.CreateFromRotationMatrix(ref matrix, out q);
            Orientation = new HalfVector4(q.ToVector4());
        }

        public void FromMatrix(ref Matrix matrix)
        {
            Position = matrix.Translation;
            Quaternion q;
            Quaternion.CreateFromRotationMatrix(ref matrix, out q);
            Orientation = new HalfVector4(q.ToVector4());
        }

        public void ToMatrix(out Matrix result)
        {
            var q = Quaternion.FromVector4(Orientation.ToVector4());
            Matrix.CreateFromQuaternion(ref q, out result);
            result.Translation = Position;
        }
    }
}
