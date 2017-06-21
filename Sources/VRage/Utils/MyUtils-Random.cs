using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using VRage.Library.Utils;
using VRageMath;

namespace VRage.Utils
{
    public static partial class MyUtils
    {
        [ThreadStatic]
        static Random m_secretRandom;

        [ThreadStatic]
        static MyRandom m_secretRandomVRage = new MyRandom();

        static Random m_random
        {
            get
            {
                if (m_secretRandom == null)
                {
                    if (VRage.Library.Utils.MyRandom.DisableRandomSeed)
                    {
                        m_secretRandom = new Random(1);
                    }
                    else
                    {
                        m_secretRandom = new Random();
                    }
                }
                return m_secretRandom;
            }
        }


        

        public static int GetRandomInt(int maxValue)
        {
            return m_random.Next(maxValue);
        }

        //  Return random int in range <minValue...maxValue>, the range of return values includes minValue but not maxValue
        public static int GetRandomInt(int minValue, int maxValue)
        {
            return m_random.Next(minValue, maxValue);
        }

        /// <summary>
        /// Returns a uniformly-distributed random vector from inside of a box (-1,-1,-1), (1, 1, 1)
        /// </summary>
        public static Vector3 GetRandomVector3()
        {
            return new Vector3(GetRandomFloat(-1, 1), GetRandomFloat(-1, 1), GetRandomFloat(-1, 1));
        }

        /// <summary>
        /// Returns a uniformly-distributed random vector from inside of a box (-1,-1,-1), (1, 1, 1)
        /// </summary>
        public static Vector3D GetRandomVector3D()
        {
            return new Vector3D(GetRandomFloat(-1, 1), GetRandomFloat(-1, 1), GetRandomFloat(-1, 1));
        }

        public static Vector3D GetRandomPerpendicularVector(ref Vector3D axis)
        {
            Debug.Assert(Vector3D.IsUnit(ref axis));
            Vector3D tangent = Vector3D.CalculatePerpendicularVector(axis);
            Vector3D bitangent; Vector3D.Cross(ref axis, ref tangent, out bitangent);
            double angle = GetRandomDouble(0, 2 * MathHelper.Pi);
            return Math.Cos(angle) * tangent + Math.Sin(angle) * bitangent;
        }

        public static Vector3D GetRandomDiscPosition(ref Vector3D center, double radius, ref Vector3D tangent, ref Vector3D bitangent)
        {
            Debug.Assert(Vector3D.IsUnit(ref tangent));
            Debug.Assert(Vector3D.IsUnit(ref bitangent));
            double radial = Math.Sqrt(GetRandomDouble(0, 1) * radius * radius);
            double angle = GetRandomDouble(0, 2 * MathHelper.Pi);
            return center + radial * (Math.Cos(angle) * tangent + Math.Sin(angle) * bitangent);
        }

        public static Vector3D GetRandomDiscPosition(ref Vector3D center, double minRadius, double maxRadius, ref Vector3D tangent, ref Vector3D bitangent)
        {
            Debug.Assert(Vector3D.IsUnit(ref tangent));
            Debug.Assert(Vector3D.IsUnit(ref bitangent));
            double radial = Math.Sqrt(GetRandomDouble(minRadius * minRadius, maxRadius * maxRadius));
            double angle = GetRandomDouble(0, 2 * MathHelper.Pi);
            return center + radial * (Math.Cos(angle) * tangent + Math.Sin(angle) * bitangent);
        }

        public static Vector3 GetRandomBorderPosition(ref BoundingSphere sphere)
        {
            return sphere.Center + GetRandomVector3Normalized() * sphere.Radius;
        }

        public static Vector3D GetRandomBorderPosition(ref BoundingSphereD sphere)
        {
            return sphere.Center + GetRandomVector3Normalized() * (float)sphere.Radius;
        }

        public static Vector3 GetRandomPosition(ref BoundingBox box)
        {
            return box.Center + GetRandomVector3() * box.HalfExtents;
        }

        public static Vector3D GetRandomPosition(ref BoundingBoxD box)
        {
            return box.Center + GetRandomVector3() * box.HalfExtents;
        }

        public static Vector3 GetRandomBorderPosition(ref BoundingBox box)
        {
            BoundingBoxD bbox = (BoundingBoxD)box;
            return (Vector3)GetRandomBorderPosition(ref bbox);
        }

        public static Vector3D GetRandomBorderPosition(ref BoundingBoxD box)
        {
            Vector3D vec = box.Size;

            // First, sample one of the six faces according to the face areas.
            // Then, sample point uniformly in the sampled face
            var surfaceReciproc = 2.0 / box.SurfaceArea;
            var probXY = vec.X * vec.Y * surfaceReciproc;
            var probXZ = vec.X * vec.Z * surfaceReciproc;
            var probYZ = 1.0f - probXY - probXZ;

            var rnd = m_random.NextDouble();
            if (rnd < probXY)
            {
                if (rnd < probXY * 0.5)
                    vec.Z = box.Min.Z;
                else
                    vec.Z = box.Max.Z;
                vec.X = GetRandomDouble(box.Min.X, box.Max.X);
                vec.Y = GetRandomDouble(box.Min.Y, box.Max.Y);
                return vec;
            }

            rnd -= probXY;
            if (rnd < probXZ)
            {
                if (rnd < probXZ * 0.5)
                    vec.Y = box.Min.Y;
                else
                    vec.Y = box.Max.Y;
                vec.X = GetRandomDouble(box.Min.X, box.Max.X);
                vec.Z = GetRandomDouble(box.Min.Z, box.Max.Z);
                return vec;
            }

            rnd -= probYZ;
            if (rnd < probYZ * 0.5)
                vec.X = box.Min.X;
            else
                vec.X = box.Max.X;
            vec.Y = GetRandomDouble(box.Min.Y, box.Max.Y);
            vec.Z = GetRandomDouble(box.Min.Z, box.Max.Z);
            return vec;
        }

        //  Return random Vector3, always normalized
        public static Vector3 GetRandomVector3Normalized()
        {
            float phi = GetRandomRadian();
            float z = GetRandomFloat(-1.0f, 1.0f);
            float root = (float)Math.Sqrt(1.0 - z * z);

            return new Vector3D(root * Math.Cos(phi), root * Math.Sin(phi), z);
        }

        //  Random vector distributed over the hemisphere about normal. 
        //  Returns random vector that always lies in hemisphere (half-sphere) defined by 'normal'
        public static Vector3 GetRandomVector3HemisphereNormalized(Vector3 normal)
        {
            Vector3 randomVector = GetRandomVector3Normalized();

            if (Vector3.Dot(randomVector, normal) < 0)
            {
                return -randomVector;
            }
            else
            {
                return randomVector;
            }
        }


        //  Returns random vector, whose direction is 'normal', but deviated by random angle (whose interval is 0..maxAngle in radians).
        public static Vector3 GetRandomVector3MaxAngle(float maxAngle)
        {
            float resultTheta = MyUtils.GetRandomFloat(-maxAngle, maxAngle);
            float resultPhi = MyUtils.GetRandomFloat(0, MathHelper.TwoPi);
			//  Convert to cartezian coordinates (XYZ)
            return -new Vector3(
                MyMath.FastSin(resultTheta) * MyMath.FastCos(resultPhi),
                MyMath.FastSin(resultTheta) * MyMath.FastSin(resultPhi),
                MyMath.FastCos(resultTheta)
                );
        }

        //  Random vector distributed over the circle about normal. 
        //  Returns random vector that always lies on circle
        public static Vector3 GetRandomVector3CircleNormalized()
        {
            float angle = MyUtils.GetRandomRadian();

            Vector3 v = new Vector3(
                (float)Math.Sin(angle),
                0,
                (float)Math.Cos(angle));

            return v;
        }

        //  Return by random +1 or -1. Nothing else. Propability is 50/50.
        public static float GetRandomSign()
        {
            return Math.Sign((float)m_random.NextDouble() - 0.5f);
        }

        public static float GetRandomFloat()
        {
            return (float)m_random.NextDouble();
        }

        public static float GetRandomFloat(int hash)
        {
            return m_secretRandomVRage.NextFloat(hash);
        }

        //  Return random float in range <minValue...maxValue>
        public static float GetRandomFloat(float minValue, float maxValue)
        {
            return VRage.Library.Utils.MyRandom.Instance.NextFloat() * (maxValue - minValue) + minValue;
        }

        //  Return random float in range <minValue...maxValue>
        public static float GetRandomFloat(int hash, float minValue, float maxValue)
        {
            return VRage.Library.Utils.MyRandom.Instance.NextFloat(hash) * (maxValue - minValue) + minValue;
        }

        //  Return random double in range <minValue...maxValue>
        public static double GetRandomDouble(double minValue, double maxValue)
        {
            return m_random.NextDouble() * (maxValue - minValue) + minValue;
        }

        //  Return random radian, covering whole circle 0..360 degrees (but returned value is in radians)
        public static float GetRandomRadian()
        {
            return GetRandomFloat(0, 2 * MathHelper.Pi);
        }

        static byte[] m_randomBuffer = new byte[8];
        public static long GetRandomLong()
        {
            m_random.NextBytes(m_randomBuffer);
            return BitConverter.ToInt64(m_randomBuffer, 0);
        }

        /// <summary>
        /// Returns a random TimeSpan between begin (inclusive) and end (exclusive).
        /// </summary>
        public static TimeSpan GetRandomTimeSpan(TimeSpan begin, TimeSpan end)
        {
            long rndTicks = GetRandomLong();
            return new TimeSpan(begin.Ticks + rndTicks % (end.Ticks - begin.Ticks));
        }
    }
}
