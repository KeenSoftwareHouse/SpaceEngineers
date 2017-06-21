using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using VRage.Compiler;

namespace VRage.Library.Utils
{
    public enum MyGameModeEnum
    {
        Creative,
        Survival,
    }


#if UNSHARPER
	public struct MyEnum_Range<T>
	where T : struct, IComparable, IFormattable, IConvertible
    {
        public static readonly T Min;
        public static readonly T Max;

		static MyEnum_Range()
        {
            var values = MyEnum<T>.Values;
            var comparer = Comparer<T>.Default;

            if (values.Length > 0)
            {
                Max = values[0];
                Min = values[0];
                for (int i = 1; i < values.Length; i++)
                {
                    var v = values[i];
                    if (comparer.Compare(Max, v) < 0)
                    {
                        Max = v;
                    }
                    if (comparer.Compare(Min, v) > 0)
                    {
                        Min = v;
                    }
                }
            }
        }
    }
#endif

    public static class MyEnum<T>
        where T : struct, IComparable, IFormattable, IConvertible
    {
#if !UNSHARPER
        // Intentionaly here as inner struct, when not used, max value is not calculated
        public struct Range
        {
            public static readonly T Min;
            public static readonly T Max;

            static Range()
            {
                var values = MyEnum<T>.Values;
                var comparer = Comparer<T>.Default;

                if (values.Length > 0)
                {
                    Max = values[0];
                    Min = values[0];
                    for (int i = 1; i < values.Length; i++)
                    {
                        var v = values[i];
                        if (comparer.Compare(Max, v) < 0)
                        {
                            Max = v;
                        }
                        if (comparer.Compare(Min, v) > 0)
                        {
                            Min = v;
                        }
                    }
                }
            }
        }
#endif


        public static string Name { get { return TypeNameHelper<T>.Name; } }
        public static readonly T[] Values = (T[])Enum.GetValues(typeof(T));
        public static readonly Type UnderlyingType = typeof(T).UnderlyingSystemType;

        /// <summary>
        /// Cached strings to avoid ToString() calls. These values are not readable in obfuscated builds!
        /// </summary>
        private static readonly Dictionary<int, string> m_names = new Dictionary<int, string>();

        public static string GetName(T value)
        {
            var idx = Array.IndexOf(Values, value);
            Debug.Assert(idx != -1);
            string result;
            if (!m_names.TryGetValue(idx, out result))
            {
                result = value.ToString();
                m_names[idx] = result;
            }

            return result;
        }

        public static unsafe ulong GetValue(T value)
        {
            ulong val = 0;
            SharpDX.Utilities.Write((IntPtr)(void*)&val, ref value);
            return val;
        }

        public static unsafe void SetValue(ref T loc, ulong value)
        {
            SharpDX.Utilities.Read((IntPtr)(void*)&value, ref loc);
        }

        public static unsafe T SetValue(ulong value)
        {
            T result = default(T);
            SharpDX.Utilities.Read((IntPtr)(void*)&value, ref result);
            return result;
        }
    }
}
