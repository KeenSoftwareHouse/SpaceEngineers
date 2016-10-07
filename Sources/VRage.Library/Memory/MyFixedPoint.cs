using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;
#if XB1 // XB1_SYNC_SERIALIZER_NOEMIT
using System.Reflection;
using VRage.Reflection;
#endif // XB1

namespace VRage
{
    /// <summary>
    /// Fixed point number represented as 64-bit integer with 6 decimal places (one millionts)
    /// </summary>
    [ProtoContract]
#if !XB1 // XB1_SYNC_SERIALIZER_NOEMIT
    public struct MyFixedPoint : IXmlSerializable
#else // XB1
    public struct MyFixedPoint : IXmlSerializable, IMySetGetMemberDataHelper
#endif // XB1
    {
        const int Places = 6;
        const int Divider = 1000000;
        static readonly string FormatSpecifier = "D" + (Places + 1);
        static readonly char[] TrimChars = new char[] { '0' };

        public static readonly MyFixedPoint MinValue = new MyFixedPoint(long.MinValue);
        public static readonly MyFixedPoint MaxValue = new MyFixedPoint(long.MaxValue);
        public static readonly MyFixedPoint Zero = new MyFixedPoint(0L);
        public static readonly MyFixedPoint SmallestPossibleValue = new MyFixedPoint(1);
        public static readonly MyFixedPoint MaxIntValue = (MyFixedPoint)int.MaxValue;
        public static readonly MyFixedPoint MinIntValue = (MyFixedPoint)int.MinValue;

        [ProtoMember]
        public long RawValue;

        private MyFixedPoint(long rawValue)
        {
            RawValue = rawValue;
        }

        /// <summary>
        /// For XmlSerialization, format is 123.456789
        /// </summary>
        public string SerializeString()
        {
            string num = RawValue.ToString(FormatSpecifier);
            string intPart = num.Substring(0, num.Length - Places);
            string decPart = num.Substring(num.Length - Places).TrimEnd(TrimChars);
            if (decPart.Length > 0)
                return intPart + "." + decPart;
            else
                return intPart;
        }

        /// <summary>
        /// For XmlSerialization, format is 123.456789
        /// Handles double and decimal formats too.
        /// </summary>
        public static MyFixedPoint DeserializeStringSafe(string text)
        {
            // For backward compatibility
            for (int i = 0; i < text.Length; i++)
            {
                var c = text[i];
                if ((c >= '0' && c <= '9') || c == '.')
                    continue;

                if (c == '-' && i == 0)
                    continue;

                return (MyFixedPoint)double.Parse(text);
            }

            try
            {
                return DeserializeString(text);
            }
            catch
            {
                Debug.Fail("MyFixedPoint deserialization failed, maybe numbers too large? This is ok, will deserialize as double and then convert to MyFixedPoint");
                return (MyFixedPoint)double.Parse(text);
            }
        }

        public static MyFixedPoint DeserializeString(string text)
        {
            if (String.IsNullOrEmpty(text))
                return new MyFixedPoint();

            int dotPos = text.IndexOf('.');
            if (dotPos == -1)
            {
                return new MyFixedPoint(long.Parse(text) * Divider);
            }
            else
            {
                // Append zeros
                text = text.Replace(".", "");
                text = text.PadRight(dotPos + 1 + Places, '0');
                text = text.Substring(0, dotPos + Places);
                return new MyFixedPoint(long.Parse(text));
            }
        }

        public static explicit operator MyFixedPoint(float d)
        {
            if ((d * Divider + 0.5f) >= (float) long.MaxValue) return MyFixedPoint.MaxValue;
            if ((d * Divider + 0.5f) <= (float) long.MinValue) return MyFixedPoint.MinValue;
            return new MyFixedPoint((long)(d * Divider + 0.5f));
        }

        public static explicit operator MyFixedPoint(double d)
        {
            if ((d * Divider + 0.5) >= (double) long.MaxValue) return MyFixedPoint.MaxValue;
            if ((d * Divider + 0.5) <= (double) long.MinValue) return MyFixedPoint.MinValue;
            return new MyFixedPoint((long)(d * Divider + 0.5));
        }

        public static explicit operator MyFixedPoint(decimal d)
        {
            return new MyFixedPoint((long)(d * Divider + 0.5m));
        }

        public static implicit operator MyFixedPoint(int i)
        {
            return new MyFixedPoint((long)i * Divider);
        }

        public static explicit operator decimal(MyFixedPoint fp)
        {
            return fp.RawValue / (decimal)Divider;
        }

        public static explicit operator float(MyFixedPoint fp)
        {
            return fp.RawValue / (float)Divider;
        }

        public static explicit operator double(MyFixedPoint fp)
        {
            return fp.RawValue / (double)Divider;
        }

        public static explicit operator int(MyFixedPoint fp)
        {
            return (int)(fp.RawValue / Divider);
        }

        public static bool IsIntegral(MyFixedPoint fp)
        {
            return fp.RawValue % Divider == 0;
        }

        public static MyFixedPoint Ceiling(MyFixedPoint a)
        {
            a.RawValue = ((a.RawValue + Divider - 1) / Divider) * Divider;
            return a;
        }

        public static MyFixedPoint Floor(MyFixedPoint a)
        {
            a.RawValue = (a.RawValue / Divider) * Divider;
            return a;
        }

        public static MyFixedPoint Min(MyFixedPoint a, MyFixedPoint b)
        {
            return a < b ? a : b;
        }

        public static MyFixedPoint Max(MyFixedPoint a, MyFixedPoint b)
        {
            return a > b ? a : b;
        }

        public static MyFixedPoint Round(MyFixedPoint a)
        {
            a.RawValue = (a.RawValue + Divider / 2) / Divider;
            return a;
        }

        public static MyFixedPoint operator-(MyFixedPoint a)
        {
            return new MyFixedPoint(-a.RawValue);
        }

        public static bool operator <(MyFixedPoint a, MyFixedPoint b)
        {
            return a.RawValue < b.RawValue;
        }

        public static bool operator >(MyFixedPoint a, MyFixedPoint b)
        {
            return a.RawValue > b.RawValue;
        }

        public static bool operator <=(MyFixedPoint a, MyFixedPoint b)
        {
            return a.RawValue <= b.RawValue;
        }

        public static bool operator >=(MyFixedPoint a, MyFixedPoint b)
        {
            return a.RawValue >= b.RawValue;
        }

        public static bool operator ==(MyFixedPoint a, MyFixedPoint b)
        {
            return a.RawValue == b.RawValue;
        }

        public static bool operator !=(MyFixedPoint a, MyFixedPoint b)
        {
            return a.RawValue != b.RawValue;
        }

        public static MyFixedPoint operator +(MyFixedPoint a, MyFixedPoint b)
        {
            a.RawValue += b.RawValue;
            return a;
        }

        public static MyFixedPoint operator -(MyFixedPoint a, MyFixedPoint b)
        {
            a.RawValue -= b.RawValue;
            return a;
        }

        public static MyFixedPoint operator *(MyFixedPoint a, MyFixedPoint b)
        {
            long ia = a.RawValue / Divider;
            long ib = b.RawValue / Divider;
            long fa = a.RawValue % Divider;
            long fb = b.RawValue % Divider;

            return new MyFixedPoint(ia * ib * Divider + fa * fb / Divider + ia * fb + ib * fa);
        }

        public static MyFixedPoint operator *(MyFixedPoint a, float b)
        {
            return a * (MyFixedPoint)b;
        }

        public static MyFixedPoint operator *(float a, MyFixedPoint b)
        {
            return (MyFixedPoint)a * b;
        }

        public static MyFixedPoint operator *(MyFixedPoint a, int b)
        {
            return a * (MyFixedPoint)b;
        }

        public static MyFixedPoint operator *(int a, MyFixedPoint b)
        {
            return (MyFixedPoint)a * b;
        }

        public static MyFixedPoint AddSafe(MyFixedPoint a, MyFixedPoint b)
        {
            return new MyFixedPoint(AddSafeInternal(a.RawValue, b.RawValue));
        }

        public static MyFixedPoint MultiplySafe(MyFixedPoint a, float b)
        {
            return MultiplySafe(a, (MyFixedPoint)b);
        }

        public static MyFixedPoint MultiplySafe(MyFixedPoint a, int b)
        {
            return MultiplySafe(a, (MyFixedPoint)b);
        }

        public static MyFixedPoint MultiplySafe(float a, MyFixedPoint b)
        {
            return MultiplySafe((MyFixedPoint)a, b);
        }

        public static MyFixedPoint MultiplySafe(int a, MyFixedPoint b)
        {
            return MultiplySafe((MyFixedPoint)a, b);
        }

        public static MyFixedPoint MultiplySafe(MyFixedPoint a, MyFixedPoint b)
        {
            long ia = a.RawValue / Divider;
            long ib = b.RawValue / Divider;
            long fa = a.RawValue % Divider;
            long fb = b.RawValue % Divider;

            // Safely multiply by parts and then add those parts together safely too
            long part1 = fa * fb / Divider;
            long part2 = MultiplySafeInternal(ia, ib * Divider);
            long part3 = MultiplySafeInternal(ia, fb);
            long part4 = MultiplySafeInternal(ib, fa);

            return new MyFixedPoint(AddSafeInternal(AddSafeInternal(AddSafeInternal(part1, part2), part3), part4));
        }

        private static long MultiplySafeInternal(long a, long b)
        {
            long result = a * b;
            if (b == 0 || result / b == a) return result; // Not counting the zero cases, if and only if a does not cause b to overflow when multiplied, then result / b = a

            return (Math.Sign(a) * Math.Sign(b) == 1) ? long.MaxValue : long.MinValue;
        }

        private static long AddSafeInternal(long a, long b)
        {
            int sa = Math.Sign(a);
            if (sa * Math.Sign(b) != 1) return a + b; // Different signs or at least one zero => no overflow

            long result = a + b;
            if (Math.Sign(result) == sa) return result; // Same signs, but the result is also the same => no overflow

            return sa < 0 ? long.MinValue : long.MaxValue; // Overflow => return the correct max value
        }

        public int ToIntSafe()
        {
            if (RawValue > MaxIntValue.RawValue) return (int)MaxIntValue;
            if (RawValue < MinIntValue.RawValue) return (int)MinIntValue;
            return (int)this;
        }

        public override string ToString()
        {
            return SerializeString();
        }

        public override int GetHashCode()
        {
            return (int)RawValue;
        }

        public override bool Equals(object obj)
        {
            if (obj != null)
            {
                var rhs = obj as MyFixedPoint?;
                if (rhs.HasValue)
                    return this == rhs.Value;
            }

            return false;
        }

        XmlSchema IXmlSerializable.GetSchema()
        {
            return null;
        }

        void IXmlSerializable.ReadXml(XmlReader reader)
        {
            string text = reader.ReadInnerXml();
            this.RawValue = DeserializeStringSafe(text).RawValue;
        }

        void IXmlSerializable.WriteXml(XmlWriter writer)
        {
            writer.WriteString(SerializeString());
        }

#if XB1 // XB1_SYNC_SERIALIZER_NOEMIT
        public object GetMemberData(MemberInfo m)
        {
            if (m.Name == "RawValue")
                return RawValue;

            System.Diagnostics.Debug.Assert(false, "TODO for XB1.");
            return null;
        }
#endif // XB1
    }
}
