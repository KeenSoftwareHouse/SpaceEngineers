using System;
using System.IO;
using System.Text;

namespace VRage.Library.Utils
{
    public static class MyHashRandomUtils
    {
        /// <summary>
        /// Create a [0, 1) float from it's mantissa.
        /// </summary>
        /// <param name="m">Mantissa bits.</param>
        /// <returns></returns>
        public static unsafe float CreateFloatFromMantissa(uint m)
        {
            const uint ieeeMantissa = 0x007FFFFFu; // binary32 mantissa bitmask
            const uint ieeeOne = 0x3F800000u;      // 1.0 in IEEE binary32

            m &= ieeeMantissa;                     // Keep only mantissa bits (fractional part)
            m |= ieeeOne;                          // Add fractional part to 1.0

            float f = * (float *) &m;       // Range [1:2]
            return f - 1.0f;                // Range [0:1]
        }

        public static uint JenkinsHash(uint x)
        {
            x += ( x << 10 );
            x ^= ( x >>  6 );
            x += ( x <<  3 );
            x ^= ( x >> 11 );
            x += ( x << 15 );
            return x;
        }

        /// <summary>
        /// Compute a float in the range [0, 1) created from the the seed.
        /// 
        /// For uniformly distributed seeds this method will produce uniformly distributed values.
        /// </summary>
        /// <param name="seed">Any integer to be used as a seed. The seed needs not be super uniform since it will be hashed.</param>
        /// <returns>A float in the range [0, 1)</returns>
        public static float UniformFloatFromSeed(int seed)
        {
            return CreateFloatFromMantissa(JenkinsHash((uint)seed));
        }

        public static void TestHashSample()
        {
            const int SIZE = 100 * 1000 * 1000;
            float[] samples = new float[SIZE];
            using (new MySimpleTestTimer("Int to sample fast"))
            {
                for (int i = 0; i < SIZE; ++i)
                {
                    samples[i] = UniformFloatFromSeed(i);
                }
            }

            float avg = 0, min = float.MaxValue, max = float.MinValue;
            for (int i = 0; i < SIZE; ++i)
            {
                avg += samples[i];
                if (min > samples[i]) min = samples[i];
                if (max < samples[i]) max = samples[i];
            }

            avg /= SIZE;

            float stddev = 0;
            for (int i = 0; i < SIZE; ++i)
            {
                var dev = samples[i] - avg;
                stddev += dev * dev;
            }

            stddev = (float)Math.Sqrt(stddev) / SIZE;

            StringBuilder results = new StringBuilder();
            results.AppendFormat("Min/Max/Avg: {0}/{1}/{2}\n", min, max, avg);
            results.AppendFormat("Std dev: {0}\n", stddev);

#if !XB1
            File.AppendAllText(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), "perf.log"),
                results.ToString());
#else // XB1
            System.Diagnostics.Debug.Assert(false, "XB1 TODO?");
#endif // XB1
        }
    }
}