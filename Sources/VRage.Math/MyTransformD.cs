using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.Serialization;

namespace VRageMath
{
    public struct MyTransformD
    {
        [Serialize(MyPrimitiveFlags.Normalized)]
        public Quaternion Rotation;
        public Vector3D Position;
        public MatrixD TransformMatrix
        {
            get
            {
                MatrixD m = MatrixD.CreateFromQuaternion(Rotation);
                m.Translation = Position;
                return m;
            }
        }


        public MyTransformD(Vector3D position) : this(ref position) { }
        public MyTransformD(MatrixD matrix) : this(ref matrix) { }

        public MyTransformD(ref Vector3D position)
        {
            Rotation = Quaternion.Identity;
            Position = position;
        }

        public MyTransformD(ref MatrixD matrix)
        {
            Quaternion.CreateFromRotationMatrix(ref matrix, out Rotation);
            Position = matrix.Translation;
        }

        public static MyTransformD Transform(ref MyTransformD t1, ref MyTransformD t2)
        {
            MyTransformD result;
            Transform(ref t1, ref t2, out result);
            return result;
        }

        public static void Transform(ref MyTransformD t1, ref MyTransformD t2, out MyTransformD result)
        {
            Vector3D newPos;
            Vector3D.Transform(ref t1.Position, ref t2.Rotation, out newPos);
            newPos += t2.Position;
            Quaternion newRot;
            Quaternion.Multiply(ref t1.Rotation, ref t2.Rotation, out newRot);
            result.Position = newPos;
            result.Rotation = newRot;
        }

        public static Vector3D Transform(ref Vector3D v, ref MyTransformD t2)
        {
            Vector3D result;
            Transform(ref v, ref t2, out result);
            return result;
        }

        public static void Transform(ref Vector3D v, ref MyTransformD t2, out Vector3D result)
        {
            Vector3D.Transform(ref v, ref t2.Rotation, out result);
            result += t2.Position;
        }
    }
}
