using System;
using System.Diagnostics;
using VRageMath;

namespace VRage.Sync
{
    public static class SyncExtensions
    {
        // Now done by default
        //public static void CompareSet<T>(this Sync<T> sync, T value)
        //    where T : struct, IEquatable<T>
        //{
        //    if (!sync.Value.Equals(value))
        //        sync.Value = value;
        //}

        /// <summary>
        /// Sets validation handler to always return false.
        /// </summary>
        public static void ValidateNever<T>(this Sync<T> sync)
        {
            sync.Validate = (value) => false;
        }

#if !XB1
        /// <summary>
        /// Sets validate handler to validate that value is in range.
        /// </summary>
        public static void ValidateRange(this Sync<float> sync, float inclusiveMin, float inclusiveMax)
        {
            Debug.Assert(sync.Validate == null, "Validate handler already set");
            sync.Validate = (value) => value >= inclusiveMin && value <= inclusiveMax;
        }

        /// <summary>
        /// Sets validate handler to validate that value is in range.
        /// </summary>
        public static void ValidateRange(this Sync<float> sync, Func<float> inclusiveMin, Func<float> inclusiveMax)
        {
            Debug.Assert(sync.Validate == null, "Validate handler already set");
            sync.Validate = (value) => value >= inclusiveMin() && value <= inclusiveMax();
        }

        /// <summary>
        /// Sets validate handler to validate that value is withing bounds.
        /// </summary>
        public static void ValidateRange(this Sync<float> sync, Func<MyBounds> bounds)
        {
            Debug.Assert(sync.Validate == null, "Validate handler already set");
            sync.Validate = (value) =>
                {
                    var b = bounds();
                    return value >= b.Min && value <= b.Max;
                };
        }
#endif // !XB1
    }
}
