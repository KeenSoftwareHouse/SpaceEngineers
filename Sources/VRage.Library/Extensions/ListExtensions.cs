using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Reflection.Emit;
using System.Linq;
using VRage.Library.Collections;

namespace System.Collections.Generic
{

    // TODO: OP! Create one generic IL, per-type is not necessary
    static class ListInternalAccessor<T>
    {
#if !XB1 // XB1_LISTEXTENSIONS_NOEMIT - this used to be #if !UNSHARPER
		public static Func<List<T>, T[]> GetArray;
		public static Action<List<T>, int> SetSize;
#endif

        static ListInternalAccessor()
        {
#if XB1 // XB1_LISTEXTENSIONS_NOEMIT - this used to be #if UNSHARPER

#else
                var dm = new DynamicMethod("get", MethodAttributes.Static | MethodAttributes.Public, CallingConventions.Standard, typeof(T[]), new Type[] { typeof(List<T>) }, typeof(ListInternalAccessor<T>), true);
                var il = dm.GetILGenerator();
                il.Emit(OpCodes.Ldarg_0); // Load List<T> argument
                il.Emit(OpCodes.Ldfld, typeof(List<T>).GetField("_items", BindingFlags.NonPublic | BindingFlags.Instance)); // Replace argument by field
                il.Emit(OpCodes.Ret); // Return field
                GetArray = (Func<List<T>, T[]>)dm.CreateDelegate(typeof(Func<List<T>, T[]>));

                var dm2 = new DynamicMethod("set", MethodAttributes.Static | MethodAttributes.Public, CallingConventions.Standard, null, new Type[] { typeof(List<T>), typeof(int) }, typeof(ListInternalAccessor<T>), true);
                var il2 = dm2.GetILGenerator();
                il2.Emit(OpCodes.Ldarg_0); // Load List<T> argument
                il2.Emit(OpCodes.Ldarg_1); // Load new value on stack
                il2.Emit(OpCodes.Stfld, typeof(List<T>).GetField("_size", BindingFlags.NonPublic | BindingFlags.Instance)); // Store new value into field

                // Increment version
                var versionField = typeof(List<T>).GetField("_version", BindingFlags.NonPublic | BindingFlags.Instance);
                il2.Emit(OpCodes.Ldarg_0); // Load List<T> argument
                il2.Emit(OpCodes.Dup); // Duplicate
                il2.Emit(OpCodes.Ldfld, versionField); // Replace second List<T> by field value
                il2.Emit(OpCodes.Ldc_I4_1); // Load 1 onto stack
                il2.Emit(OpCodes.Add); // Replace version value and 1 with it's sum
                il2.Emit(OpCodes.Stfld, versionField); // Load first List<T> and sum, write new sum

                il2.Emit(OpCodes.Ret);
                SetSize = (Action<List<T>, int>)dm2.CreateDelegate(typeof(Action<List<T>, int>));
#endif
        }
    }


    public struct ClearToken<T> : IDisposable
    {
        public List<T> List;

        public void Dispose()
        {
            Debug.Assert(List != null, "List cannot be null");
            List.Clear();
        }
    }

    public static class ListExtensions
    {

        public static ClearToken<T> GetClearToken<T>(this List<T> list)
        {
            return new ClearToken<T>() { List = list };
        }

        /// <summary>
        /// Remove element at index by replacing it with last element in list.
        /// Removing is very fast but it breaks order of items in list!
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list">The list.</param>
        /// <param name="index">The index.</param>
        public static void RemoveAtFast<T>(this IList<T> list, int index)
        {
            int lastPos = list.Count - 1;

            list[index] = list[lastPos];
            list.RemoveAt(lastPos);
        }

#if UNSHARPER_TMP
        public static T[] GetInternalArray<T>(this List<T> list)
        {
            //not the same thing but will work for now.
            return list.ToArray();
        }
#else
#if XB1 // XB1_LISTEXTENSIONS_NOEMIT
        public static T[] GetInternalArray<T>(this List<T> list)
        {
            var field = list.GetType().GetField("_items",
                System.Reflection.BindingFlags.Instance |
                System.Reflection.BindingFlags.NonPublic);
            return (T[])field.GetValue(list);
        }
#else // !XB1
        public static T[] GetInternalArray<T>(this List<T> list)
        {
            return ListInternalAccessor<T>.GetArray(list);
        }
#endif // !XB1
#endif

        public static void AddOrInsert<T>(this List<T> list, T item, int index)
        {
            if (index < 0 || index > list.Count)
                list.Add(item);
            else
                list.Insert(index, item);
        }

        public static void AddArray<T>(this List<T> list, T[] itemsToAdd)
        {
#if UNSHARPER_TMP
            list.AddRange(itemsToAdd);
#else
            AddArray(list, itemsToAdd, itemsToAdd.Length);
#endif
        }

#if !UNSHARPER
        public static void AddArray<T>(this List<T> list, T[] itemsToAdd, int itemCount)
        {
            if (list.Capacity < list.Count + itemCount)
            {
                list.Capacity = list.Count + itemCount;
            }

            Array.Copy(itemsToAdd, 0, list.GetInternalArray(), list.Count, itemCount);
#if XB1 // XB1_LISTEXTENSIONS_NOEMIT
            var sizeField = list.GetType().GetField("_size",
                System.Reflection.BindingFlags.Instance |
                System.Reflection.BindingFlags.NonPublic);
            sizeField.SetValue(list, list.Count + itemCount);
            var versionField = list.GetType().GetField("_version",
                System.Reflection.BindingFlags.Instance |
                System.Reflection.BindingFlags.NonPublic);
            versionField.SetValue(list, (int)versionField.GetValue(list) + 1);
#else // !XB1
            ListInternalAccessor<T>.SetSize(list, list.Count + itemCount);
#endif // !XB1
        }
#endif


#if UNSHARPER_TMP
        public static void Resize<T>(this List<T> list, int sz, T c)
        {
            int cur = list.Count;
            if (sz < cur)
                list.RemoveRange(sz, cur - sz);
            else if (sz > cur)
            {
                if (sz > list.Capacity)//this bit is purely an optimisation, to avoid multiple automatic capacity changes.
                    list.Capacity = sz;
                list.AddRange(Enumerable.Repeat(c, sz - cur));
            }
        }
        public static void SetSize<T>(this List<T> list, int sz) where T : new()
        {
            if (sz == 0)
            {
                list.Clear();
            }
            else
            {
                System.Diagnostics.Debug.Assert(false);
            }
            //			Resize(list, sz, default(T));
        }
#else
		public static void SetSize<T>(this List<T> list, int newSize)
        {
#if XB1 // XB1_LISTEXTENSIONS_NOEMIT
            var sizeField = list.GetType().GetField("_size",
                System.Reflection.BindingFlags.Instance |
                System.Reflection.BindingFlags.NonPublic);
            sizeField.SetValue(list, newSize);
            var versionField = list.GetType().GetField("_version",
                System.Reflection.BindingFlags.Instance |
                System.Reflection.BindingFlags.NonPublic);
            versionField.SetValue(list, (int)versionField.GetValue(list) + 1);
#else // !XB1
            ListInternalAccessor<T>.SetSize(list, newSize);
#endif // !XB1
        }
#endif
        public static void AddList<T>(this List<T> list, List<T> itemsToAdd)
        {
#if UNSHARPER_TMP
            list.AddRange(itemsToAdd);
#else
            AddArray(list, itemsToAdd.GetInternalArray(), itemsToAdd.Count);
#endif
        }

        public static void AddHashset<T>(this List<T> list, HashSet<T> hashset)
        {
            foreach (var item in hashset)
                list.Add(item);
        }

        public static void AddHashsetCasting<T1, T2>(this List<T1> list, HashSet<T2> hashset)
        {
            foreach (var item in hashset)
                list.Add((T1)(object)item);
        }

        /// <summary>
        /// Moves item in the list from original index to target index, reordering elements as if Remove and Insert was called.
        /// However, only elements in the range between the two indices are affected.
        /// </summary>
        public static void Move<T>(this List<T> list, int originalIndex, int targetIndex)
        {
            int step = Math.Sign(targetIndex - originalIndex);
            if (step == 0)
                return;

            T tmp = list[originalIndex];
            for (int i = originalIndex; i != targetIndex; i += step)
                list[i] = list[i + step];
            list[targetIndex] = tmp;
        }

        public static bool IsValidIndex<T>(this List<T> list, int index)
        {
            return 0 <= index && index < list.Count;
        }

        /**
         * Remove each element in indices from the list.
         * 
         * The list of indices must be sorted.
         */
        public static void RemoveIndices<T>(this List<T> list, List<int> indices)
        {
            if (indices.Count == 0) return;

            int offset = 0;
            for (int i = indices[offset]; i < list.Count - indices.Count; i++)
            {
                while (offset < indices.Count && i == indices[offset] - offset)
                    offset++;
                list[i] = list[i + offset];
            }

            list.RemoveRange(list.Count - indices.Count, indices.Count);
        }

        public static void Swap<T>(this List<T> list, int a, int b)
        {
            T x = list[a];
            list[a] = list[b];
            list[b] = x;
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
        public static int BinaryIntervalSearch<T>(this IList<T> self, T value) where T : IComparable<T>
        {
            if (self.Count == 0) return 0;
            if (self.Count == 1)
            {
                return value.CompareTo(self[0]) >= 0 ? 1 : 0;
            }

            int mid;
            int start = 0, end = self.Count;

            while (end - start > 1)
            {
                mid = (start + end) / 2;

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

        public static MyRangeIterator<T>.Enumerable Range<T>(this List<T> array, int start, int end)
        {
            return MyRangeIterator<T>.ForRange(array, start, end);
        }

        public static void InsertInOrder<T>(this List<T> self, T value, IComparer<T> comparer)
        {
            int index = self.BinarySearch(value, comparer);
            if (index < 0)
            {
                index = ~index;
            }
            self.Insert(index, value);
        }

        public static void InsertInOrder<T>(this List<T> self, T value) where T : IComparable<T>
        {
            self.InsertInOrder(value, Comparer<T>.Default);
        }

        public static bool IsSorted<T>(this List<T> self, IComparer<T> comparer)
        {
            for (int i = 1; i < self.Count; ++i)
            {
                if (comparer.Compare(self[i - 1], self[i]) > 0) return false;
            }
            return true;
        }

    }
}
