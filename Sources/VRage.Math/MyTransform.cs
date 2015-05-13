using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VRageMath
{
    public struct MyTransform
    {
        public Quaternion Rotation;
        public Vector3 Position;
        public Matrix TransformMatrix
        {
            get
            {
                Matrix m = Matrix.CreateFromQuaternion(Rotation);
                m.Translation = Position;
                return m;
            }
        }


        public MyTransform(Vector3 position) : this(ref position) { }
        public MyTransform(Matrix matrix) : this(ref matrix) { }

        public MyTransform(ref Vector3 position)
        {
            Rotation = Quaternion.Identity;
            Position = position;
        }

        public MyTransform(ref Matrix matrix)
        {
            Quaternion.CreateFromRotationMatrix(ref matrix, out Rotation);
            Position = matrix.Translation;
        }

        public static MyTransform Transform(ref MyTransform t1, ref MyTransform t2)
        {
            MyTransform result;
            Transform(ref t1, ref t2, out result);
            return result;
        }

        public static void Transform(ref MyTransform t1, ref MyTransform t2, out MyTransform result)
        {
            Vector3 newPos;
            Vector3.Transform(ref t1.Position, ref t2.Rotation, out newPos);
            newPos += t2.Position;
            Quaternion newRot;
            Quaternion.Multiply(ref t1.Rotation, ref t2.Rotation, out newRot);
            result.Position = newPos;
            result.Rotation = newRot;
        }

        public static Vector3 Transform(ref Vector3 v, ref MyTransform t2)
        {
            Vector3 result;
            Transform(ref v, ref t2, out result);
            return result;
        }

        public static void Transform(ref Vector3 v, ref MyTransform t2, out Vector3 result)
        {
            Vector3.Transform(ref v, ref t2.Rotation, out result);
            result += t2.Position;
        }
    }
}
