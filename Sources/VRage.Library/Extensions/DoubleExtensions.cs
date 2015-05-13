using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace System
{
    public static class DoubleExtensions
    {
        /// <summary>
        /// Returns true if double is valid
        /// </summary>
        public static bool IsValid(this double f)
        {
            return !double.IsNaN(f) && !double.IsInfinity(f);
        }

        [Conditional("DEBUG")]
        public static void AssertIsValid(this double f)
        {
            Debug.Assert(f.IsValid());
        }
    }
}
