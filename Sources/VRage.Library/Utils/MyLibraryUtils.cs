using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace VRage.Library.Utils
{
    public class MyLibraryUtils
    {
#if !XB1
        [Conditional("DEBUG")]
        [DebuggerStepThrough]
        public static void AssertBlittable<T>()
        {
            try
            {
                // Non-blittable types cannot allocate pinned handler
                if (default(T) != null) // This will rule out classes without constraint
                {
                    var handle = GCHandle.Alloc(default(T), GCHandleType.Pinned);
                    handle.Free();
                }
            }
            catch { }
            Debug.Fail("Type '" + typeof(T).Name + "' is not blittable");
        }

        public static void ThrowNonBlittable<T>()
        {
            try
            {
                // class test
                if (default(T) == null)
                {
                    throw new InvalidOperationException("Class is never blittable");
                }
                else
                {
                    // Non-blittable types cannot allocate pinned handler
                    var handle = GCHandle.Alloc(default(T), GCHandleType.Pinned);
                    handle.Free();
                }

            }
            catch (Exception e)
            {
                throw new InvalidOperationException("Type '" + typeof(T) + "' is not blittable", e);
            }
        }
#endif // !XB1

        /// <summary>
        /// Normalizes uniform-spaced float within min/max into uint with specified number of bits.
        /// This does not preserve 0 when min = -max
        /// </summary>
        public static uint NormalizeFloat(float value, float min, float max, int bits)
        {
            int num = (1 << bits) - 1; // 255 for 8 bits
            value = (value - min) / (max - min); // Scale to 0 ~ 1
            return (uint)(value * num + 0.5f); // Scale to 0 ~ 255 and round
        }

        /// <summary>
        /// Denormalizes uint with specified number of bits into uniform-space float within min/max.
        /// This does not preserve 0 when min = -max
        /// </summary>
        public static float DenormalizeFloat(uint value, float min, float max, int bits)
        {
            int num = (1 << bits) - 1; // 255 for 8 bits
            float result = value / (float)num; // Scale to 0 ~ 1
            return min + result * (max - min); // Scale to min ~ max
        }

        /// <summary>
        /// Normalizes uniform-spaced float within min/max into uint with specified number of bits.
        /// This preserves 0 when min = -max
        /// </summary>
        public static uint NormalizeFloatCenter(float value, float min, float max, int bits)
        {
            int num = (1 << bits) - 2; // 254 for 8 bits
            value = (value - min) / (max - min); // Scale to 0 ~ 1
            return (uint)(value * num + 0.5f); // Scale to 0 ~ 254 and round
        }

        /// <summary>
        /// Denormalizes uint with specified number of bits into uniform-space float within min/max.
        /// This preserves 0 when min = -max
        /// </summary>
        public static float DenormalizeFloatCenter(uint value, float min, float max, int bits)
        {
            int num = (1 << bits) - 2; // 254 for 8 bits
            float result = value / (float)num; // Scale to 0 ~ 1
            return min + result * (max - min); // Scale to min ~ max
        }

        public static int GetDivisionCeil(int num, int div)
        {
            return (num - 1) / div + 1;
        }
    }
}
