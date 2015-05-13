using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using VRage.Extensions;

namespace System
{
    public static class ArrayExtensions
    {
        public static bool IsValidIndex<T>(this T[] self, int index)
        {
            // Only one unsigned comparison instead of signed comparison against 0 and length.
            return ((uint)index < (uint)self.Length);
        }

        public static bool IsNullOrEmpty<T>(this T[] self)
        {
            return self == null || self.Length == 0;
        }

        public static bool TryGetValue<T>(this T[] self, int index, out T value)
        {
            if ((uint)index < (uint)self.Length)
            {
                value = self[index];
                return true;
            }
            else
            {
                value = default(T);
                return false;
            }
        }

        /// <summary>
        /// OfType on array implemented without allocations
        /// </summary>
        public static ArrayOfTypeEnumerator<TBase, ArrayEnumerator<TBase>, T> OfTypeFast<TBase, T>(this TBase[] array)
            where T : TBase
        {
            return new ArrayOfTypeEnumerator<TBase, ArrayEnumerator<TBase>, T>(new ArrayEnumerator<TBase>(array));
        }
    }
}
