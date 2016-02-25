using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.Serialization;
using VRageMath;
using VRageMath.PackedVector;

namespace VRage
{
    /// <summary>
    /// Transform structure for delta-transforms.
    /// </summary>
    [ProtoBuf.ProtoContract, Serializable]
    public struct MyDeltaTransform // 28 B
    {
        [NoSerialize]
        public Quaternion OrientationOffset; // Quaternion wasn't enough for delta transform (because delta transform is not normalized?)

        [Serialize]
        public Vector4 OrientationAsVector // 16 B
        {
            get { return OrientationOffset.ToVector4(); }
            set { OrientationOffset = Quaternion.FromVector4(value); }
        }

        public Vector3 PositionOffset; // 12 B

        public bool IsZero
        {
            get { return PositionOffset == Vector3.Zero && OrientationOffset == Quaternion.Zero; }
        }

        public static implicit operator Matrix(MyDeltaTransform transform)
        {
            Matrix result;
            Matrix.CreateFromQuaternion(ref transform.OrientationOffset, out result);
            result.Translation = transform.PositionOffset;
            return result;
        }

        public static implicit operator MyDeltaTransform(Matrix matrix)
        {
            MyDeltaTransform result;
            result.PositionOffset = matrix.Translation;
            Quaternion.CreateFromRotationMatrix(ref matrix, out result.OrientationOffset);
            return result;
        }

        public static implicit operator MatrixD(MyDeltaTransform transform)
        {
            MatrixD result;
            MatrixD.CreateFromQuaternion(ref transform.OrientationOffset, out result);
            result.Translation = (Vector3)transform.PositionOffset;
            return result;
        }

        public static implicit operator MyDeltaTransform(MatrixD matrix)
        {
            MyDeltaTransform result;
            result.PositionOffset = (Vector3)matrix.Translation;
            Quaternion.CreateFromRotationMatrix(ref matrix, out result.OrientationOffset);
            return result;
        }

        public static implicit operator MyPositionAndOrientation(MyDeltaTransform deltaTransform)
        {
            return new MyPositionAndOrientation((Vector3)deltaTransform.PositionOffset, deltaTransform.OrientationOffset.Forward, deltaTransform.OrientationOffset.Up);
        }

        public static implicit operator MyDeltaTransform(MyPositionAndOrientation value)
        {
            return new MyDeltaTransform() { PositionOffset = (Vector3)(Vector3D)value.Position, OrientationOffset = Quaternion.CreateFromForwardUp(value.Forward, value.Up) };
        }
    }
}
