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

        public static T[] SubtractIndices<T>(this T[] self, List<int> indices)
        {
            if (indices.Count >= self.Length)
                return new T[0];

            if (indices.Count == 0)
                return self;

            T[] better = new T[self.Length - indices.Count];
            int offset = 0;
            for (int i = indices[offset]; i < self.Length - indices.Count; i++)
            {
                while (offset < indices.Count && i == indices[offset] - offset)
                    offset++;
                better[i] = self[i + offset];
            }

            return better;
        }

        /**
         * Do a binary search in an array of interval limits, each member is the interval threshold.
         * 
         * The result is the index of the interval that contains the value searched for.
         * 
         * If the interval array is empty 0 is returned (as we assume we have only the (-∞,+∞) interval).
         * 
         * Return range: [0, Length]
         */
        public static int BinaryIntervalSearch<T>(this T[] self, T value) where T : IComparable<T>
        {
            if (self.Length == 0) return 0;
            if (self.Length == 1)
            {
                return value.CompareTo(self[0]) > 0 ? 1 : 0;
            }

            int mid;
            int start = 0, end = self.Length;

            while (end - start > 1)
            {
                mid = (start + end)/2;

                if (value.CompareTo(self[mid]) > 0)
                {
                    start = mid;
                }
                else
                {
                    end = mid;
                }
            }

            int ret = start;

            // end of array;
            if (value.CompareTo(self[start]) > 0)
            {
                ret = end;
            }

            return ret;
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
