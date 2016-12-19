using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using VRage;

namespace VRageMath
{
    /// <summary>
    /// Contains commonly used precalculated values.
    /// </summary>
    public static class MathHelper
    {
        /// <summary>
        /// Represents the mathematical constant e.
        /// </summary>
        public const float E = 2.718282f;
        /// <summary>
        /// Represents the log base two of e.
        /// </summary>
        public const float Log2E = 1.442695f;
        /// <summary>
        /// Represents the log base ten of e.
        /// </summary>
        public const float Log10E = 0.4342945f;
        /// <summary>
        /// Represents the value of pi.
        /// </summary>
        public const float Pi = 3.141593f;
        /// <summary>
        /// Represents the value of pi times two.
        /// </summary>
        public const float TwoPi = 6.28318530718f;
        /// <summary>
        /// Represents the value of pi times two.
        /// </summary>
        public const float FourPi = 12.5663706144f;
        /// <summary>
        /// Represents the value of pi divided by two.
        /// </summary>
        public const float PiOver2 = 1.570796f;
        /// <summary>
        /// Represents the value of pi divided by four.
        /// </summary>
        public const float PiOver4 = 0.7853982f;
        /// <summary>
        /// Represents the value of the square root of two
        /// </summary>
        public const float Sqrt2 = 1.4142135623730951f;
        /// <summary>
        /// Represents the value of the square root of three
        /// </summary>
        public const float Sqrt3 = 1.7320508075688773f;
        /// <summary>
        /// 60 / 2*pi
        /// </summary>
        public const float RadiansPerSecondToRPM = 9.549296585513720f;
        /// <summary>
        /// 2*pi / 60
        /// </summary>
        public const float RPMToRadiansPerSecond = 0.104719755119660f;
        /// <summary>
        /// 2*pi / 60000
        /// </summary>
        public const float RPMToRadiansPerMillisec = 0.00010471975512f;

        /// <summary>
        /// Converts degrees to radians.
        /// </summary>
        /// <param name="degrees">The angle in degrees.</param>
        public static float ToRadians(float degrees)
        {
            return (degrees / 360.0f) * TwoPi;
        }

        /// <summary>
        /// Converts radians to degrees.
        /// </summary>
        /// <param name="radians">The angle in radians.</param>
        public static float ToDegrees(float radians)
        {
            return radians * 57.29578f;
        }

        public static double ToDegrees(double radians)
        {
            return radians * 57.29578;
        }

        /// <summary>
        /// Calculates the absolute value of the difference of two values.
        /// </summary>
        /// <param name="value1">Source value.</param><param name="value2">Source value.</param>
        public static float Distance(float value1, float value2)
        {
            return Math.Abs(value1 - value2);
        }

        /// <summary>
        /// Returns the lesser of two values.
        /// </summary>
        /// <param name="value1">Source value.</param><param name="value2">Source value.</param>
        public static float Min(float value1, float value2)
        {
            return Math.Min(value1, value2);
        }

        /// <summary>
        /// Returns the greater of two values.
        /// </summary>
        /// <param name="value1">Source value.</param><param name="value2">Source value.</param>
        public static float Max(float value1, float value2)
        {
            return Math.Max(value1, value2);
        }

        /// <summary>
        /// Returns the lesser of two values.
        /// </summary>
        /// <param name="value1">Source value.</param><param name="value2">Source value.</param>
        public static double Min(double value1, double value2)
        {
            return Math.Min(value1, value2);
        }

        /// <summary>
        /// Returns the greater of two values.
        /// </summary>
        /// <param name="value1">Source value.</param><param name="value2">Source value.</param>
        public static double Max(double value1, double value2)
        {
            return Math.Max(value1, value2);
        }

        /// <summary>
        /// Restricts a value to be within a specified range. Reference page contains links to related code samples.
        /// </summary>
        /// <param name="value">The value to clamp.</param><param name="min">The minimum value. If value is less than min, min will be returned.</param><param name="max">The maximum value. If value is greater than max, max will be returned.</param>
        public static float Clamp(float value, float min, float max)
        {
            value = (value > max) ? max : value;
            value = (value < min) ? min : value;
            return value;
        }

        /// <summary>
        /// Restricts a value to be within a specified range. Reference page contains links to related code samples.
        /// </summary>
        /// <param name="value">The value to clamp.</param><param name="min">The minimum value. If value is less than min, min will be returned.</param><param name="max">The maximum value. If value is greater than max, max will be returned.</param>
        public static double Clamp(double value, double min, double max)
        {
            value = (double)value > (double)max ? max : value;
            value = (double)value < (double)min ? min : value;
            return value;
        }

        /// <summary>
        /// Restricts a value to be within a specified range. Reference page contains links to related code samples.
        /// </summary>
        /// <param name="value">The value to clamp.</param><param name="min">The minimum value. If value is less than min, min will be returned.</param><param name="max">The maximum value. If value is greater than max, max will be returned.</param>
        public static MyFixedPoint Clamp(MyFixedPoint value, MyFixedPoint min, MyFixedPoint max)
        {
            value = value > max ? max : value;
            value = value < min ? min : value;
            return value;
        }

        /// <summary>
        /// Restricts a value to be within a specified range. Reference page contains links to related code samples.
        /// </summary>
        /// <param name="value">The value to clamp.</param><param name="min">The minimum value. If value is less than min, min will be returned.</param><param name="max">The maximum value. If value is greater than max, max will be returned.</param>
        public static int Clamp(int value, int min, int max)
        {
            value = value > max ? max : value;
            value = value < min ? min : value;
            return value;
        }

        /// <summary>
        /// Linearly interpolates between two values.
        /// </summary>
        /// <param name="value1">Source value.</param><param name="value2">Source value.</param><param name="amount">Value between 0 and 1 indicating the weight of value2.</param>
        public static float Lerp(float value1, float value2, float amount)
        {
            return value1 + (value2 - value1) * amount;
        }

        /// <summary>
        /// Linearly interpolates between two values.
        /// </summary>
        /// <param name="value1">Source value.</param><param name="value2">Source value.</param><param name="amount">Value between 0 and 1 indicating the weight of value2.</param>
        public static double Lerp(double value1, double value2, double amount)
        {
            return value1 + (value2 - value1) * amount;
        }

        /// <summary>
        /// Performs interpolation on logarithmic scale.
        /// </summary>
        public static float InterpLog(float value, float amount1, float amount2)
        {
            Debug.Assert(amount1 != 0f);
            Debug.Assert(amount2 != 0f);
            return (float)(Math.Pow((double)amount1, 1.0 - (double)value) * Math.Pow((double)amount2, (double)value));
        }

        public static float InterpLogInv(float value, float amount1, float amount2)
        {
            Debug.Assert(amount1 != 0f);
            Debug.Assert(amount2 != 0f);
            return (float)Math.Log(value / amount1, amount2 / amount1);
        }

        /// <summary>
        /// Returns the Cartesian coordinate for one axis of a point that is defined by a given triangle and two normalized barycentric (areal) coordinates.
        /// </summary>
        /// <param name="value1">The coordinate on one axis of vertex 1 of the defining triangle.</param><param name="value2">The coordinate on the same axis of vertex 2 of the defining triangle.</param><param name="value3">The coordinate on the same axis of vertex 3 of the defining triangle.</param><param name="amount1">The normalized barycentric (areal) coordinate b2, equal to the weighting factor for vertex 2, the coordinate of which is specified in value2.</param><param name="amount2">The normalized barycentric (areal) coordinate b3, equal to the weighting factor for vertex 3, the coordinate of which is specified in value3.</param>
        public static float Barycentric(float value1, float value2, float value3, float amount1, float amount2)
        {
            return (float)((double)value1 + (double)amount1 * ((double)value2 - (double)value1) + (double)amount2 * ((double)value3 - (double)value1));
        }

        /// <summary>
        /// Interpolates between two values using a cubic equation.
        /// </summary>
        /// <param name="value1">Source value.</param><param name="value2">Source value.</param><param name="amount">Weighting value.</param>
        public static float SmoothStep(float value1, float value2, float amount)
        {
            Debug.Assert(amount >= 0f && amount <= 1f, "Wrong amount value for SmoothStep");
            return MathHelper.Lerp(value1, value2, SCurve3(amount));
        }

        /// <summary>
        /// Interpolates between two values using a cubic equation.
        /// </summary>
        /// <param name="value1">Source value.</param><param name="value2">Source value.</param><param name="amount">Weighting value.</param>
        public static double SmoothStep(double value1, double value2, double amount)
        {
            Debug.Assert(amount >= 0f && amount <= 1f, "Wrong amount value for SmoothStep");
            return MathHelper.Lerp(value1, value2, SCurve3(amount));
        }

        /// <summary>
        /// Interpolates between zero and one using cubic equiation, solved by de Casteljau.
        /// </summary>
        /// <param name="amount">Weighting value [0..1].</param>
        public static float SmoothStepStable(float amount)
        {
            Debug.Assert(amount >= 0f && amount <= 1f, "Wrong amount value for SmoothStep");
            float invAmount = 1 - amount;
            // y1 = 0, y2 = 0, y3 = 1, y4 = 1
            // y12 = 0
            float y23 = amount;
            // y34 = 1
            float y123 = /*y12 * invAmount + */ y23 * amount;
            float y234 = y23 * invAmount + /* y34 * */amount;
            float y1234 = y123 * invAmount + y234 * amount;
            return y1234;
        }

        /// <summary>
        /// Interpolates between zero and one using cubic equiation, solved by de Casteljau.
        /// </summary>
        /// <param name="amount">Weighting value [0..1].</param>
        public static double SmoothStepStable(double amount)
        {
            Debug.Assert(amount >= 0f && amount <= 1f, "Wrong amount value for SmoothStep");
            double invAmount = 1 - amount;
            // y1 = 0, y2 = 0, y3 = 1, y4 = 1
            // y12 = 0
            double y23 = amount;
            // y34 = 1
            double y123 = /*y12 * invAmount + */ y23 * amount;
            double y234 = y23 * invAmount + /* y34 * */amount;
            double y1234 = y123 * invAmount + y234 * amount;
            return y1234;
        }

        /// <summary>
        /// Performs a Catmull-Rom interpolation using the specified positions.
        /// </summary>
        /// <param name="value1">The first position in the interpolation.</param><param name="value2">The second position in the interpolation.</param><param name="value3">The third position in the interpolation.</param><param name="value4">The fourth position in the interpolation.</param><param name="amount">Weighting factor.</param>
        public static float CatmullRom(float value1, float value2, float value3, float value4, float amount)
        {
            float num1 = amount * amount;
            float num2 = amount * num1;
            return (float)(0.5 * (2.0 * (double)value2 + (-(double)value1 + (double)value3) * (double)amount + (2.0 * (double)value1 - 5.0 * (double)value2 + 4.0 * (double)value3 - (double)value4) * (double)num1 + (-(double)value1 + 3.0 * (double)value2 - 3.0 * (double)value3 + (double)value4) * (double)num2));
        }

        /// <summary>
        /// Performs a Hermite spline interpolation.
        /// </summary>
        /// <param name="value1">Source position.</param><param name="tangent1">Source tangent.</param><param name="value2">Source position.</param><param name="tangent2">Source tangent.</param><param name="amount">Weighting factor.</param>
        public static float Hermite(float value1, float tangent1, float value2, float tangent2, float amount)
        {
            float num1 = amount;
            float num2 = num1 * num1;
            float num3 = num1 * num2;
            float num4 = (float)(2.0 * (double)num3 - 3.0 * (double)num2 + 1.0);
            float num5 = (float)(-2.0 * (double)num3 + 3.0 * (double)num2);
            float num6 = num3 - 2f * num2 + num1;
            float num7 = num3 - num2;
            return (float)((double)value1 * (double)num4 + (double)value2 * (double)num5 + (double)tangent1 * (double)num6 + (double)tangent2 * (double)num7);
        }

        public static Vector3D CalculateBezierPoint(double t, Vector3D p0, Vector3D p1, Vector3D p2, Vector3D p3)
        {
            double u = 1 - t;
            double tt = t * t;
            double uu = u * u;
            double uuu = uu * u;
            double ttt = tt * t;

            Vector3D p = uuu * p0; //first term
            p += 3 * uu * t * p1; //second term
            p += 3 * u * tt * p2; //third term
            p += ttt * p3; //fourth term

            return p;
        }

        /// <summary>
        /// Reduces a given angle to a value between π and -π.
        /// </summary>
        /// <param name="angle">The angle to reduce, in radians.</param>
        public static float WrapAngle(float angle)
        {
            angle = (float)Math.IEEERemainder((double)angle, 6.28318548202515);
            if ((double)angle <= -3.14159274101257)
                angle += 6.283185f;
            else if ((double)angle > 3.14159274101257)
                angle -= 6.283185f;
            return angle;
        }

        public static int GetNearestBiggerPowerOfTwo(int v)
        {
            --v;
            v |= v >> 1;
            v |= v >> 2;
            v |= v >> 4;
            v |= v >> 8;
            v |= v >> 16;
            ++v;
            return v;
        }

        public static uint GetNearestBiggerPowerOfTwo(uint v)
        {
            --v;
            v |= v >> 1;
            v |= v >> 2;
            v |= v >> 4;
            v |= v >> 8;
            v |= v >> 16;
            ++v;
            return v;
        }

        /// <summary>
        /// Returns nearest bigger power of two
        /// </summary>
        /// <param name="f"></param>
        /// <returns></returns>
        public static int GetNearestBiggerPowerOfTwo(float f)
        {
            int x = 1;
            while (x < f)
            {
                x <<= 1;
            }

            return x;
        }

        public static int GetNearestBiggerPowerOfTwo(double f)
        {
            int x = 1;
            while (x < f)
            {
                x <<= 1;
            }

            return x;
        }

        public static float Max(float a, float b, float c)
        {
            float abMax = a > b ? a : b;

            return abMax > c ? abMax : c;
        }

        public static int Max(int a, int b, int c)
        {
            int abMax = a > b ? a : b;
            return abMax > c ? abMax : c;
        }

        public static float Min(float a, float b, float c)
        {
            float abMin = a < b ? a : b;

            return abMin < c ? abMin : c;
        }

        public static double Max(double a, double b, double c)
        {
            double abMax = a > b ? a : b;

            return abMax > c ? abMax : c;
        }

        public static double Min(double a, double b, double c)
        {
            double abMin = a < b ? a : b;

            return abMin < c ? abMin : c;
        }

#if !XB1
        public static int ComputeHashFromBytes(byte[] bytes)
        {
            int size = bytes.Length;
            size -= (size % 4); // Ignore bytes past the aligned section.
            GCHandle gcHandle = GCHandle.Alloc(bytes, GCHandleType.Pinned);
            int hash = 0;
            unsafe
            {
                try
                {
                    int* numPtr = (int*)gcHandle.AddrOfPinnedObject().ToPointer();
                    for (int i = 0; i < size; i += 4, ++numPtr)
                        hash ^= (*numPtr);
                    return hash;
                }
                finally
                {
                    gcHandle.Free();
                }
            }
        }
#endif // !XB1

        public static float RoundOn2(float x)
        {
            return ((int)(x * 100)) / 100.0f; // Oriznuti staci :)
        }
        /// <summary>
        /// Returns true if value is power of two
        /// </summary>
        /// <param name="x"></param>
        /// <returns></returns>
        public static bool IsPowerOfTwo(int x)
        {
            return ((x > 0) && ((x & (x - 1)) == 0));
        }

        public static float  SCurve3(float  t)
        {
            return t*t*(3f - 2f*t);
        }
        public static double SCurve3(double t)
        {
            return t*t*(3 - 2*t);
        }
        public static float  SCurve5(float  t)
        {
            return t*t*t*(t*(t*6f - 15f) + 10f);
        }
        public static double SCurve5(double t)
        {
            return t*t*t*(t*(t*6 - 15) + 10);
        }

        public static float  Saturate(float  n)
        {
            return (n < 0f) ? 0f : (n > 1f) ? 1f : n;
        }
        public static double Saturate(double n)
        {
            return (n < 0.0) ? 0.0 : (n > 1.0) ? 1.0 : n;
        }

        public static int Floor(float  n)
        {
            return n < 0f ? (int)n - 1 : (int)n;
        }
        public static int Floor(double n)
        {
            return n < 0.0 ? (int)n - 1 : (int)n;
        }

        private static readonly int[] lof2floor_lut = new int[]
        {
             0,  9,  1, 10, 13, 21,  2, 29,
            11, 14, 16, 18, 22, 25,  3, 30,
             8, 12, 20, 28, 15, 17, 24,  7,
            19, 27, 23,  6, 26,  5,  4, 31
        };

        /**
         * Fast integer Floor(Log2(value)).
         * 
         * Uses a DeBruijn-like method to find quickly the MSB.
         * 
         * Algorithm:
         * https://en.wikipedia.org/wiki/De_Bruijn_sequence#Uses
         * 
         * This implementation:
         * http://stackoverflow.com/a/11398748
         */
        public static int Log2Floor(int value)
        {
            value |= value >> 1;
            value |= value >> 2;
            value |= value >> 4;
            value |= value >> 8;
            value |= value >> 16;
            return lof2floor_lut[(uint)(value * 0x07C4ACDD) >> 27];
        }

        /**
         * Based on the above and this discussion:
         * http://stackoverflow.com/questions/3272424/compute-fast-log-base-2-ceiling
         * 
         */
        public static int Log2Ceiling(int value)
        {
            value |= value >> 1;
            value |= value >> 2;
            value |= value >> 4;
            value |= value >> 8;
            value |= value >> 16;
            value = lof2floor_lut[(uint)(value * 0x07C4ACDD) >> 27];
            return (value & (value - 1)) != 0 ? value + 1 : value;
        }

        public static int Log2(int n)
        {
            int r = 0;

            while ((n >>= 1) > 0)
                ++r;

            return r;
        }

        public static int Log2(uint n)
        {
            int r = 0;

            while ((n >>= 1) > 0)
                ++r;

            return r;
        }

        /// <summary>
        /// Returns 2^n
        /// </summary>
        public static int Pow2(int n)
        {
            return 1 << n;
        }

        public static double CubicInterp(double p0, double p1, double p2, double p3, double t)
        {
            double P  = (p3 - p2) - (p0 - p1);
            double Q  = (p0 - p1) - P;
            double t2 = t*t;

            return P*t2*t + Q*t2 + (p2 - p0)*t + p1;
        }

        /// <summary>
        /// Returns angle in range 0..2*PI
        /// </summary>
        /// <param name="angle">in radians</param>
        public static void LimitRadians2PI(ref double angle)
        {
            if (angle > TwoPi)
            {            
                angle = angle % TwoPi;
            }
            else if (angle < 0)
            {
                angle = angle % TwoPi + TwoPi;
            }
        }

        /// <summary>
        /// Returns angle in range 0..2*PI
        /// </summary>
        /// <param name="angle">in radians</param>
        public static void LimitRadians(ref float angle)
        {
            if (angle > TwoPi)
            {
                angle = angle % TwoPi;
            }
            else if (angle < 0)
            {
                angle = angle % TwoPi + TwoPi;
            }
        }

        /// <summary>
        /// Returns angle in range -PI..PI
        /// </summary>
        /// <param name="angle">radians</param>
        public static void LimitRadiansPI(ref double angle)
        {
            if (angle > Pi)
            {
                angle = angle % Pi - Pi;
            }
            else if (angle < -Pi)
            {
                angle = angle % Pi + Pi;
            }
        }

        /// <summary>
        /// Returns angle in range -PI..PI
        /// </summary>
        /// <param name="angle">radians</param>
        public static void LimitRadiansPI(ref float angle)
        {
            if (angle > Pi)
            {
                angle = angle % Pi - Pi;
            }
            else if (angle < Pi)
            {
                angle = angle % Pi + Pi;
            }
        }

        public static Vector3 CalculateVectorOnSphere(Vector3 northPoleDir, float phi, float theta)
        {
            var sinTheta = Math.Sin(theta);
            return Vector3.TransformNormal(new Vector3(
               Math.Cos(phi) * sinTheta,
               Math.Sin(phi) * sinTheta,
               Math.Cos(theta)), Matrix.CreateFromDir(northPoleDir));
        }

        public static float MonotonicCosine(float radians)
        {
            if (radians > 0)
                return 2 - (float)Math.Cos(radians);
            else
                return (float)Math.Cos(radians);
        }

        public static float MonotonicAcos(float cos)
        {
            if (cos > 1)
                return (float)Math.Acos(2 - cos);
            else
                return (float)-Math.Acos(cos);
        }
    }
}
