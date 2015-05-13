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
