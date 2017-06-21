using System;
using VRageMath;
using VRage.Utils;
using Sandbox.Common;

namespace Sandbox.Engine.Utils
{
    //  Use this struct to generate random vectors deviated around 'originalVector' by random vector of limit 'maxAngle'
    //  It can be used by creating instance of this struct (if you need more random vectors deviated from one basic), or
    //  by calling static one-time method MyUtilRandomVector3ByDeviatingVector.GetRandom(...original vector..., ...max angle...)
    struct MyUtilRandomVector3ByDeviatingVector
    {
        Matrix m_matrix;

        public MyUtilRandomVector3ByDeviatingVector(Vector3 originalVector)
        {
            m_matrix = Matrix.CreateFromDir(originalVector);
        }

        //  Create random vector, whose direction is 'originalVector', but deviated by random angle (whose interval is 0..maxAngle).
        //  Use if you want deviate vector by a smal amount (e.g. debris thrown from projectile hit point)
        public Vector3 GetNext(float maxAngle)
        {
            float resultTheta = MyUtils.GetRandomFloat(-maxAngle, maxAngle);
            float resultPhi = MyUtils.GetRandomFloat(0, MathHelper.TwoPi);
			//  Convert to cartezian coordinates (XYZ)
            Vector3 result = -new Vector3(
                MyMath.FastSin(resultTheta) * MyMath.FastCos(resultPhi),
                MyMath.FastSin(resultTheta) * MyMath.FastSin(resultPhi),
                MyMath.FastCos(resultTheta)
                );

            return Vector3.TransformNormal(result, m_matrix);
        }

        public Vector3 GetNext(int hash, float maxAngle)
        {
            float resultTheta = MyUtils.GetRandomFloat(hash, -maxAngle, maxAngle);
            float resultPhi = MyUtils.GetRandomFloat(hash, 0, MathHelper.TwoPi);
            //  Convert to cartezian coordinates (XYZ)
            Vector3 result = -new Vector3(
                MyMath.FastSin(resultTheta) * MyMath.FastCos(resultPhi),
                MyMath.FastSin(resultTheta) * MyMath.FastSin(resultPhi),
                MyMath.FastCos(resultTheta)
                );

            return Vector3.TransformNormal(result, m_matrix);
        }

        //  One-time call
        public static Vector3 GetRandom(Vector3 originalVector, float maxAngle)
        {
            if (maxAngle == 0)
                return originalVector;

            MyUtilRandomVector3ByDeviatingVector rnd = new MyUtilRandomVector3ByDeviatingVector(originalVector);
            return rnd.GetNext(maxAngle);
        }

        public static Vector3 GetRandom(int hash, Vector3 originalVector, float maxAngle)
        {
            if (maxAngle == 0)
                return originalVector;

            MyUtilRandomVector3ByDeviatingVector rnd = new MyUtilRandomVector3ByDeviatingVector(originalVector);
            return rnd.GetNext(hash, maxAngle);
        }
    }
}
