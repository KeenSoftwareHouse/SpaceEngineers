using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.Generics;
using VRage.Library.Utils;
using VRage.Utils;
using VRageMath;

namespace VRageRender
{             
    internal static class X
    {
        internal static MyStringId TEXT_(string str)
        {
            return MyStringId.GetOrCompute(str);
        }

    }

    public static class MyVector3Extensions
    {
        public static Vector3 Round(this Vector3 v)
        {
            return new Vector3(
                (float) Math.Round(v.X),
                (float) Math.Round(v.Y),
                (float) Math.Round(v.Z)
                );
        }

        public static Vector3D Round(this Vector3D v)
        {
            return new Vector3D(
                Math.Round(v.X),
                Math.Round(v.Y),
                Math.Round(v.Z)
                );
        }

        public static Vector3 HsvToRgb(this Vector3 hsv)
        {
            float hue = hsv.X * 360;
            float saturation = hsv.Y;
            float value = hsv.Z;

            int hi = Convert.ToInt32(Math.Floor(hue / 60)) % 6;
            float f = (float)(hue / 60 - Math.Floor(hue / 60));

            float v = value;
            float p = value * (1 - saturation);
            float q = value * (1 - f * saturation);
            float t = value * (1 - (1 - f) * saturation);

            if (hi == 0)
                return new Vector3(v, t, p);
            else if (hi == 1)
                return new Vector3(q, v, p);
            else if (hi == 2)
                return new Vector3(p, v, t);
            else if (hi == 3)
                return new Vector3(p, q, v);
            else if (hi == 4)
                return new Vector3(t, p, v);
            else
                return new Vector3(v, p, q);
        }
    }

    static class MyVector3Helpers
    {
        internal static Vector3 Snap(this Vector3 vec, float res)
        {
            var tmp = vec / res;
            return new Vector3((float)Math.Floor(tmp.X), (float)Math.Floor(tmp.Y), (float)Math.Floor(tmp.Z)) * res;
        }

        internal static Vector3I AsCoord(this Vector3 vec, float res)
        {
            var tmp = vec / res;
            return new Vector3I((int)Math.Floor(tmp.X), (int)Math.Floor(tmp.Y), (int)Math.Floor(tmp.Z));
        }
    }

    static class MyVector3HDelpers
    {
        internal static Vector3D Snap(this Vector3D vec, float res)
        {
            var tmp = vec / res;
            return new Vector3D(Math.Floor(tmp.X), Math.Floor(tmp.Y), Math.Floor(tmp.Z)) * res;
        }

        internal static Vector3I AsCoord(this Vector3D vec, float res)
        {
            var tmp = vec / res;
            return new Vector3I((int)Math.Floor(tmp.X), (int)Math.Floor(tmp.Y), (int)Math.Floor(tmp.Z));
        }
    }

    public static class MyVector4Helpers
    {
        public static Vector4 Create(Vector3 v, float x)
        {
            return new Vector4(v.X, v.Y, v.Z, x);
        }
    }

    public static class MyMatrixHelpers
    {
        public static Matrix ClipspaceToTexture
        {
            get
            {
                return new Matrix(
                  0.5f, 0.0f, 0.0f, 0.0f,
                  0.0f, -0.5f, 0.0f, 0.0f,
                  0.0f, 0.0f, 1.0f, 0.0f,
                  0.5f, 0.5f, 0.0f, 1.0f);
            }
        }
    }

    class MyHashHelper
    {
        public static int Combine(int h0, int h1)
        {
            unchecked { 
                int hash = 17;
                hash = hash * 31 + h0;
                hash = hash * 31 + h1;
                return hash;
            }
        }
        public static void Combine(ref int h0, int h1)
        {
            h0 = Combine(h0, h1);
        }
    }

    public static class MyArrayHelpers
    {
        public static void ResizeNoCopy<T>(ref T[] array, int newSize)
        {
            if(array == null || array.Length != newSize)
            {
                array = new T[newSize];
            }
        }

        public static void Reserve<T>(ref T[] array, int size, int threshold = 1024, float allocScale = 1.5f)
        {
            if(array.Length < size)
            {
                var newSize = size == 0 ? 1 : size;
                Array.Resize(ref array, newSize < threshold ? newSize * 2 : (int)(newSize * allocScale));
            }
        }

        public static void ReserveNoCopy<T>(ref T[] array, int size, int threshold = 1024, float allocScale = 1.5f)
        {
            if (array.Length < size)
            {
                var newSize = size == 0 ? 1 : size;
                array = new T[newSize < threshold ? newSize * 2 : (int)(newSize * allocScale)];
            }
        }

        public static void InitOrReserve<T>(ref T[] array, int size, int threshold = 1024, float allocScale = 1.5f)
        {
            if (array == null)
                array = new T[size];
            else
                Reserve(ref array, size, threshold, allocScale);
        }

        public static void InitOrReserveNoCopy<T>(ref T[] array, int size, int threshold = 1024, float allocScale = 1.5f)
        {
            if (array == null)
                array = new T[size];
            else
                ReserveNoCopy(ref array, size, threshold, allocScale);
        }
    }

    public static class MyDictionaryExtensions
    {
        public static TValue Get<TKey, TValue>(this Dictionary<TKey, TValue> dictionary, TKey key, TValue def = default(TValue))
        {
            TValue value;
            if (dictionary.TryGetValue(key, out value))
            {
                return value;
            }
            return def;
        }
#if !XB1
        public static TValue Get<TKey, TValue>(this ConcurrentDictionary<TKey, TValue> dictionary, TKey key, TValue def = default(TValue))
        {
            TValue value;
            if (dictionary.TryGetValue(key, out value))
            {
                return value;
            }
            return def;
        }
#endif

        public static TValue SetDefault<TKey, TValue>(this Dictionary<TKey, TValue> dictionary, TKey key, TValue def = default(TValue))
        {
            TValue value;
            if (dictionary.TryGetValue(key, out value))
            {
                return value;
            }

            return dictionary[key] = value = def;
        }

        public static TValue Get<TKey, TValue>(this SortedDictionary<TKey, TValue> dictionary, TKey key, TValue def = default(TValue))
        {
            TValue value;
            if (dictionary.TryGetValue(key, out value))
            {
                return value;
            }
            return def;
        }

        public static TValue SetDefault<TKey, TValue>(this SortedDictionary<TKey, TValue> dictionary, TKey key, TValue def = default(TValue))
        {
            TValue value;
            if (dictionary.TryGetValue(key, out value))
            {
                return value;
            }

            return dictionary[key] = value = def;
        }
    }

    public static class IEnumerableExtensions
    {
        /// <summary>
        /// Wraps this object instance into an IEnumerable&lt;T&gt;
        /// consisting of a single item.
        /// </summary>
        /// <typeparam name="T"> Type of the object. </typeparam>
        /// <param name="item"> The instance that will be wrapped. </param>
        /// <returns> An IEnumerable&lt;T&gt; consisting of a single item. </returns>
        public static IEnumerable<T> Yield_<T>(this T item)
        {
            yield return item;
        }
    }

}
