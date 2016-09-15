using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using VRage.Extensions;
using VRage.Library.Collections;

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

        public static T[] RemoveIndices<T>(this T[] self, List<int> indices)
        {
            if (indices.Count >= self.Length)
                return new T[0];

            if (indices.Count == 0)
                return self;

            T[] better = new T[self.Length - indices.Count];
            int offset = 0;
            for (int i = 0; i < self.Length - indices.Count; i++)
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
                return value.CompareTo(self[0]) >= 0 ? 1 : 0;
            }

            int mid;
            int start = 0, end = self.Length;

            while (end - start > 1)
            {
                mid = (start + end)/2;

                if (value.CompareTo(self[mid]) >= 0)
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
            if (value.CompareTo(self[start]) >= 0)
            {
                ret = end;
            }

            return ret;
        }

        public static MyRangeIterator<T>.Enumerable Range<T>(this T[] array, int start, int end)
        {
            return MyRangeIterator<T>.ForRange(array, start, end);
        }

        /// <summary>
        /// OfType on array implemented without allocations
        /// </summary>
        public static ArrayOfTypeEnumerator<TBase, ArrayEnumerator<TBase>, T> OfTypeFast<TBase, T>(this TBase[] array)
            where T : TBase
        {
            return new ArrayOfTypeEnumerator<TBase, ArrayEnumerator<TBase>, T>(new ArrayEnumerator<TBase>(array));
        }

        // Copyright (c) 2008-2013 Hafthor Stefansson, MIT/X11 software license
        public static unsafe bool Compare(this byte[] a1, byte[] a2)
        {
            if (a1 == null || a2 == null || a1.Length != a2.Length)
                return false;
            fixed (byte* p1 = a1, p2 = a2)
            {
                byte* x1 = p1, x2 = p2;
                int l = a1.Length;
                for (int i = 0; i < l / 8; i++, x1 += 8, x2 += 8)
                    if (*((long*)x1) != *((long*)x2)) return false;
                if ((l & 4) != 0) { if (*((int*)x1) != *((int*)x2)) return false; x1 += 4; x2 += 4; }
                if ((l & 2) != 0) { if (*((short*)x1) != *((short*)x2)) return false; x1 += 2; x2 += 2; }
                if ((l & 1) != 0) if (*((byte*)x1) != *((byte*)x2)) return false;
                return true;
            }
        }
    }
}
