using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using VRage.Library.Utils;

namespace VRage.Serialization
{
    public class MySerializerEnum<TEnum> : MySerializer<TEnum>
        where TEnum : struct,  IComparable, IFormattable, IConvertible
    {
        readonly static int m_valueCount = MyEnum<TEnum>.Values.Length;
        readonly static TEnum m_firstValue = MyEnum<TEnum>.Values.FirstOrDefault();
        readonly static TEnum m_secondValue = MyEnum<TEnum>.Values.Skip(1).FirstOrDefault();
        readonly static ulong m_firstUlong = MyEnum<TEnum>.GetValue(m_firstValue);
#if UNSHARPER
		readonly static int m_bitCount = (int)Math.Log(MyEnum<TEnum>.GetValue(MyEnum_Range<TEnum>.Max), 2) + 1;
		public readonly static bool HasNegativeValues = Comparer<TEnum>.Default.Compare(MyEnum_Range<TEnum>.Min, default(TEnum)) < 0;
#else
        readonly static int m_bitCount = (int)Math.Log(MyEnum<TEnum>.GetValue(MyEnum<TEnum>.Range.Max), 2) + 1;
        public readonly static bool HasNegativeValues = Comparer<TEnum>.Default.Compare(MyEnum<TEnum>.Range.Min, default(TEnum)) < 0;
#endif
        // TODO: Special serialization for flags

        static MySerializerEnum()
        {
            //Debug.Assert(!HasNegativeValues, "Enum has negative values, are you kidding me? " + typeof(TEnum).Name);
        }

        public override void Clone(ref TEnum value)
        {
        }

        public override bool Equals(ref TEnum a, ref TEnum b)
        {
            return MyEnum<TEnum>.GetValue(a) == MyEnum<TEnum>.GetValue(b);
        }

        public override void Read(Library.Collections.BitStream stream, out TEnum value, MySerializeInfo info)
        {
            if (m_valueCount == 1)
            {
                value = m_firstValue;
            }
            else if (m_valueCount == 2)
            {
                value = stream.ReadBool() ? m_firstValue : m_secondValue;
            }
            else if (m_valueCount > 2)
            {
                if (HasNegativeValues)
                    value = MyEnum<TEnum>.SetValue((ulong)stream.ReadInt64Variant());
                else
                    value = MyEnum<TEnum>.SetValue(stream.ReadUInt64(m_bitCount));
            }
            else
            {
                value = default(TEnum);
            }
        }

        public override void Write(Library.Collections.BitStream stream, ref TEnum value, MySerializeInfo info)
        {
            ulong val = MyEnum<TEnum>.GetValue(value);
            if (m_valueCount == 2)
            {
                stream.WriteBool(val == m_firstUlong);
            }
            else if (m_valueCount > 2)
            {
                if (HasNegativeValues)
                    stream.WriteVariantSigned((long)val);
                else
                    stream.WriteUInt64(val, m_bitCount);
            }
        }
    }
}
