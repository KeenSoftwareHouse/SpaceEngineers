using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VRage.Serialization
{
    public class MySerializerArray<TItem> : MySerializer<TItem[]>
    {
        MySerializer<TItem> m_itemSerializer = MyFactory.GetSerializer<TItem>();

        public override void Clone(ref TItem[] value)
        {
            value = (TItem[])value.Clone();
            for (int i = 0; i < value.Length; i++)
            {
                m_itemSerializer.Clone(ref value[i]);
            }
        }

        public override bool Equals(ref TItem[] a, ref TItem[] b)
        {
            if (ReferenceEquals(a, b))
                return true;
            else if (AnyNull(a, b))
                return false;
            else if (a.Length != b.Length)
                return false;
            else
            {
                for (int i = 0; i < a.Length; i++)
                {
                    if (!m_itemSerializer.Equals(ref a[i], ref b[i]))
                        return false;
                }
                return true;
            }
        }

        public override void Read(Library.Collections.BitStream stream, out TItem[] value, MySerializeInfo info)
        {
            int num = (int)stream.ReadUInt32Variant();
            value = new TItem[num];
            for (int i = 0; i < value.Length; i++)
            {
                MySerializationHelpers.CreateAndRead<TItem>(stream, out value[i], m_itemSerializer, info.ItemInfo ?? MySerializeInfo.Default);
            }
        }

        public override void Write(Library.Collections.BitStream stream, ref TItem[] value, MySerializeInfo info)
        {
            stream.WriteVariant((uint)value.Length);
            for (int i = 0; i < value.Length; i++)
            {
                MySerializationHelpers.Write<TItem>(stream, ref value[i], m_itemSerializer, info.ItemInfo ?? MySerializeInfo.Default);
            }
        }
    }
}
