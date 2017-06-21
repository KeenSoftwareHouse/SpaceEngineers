
using System;
using VRageMath;

namespace VRageMath
{
    public static class MyMath
    {
        //Number of steps dividing whole circle
        private const float Size = 10000f;
        private static int ANGLE_GRANULARITY = 0;
        private static float[] m_precomputedValues = null;

        private static Vector3[] m_corners = new Vector3[8];

        private static readonly float OneOverRoot3 = (float)Math.Pow(3, -0.5f);
        public static Vector3 Vector3One = Vector3.One;

        public static void InitializeFastSin()
        {
            // Don't re-initialize look-up table if not necessary
            if (m_precomputedValues != null)
                return;

            ANGLE_GRANULARITY = 2 * (int)(Math.PI * Size);
            m_precomputedValues = new float[ANGLE_GRANULARITY];
            for (int i = 0; i < ANGLE_GRANULARITY; i++)
            {
                m_precomputedValues[i] = (float)Math.Sin(i / Size);
            }
        }

        public static float FastSin(float angle)
        {
            //Reduce angle to interval 0-2PI
            int angleInt = (int)(angle * Size);
            angleInt = angleInt % ANGLE_GRANULARITY;
            if (angleInt < 0)
                angleInt += ANGLE_GRANULARITY;

            return m_precomputedValues[angleInt];
        }

        public static float FastCos(float angle)
        {
            return FastSin(angle + MathHelper.PiOver2);
        }


        public static float NormalizeAngle(float angle, float center = 0.0f)
        {
            return angle - MathHelper.TwoPi * (float)Math.Floor((double)((angle + MathHelper.Pi - center) / MathHelper.TwoPi));
        }

        /// <summary>
        /// ArcTanAngle
        /// </summary>
        /// <returns>ArcTan angle between x and y</returns>
        public static float ArcTanAngle(float x, float y)
        {
            if (x == 0.0f)
            {
                if (y == 1.0f)
                {
                    return (float)MathHelper.PiOver2;
                }
                else
                {
                    return (float)-MathHelper.PiOver2;
                }
            }
            else if (x > 0.0f)
                return (float)Math.Atan(y / x);
            else if (x < 0.0f)
            {
                if (y > 0.0f)
                    return (float)Math.Atan(y / x) + MathHelper.Pi;
                else
                    return (float)Math.Atan(y / x) - MathHelper.Pi;
            }
            else
                return 0.0f;
        }

        public static Vector3 Abs(ref Vector3 vector)
        {
            return new Vector3(Math.Abs(vector.X), Math.Abs(vector.Y), Math.Abs(vector.Z));
        }

        /// <summary>
        /// Return vector with each component max
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static Vector3 MaxComponents(ref Vector3 a, ref Vector3 b)
        {
            return new Vector3(MathHelper.Max(a.X, b.X), MathHelper.Max(a.Y, b.Y), MathHelper.Max(a.Z, b.Z));
        }

        /// <summary>
        /// AngleTo 
        /// </summary>
        /// <returns>Angle between the vector lines</returns>
        public static Vector3 AngleTo(Vector3 From, Vector3 Location)
        {
            Vector3 angle = Vector3.Zero;
            Vector3 v = Vector3.Normalize(Location - From);
            angle.X = (float)Math.Asin(v.Y);
            angle.Y = ArcTanAngle(-v.Z, -v.X);
            return angle;
        }

        public static float AngleBetween(Vector3 a, Vector3 b)
        {
            var dotProd = Vector3.Dot(a, b);
            var lenProd = a.Length() * b.Length();
            var divOperation = dotProd / lenProd;
            if (Math.Abs(1.0f - divOperation) < 0.001f)
                return 0;
            else
                return (float)(Math.Acos(divOperation));
        }

        public static int Mod(int x, int m)
        {
            return (x % m + m) % m;
        }

        public static long Mod(long x, int m)
        {
            return (x % m + m) % m;
        }

        /// <summary>
        /// QuaternionToEuler 
        /// </summary>
        /// <returns>Converted quaternion to the euler pitch, rot, yaw</returns>
        public static Vector3 QuaternionToEuler(Quaternion Rotation)
        {
            Vector3 forward = Vector3.Transform(Vector3.Forward, Rotation);
            Vector3 up = Vector3.Transform(Vector3.Up, Rotation);
            Vector3 rotationAxes = AngleTo(new Vector3(), forward);
            if (rotationAxes.X == MathHelper.PiOver2)
            {
                rotationAxes.Y = ArcTanAngle(up.Z, up.X);
                rotationAxes.Z = 0.0f;
            }
            else if (rotationAxes.X == -MathHelper.PiOver2)
            {
                rotationAxes.Y = ArcTanAngle(-up.Y, -up.X);
                rotationAxes.Z = 0.0f;
            }
            else
            {
                up = Vector3.Transform(up, Matrix.CreateRotationY(-rotationAxes.Y));
                up = Vector3.Transform(up, Matrix.CreateRotationX(-rotationAxes.X));
                rotationAxes.Z = ArcTanAngle(up.Y, -up.X);
            }
            return rotationAxes;
        }


        /// <summary>
        /// This projection results to initial velocity of non-engine objects, which parents move in some velocity
        /// We want to add only forward speed of the parent to the forward direction of the object, and if parent
        /// is going backward, no speed is added.
        /// </summary>
        /// <param name="forwardVector"></param>
        /// <param name="projectedVector"></param>
        /// <returns></returns>
        public static Vector3 ForwardVectorProjection(Vector3 forwardVector, Vector3 projectedVector)
        {
            Vector3 forwardVelocity = forwardVector;

            if (Vector3.Dot(projectedVector, forwardVector) > 0)
            {  //going forward
                forwardVelocity = forwardVector.Project(projectedVector + forwardVector);
                return forwardVelocity;
            }

            return Vector3.Zero;
        }

        public static BoundingBox CreateFromInsideRadius(float radius)
        {
            float halfSize = OneOverRoot3 * radius;
            return new BoundingBox(-new Vector3(halfSize), new Vector3(halfSize));
        }

        /// <summary>
        /// Calculates color from vector
        /// </summary>
        public static Vector3 VectorFromColor(byte red, byte green, byte blue)
        {
            return new Vector3(red / 255.0f, green / 255.0f, blue / 255.0f);
        }
        public static Vector4 VectorFromColor(byte red, byte green, byte blue, byte alpha)
        {
            return new Vector4(red / 255.0f, green / 255.0f, blue / 255.0f, alpha / 255.0f);
        }


        /// <summary>
        /// Return minimum distance between line segment v-w and point p.
        /// </summary>
        public static float DistanceSquaredFromLineSegment(Vector3 v, Vector3 w, Vector3 p)
        {
            Vector3 d = w - v;
            float l = d.LengthSquared();
            if (l == 0) return Vector3.DistanceSquared(p, v);   // v == w case

            float t = Vector3.Dot(p - v, d);
            if (t <= 0) return Vector3.DistanceSquared(p, v);       // Beyond the 'v' end of the segment
            else if (t >= l) return Vector3.DistanceSquared(p, w);  // Beyond the 'w' end of the segment
            else return Vector3.DistanceSquared(p, v + (t / l) * d);        // On the segment
        }

        /**
         * Clamp the provided value to an interval.
         */
        public static float Clamp(float val, float min, float max)
        {
            if (val < min)
                return min;
            if (val > max)
                return max;
            return val;
        }
    }

    /// <summary>
    /// Usefull Vector3 extensions
    /// </summary>
    public static class Vector3Extensions
    {
        /// <summary>
        /// Calculates projection vector
        /// </summary>
        /// <param name="sb">The sb.</param>
        /// <param name="length">The length.</param>
        public static Vector3 Project(this Vector3 projectedOntoVector, Vector3 projectedVector)
        {
            float dotProduct = 0.0f;
            dotProduct = Vector3.Dot(projectedVector, projectedOntoVector);

            Vector3 projectedOutputVector = (dotProduct / projectedOntoVector.LengthSquared()) * projectedOntoVector;
            return projectedOutputVector;
        }
    }

    public static class BoundingBoxExtensions
    {

    }

    public static class BoundingFrustumExtensions
    {
        /// <summary>
        /// Creates bounding sphere from bounding frustum.
        /// Implementation taken from XNA source, replace IEnumerable with array
        /// </summary>
        /// <param name="frustum">The bounding frustum.</param>
        /// <param name="corners">Temporary memory to save corner when getting from frustum.</param>
        /// <returns>BoundingSphere</returns>
        public static BoundingSphere ToBoundingSphere(this BoundingFrustum frustum, Vector3[] corners)
        {
            float num;
            float num2;
            Vector3 vector2;
            float num4;
            float num5;
            BoundingSphere sphere;
            Vector3 vector5;
            Vector3 vector6;
            Vector3 vector7;
            Vector3 vector8;
            Vector3 vector9;

            if (corners.Length < 8)
            {
                throw new ArgumentException("Corners length must be at least 8");
            }

            frustum.GetCorners(corners);

            Vector3 vector4 = vector5 = vector6 = vector7 = vector8 = vector9 = corners[0];

            for (int i = 0; i < corners.Length; i++)
            {
                Vector3 vector = corners[i];

                if (vector.X < vector4.X)
                {
                    vector4 = vector;
                }
                if (vector.X > vector5.X)
                {
                    vector5 = vector;
                }
                if (vector.Y < vector6.Y)
                {
                    vector6 = vector;
                }
                if (vector.Y > vector7.Y)
                {
                    vector7 = vector;
                }
                if (vector.Z < vector8.Z)
                {
                    vector8 = vector;
                }
                if (vector.Z > vector9.Z)
                {
                    vector9 = vector;
                }
            }
            Vector3.Distance(ref vector5, ref vector4, out num5);
            Vector3.Distance(ref vector7, ref vector6, out num4);
            Vector3.Distance(ref vector9, ref vector8, out num2);
            if (num5 > num4)
            {
                if (num5 > num2)
                {
                    Vector3.Lerp(ref vector5, ref vector4, 0.5f, out vector2);
                    num = num5 * 0.5f;
                }
                else
                {
                    Vector3.Lerp(ref vector9, ref vector8, 0.5f, out vector2);
                    num = num2 * 0.5f;
                }
            }
            else if (num4 > num2)
            {
                Vector3.Lerp(ref vector7, ref vector6, 0.5f, out vector2);
                num = num4 * 0.5f;
            }
            else
            {
                Vector3.Lerp(ref vector9, ref vector8, 0.5f, out vector2);
                num = num2 * 0.5f;
            }
            for (int i = 0; i < corners.Length; i++)
            {
                Vector3 vector10 = corners[i];

                Vector3 vector3;
                vector3.X = vector10.X - vector2.X;
                vector3.Y = vector10.Y - vector2.Y;
                vector3.Z = vector10.Z - vector2.Z;
                float num3 = vector3.Length();
                if (num3 > num)
                {
                    num = (num + num3) * 0.5f;
                    vector2 += (Vector3)((1f - (num / num3)) * vector3);
                }
            }
            sphere.Center = vector2;
            sphere.Radius = num;
            return sphere;
        }      
    }

}
