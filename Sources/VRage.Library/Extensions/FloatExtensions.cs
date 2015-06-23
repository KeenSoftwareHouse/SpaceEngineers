using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace System
{
    public static class FloatExtensions
    {
        /// <summary>
        /// Returns true if float is not NaN or infinity.
        /// </summary>
        public static bool IsValid(this float f)
        {
            return !float.IsNaN(f) && !float.IsInfinity(f);
        }

        /// <summary>
        /// Used to check if a value deserialized by ObjectBuilders are initialized or left with the default value (NaN).
        /// 
        /// Used in Block definitions to make things a bit less hairy, and to standardize what a non-initialized value is.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static bool IsUninitialized(this float value)
        {
            return float.IsNaN(value);
        }

        /// <summary>
        /// If value is NaN, return defaultValue.
        /// 
        /// Used in Block definitions to make things a bit less hairy, and to standardize what a non-initialized value is.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        public static float GetOrDefault(this float value, float defaultValue)
        {
            return value.IsUninitialized() ? defaultValue : value;
        }

        [Conditional("DEBUG")]
        public static void AssertIsValid(this float f)
        {
            Debug.Assert(f.IsValid());
        }

        public static bool IsEqual(this float f, float other, float epsilon = 0.0001f)
        {
            return IsZero(f - other, epsilon);
        }

        public static bool IsZero(this float f, float epsilon = 0.0001f)
        {
            return Math.Abs(f) < epsilon;
        }
    }
}
