using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using VRage.Compiler;

namespace VRage.Library.Utils
{
    public static class MyEnum<T>
        where T : struct, IComparable, IFormattable, IConvertible
    {
        // Intentionaly here as inner struct, when not used, max value is not calculated
        public struct MaxValue
        {
            public static readonly T Value = FindMaxValue();
        }

        public static string Name { get { return TypeNameHelper<T>.Name; } }
        public static readonly T[] Values = (T[])Enum.GetValues(typeof(T));
        public static readonly Type UnderlyingType = typeof(T).UnderlyingSystemType;

        /// <summary>
        /// Cached strings to avoid ToString() calls. These values are not readable in obfuscated builds!
        /// </summary>
        private static readonly Dictionary<int, string> m_names = new Dictionary<int, string>();

        static T FindMaxValue()
        {
            var values = MyEnum<T>.Values;
            var comparer = Comparer<T>.Default;

            if (values.Length > 0)
            {
                T max = values[0];
                for (int i = 1; i < values.Length; i++)
                {
                    if (comparer.Compare(max, values[i]) < 0)
                    {
                        max = values[i];
                    }
                }
                return max;
            }
            else
            {
                return default(T);
            }
        }

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

    }
}
