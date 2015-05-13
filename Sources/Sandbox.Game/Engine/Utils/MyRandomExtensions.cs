using Sandbox.Common;
using Sandbox.Engine.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.Library.Utils;
using VRageMath;

namespace VRage
{
    public static class MyRandomExtensions
    {
        /// <summary>
        /// Normal distribution, Returns number from -3,3
        /// </summary>
        /// <param name="rnd"></param>
        /// <returns></returns>
        public static float FloatNormal(this MyRandom rnd)
        {
            // Use Box-Muller algorithm
            double u1 = rnd.NextDouble();
            double u2 = rnd.NextDouble();
            double r = Math.Sqrt(-2.0 * Math.Log(u1));
            double theta = 2.0 * Math.PI * u2;
            return (float)(r * Math.Sin(theta));
            //return (phi((float)rnd.NextDouble() * 2 - 1) - 0.15f) / 0.7f;
        }

        /// <summary>
        /// Some distribution, probably normal?
        /// </summary>
        /// <param name="standardDeviation">0.2f gets numbers aprox from -1,1</param>
        public static float FloatNormal(this MyRandom rnd, float mean, float standardDeviation)
        {
            if (standardDeviation <= 0.0)
            {
                string msg = string.Format("Shape must be positive. Received {0}.", standardDeviation);
                throw new ArgumentOutOfRangeException(msg);
            }
            return mean + standardDeviation * FloatNormal(rnd);
        }

        /// <summary>
        /// Returns exponentially distributed numbers.
        /// For example, time between events of a Poisson process (i.e. events that happen independently of each other with a
        /// constant rate of generation - raindrops falling onto a surface, incoming meteors, webserver requests, etc.) is an
        /// exponentially distributed random variable.
        /// </summary>
        /// <param name="mean">Mean value of the exponential distribution. This is the same as 1/lambda.</param>
        public static float FloatExponential(this MyRandom rnd, float mean)
        {
            if (mean <= 0.0)
            {
                string msg = string.Format("Mean of exponential distribution must be positive. Received {0}.", mean);
                throw new ArgumentOutOfRangeException(msg);
            }
            return (float)(-Math.Log(rnd.NextDouble()) * mean);
        }

        public static float phi(float x)
        {
            const float a1 = 0.254829592f;
            const float a2 = -0.284496736f;
            const float a3 = 1.421413741f;
            const float a4 = -1.453152027f;
            const float a5 = 1.061405429f;
            const float p = 0.3275911f;

            float sign = 1;
            if (x < 0)
            {
                sign = -1;
            }
            x = (float)(Math.Abs(x) / Math.Sqrt(2.0f));

            //# A&S formula 7.1.26
            float t = 1.0f / (1.0f + p * x);
            float y = 1.0f - (((((a5 * t + a4) * t) + a3) * t + a2) * t + a1) * t * (float)Math.Exp(-x * x);

            return 0.5f * (1.0f + sign * y);
        }

        public static float NextFloat(this MyRandom random, float minValue, float maxValue)
        {
            return (float)random.NextDouble() * (maxValue - minValue) + minValue;
        }

        /// <summary>
        /// Create random vector, whose direction is 'originalVector', but deviated by random angle (whose interval is 0..maxAngle).
        /// Use if you want deviate vector by a smal amount (e.g. debris thrown from projectile hit point)
        /// </summary>
        public static Vector3 NextDeviatingVector(this MyRandom random, Vector3 originalVector, float maxAngle)
        {
            var matrix = Matrix.CreateFromDir(originalVector);
            return random.NextDeviatingVector(ref matrix, maxAngle);
        }

        /// <summary>
        /// Create random vector, whose direction is 'originalVector', but deviated by random angle (whose interval is 0..maxAngle).
        /// Use if you want deviate vector by a smal amount (e.g. debris thrown from projectile hit point)
        /// Optimized version with Matrix precalculated
        /// </summary>
        public static Vector3 NextDeviatingVector(this MyRandom random, ref Matrix matrix, float maxAngle)
        {
            float resultTheta = random.NextFloat(-maxAngle, maxAngle);
            float resultPhi = random.NextFloat(0, MathHelper.TwoPi);
            //  Convert to cartezian coordinates (XYZ)
            Vector3 result = -new Vector3(
                MyMath.FastSin(resultTheta) * MyMath.FastCos(resultPhi),
                MyMath.FastSin(resultTheta) * MyMath.FastSin(resultPhi),
                MyMath.FastCos(resultTheta)
                );

            return Vector3.TransformNormal(result, matrix);
        }
    }
}
