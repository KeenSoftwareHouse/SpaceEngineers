using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace VRage
{
    /// <summary>
    /// Fixed point number represented as 64-bit integer with 6 decimal places (one millionts)
    /// </summary>
    [ProtoContract]
    public struct MyFixedPoint : IXmlSerializable
    {
        const int Places = 6;
        const int Divider = 1000000;
        static readonly string FormatSpecifier = "D" + (Places + 1);
        static readonly char[] TrimChars = new char[] { '0' };

        public static readonly MyFixedPoint MinValue = new MyFixedPoint(long.MinValue);
        public static readonly MyFixedPoint MaxValue = new MyFixedPoint(long.MaxValue);
        public static readonly MyFixedPoint SmallestPossibleValue = new MyFixedPoint(1);

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
    }
}
