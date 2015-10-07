using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.Library.Collections;

namespace VRage.Serialization
{
    public class MySerializerList<TItem> : MySerializer<List<TItem>>
    {
        MySerializer<TItem> m_itemSerializer = MyFactory.GetSerializer<TItem>();

        public override void Clone(ref List<TItem> value)
        {
            TItem item;
            var copy = new List<TItem>(value.Count);
            for (int i = 0; i < value.Count; i++)
            {
                item = value[i];
                m_itemSerializer.Clone(ref item);
                copy.Add(item);
            }
            value = copy;
        }

        public override bool Equals(ref List<TItem> a, ref List<TItem> b)
        {
            if (ReferenceEquals(a, b))
                return true;
            else if (AnyNull(a, b))
                return false;
            else if (a.Count != b.Count)
                return false;
            else
            {
                TItem aa, bb;
                for (int i = 0; i < a.Count; i++)
                {
                    aa = a[i];
                    bb = b[i];
                    if (!m_itemSerializer.Equals(ref aa, ref bb))
                        return false;
                }
                return true;
            }
        }

        public override void Read(BitStream stream, out List<TItem> value, MySerializeInfo info)
        {
            TItem item;
            int num = (int)stream.ReadUInt32Variant();
            value = new List<TItem>(num);
            for (int i = 0; i < num; i++)
            {
                MySerializationHelpers.CreateAndRead<TItem>(stream, out item, m_itemSerializer, info.ItemInfo ?? MySerializeInfo.Default);
                value.Add(item);
            }
        }

        public override void Write(BitStream stream, ref List<TItem> value, MySerializeInfo info)
        {
            TItem item;
            int num = value.Count;
            stream.WriteVariant((uint)num);
            for (int i = 0; i < num; i++)
            {
                item = value[i];
                MySerializationHelpers.Write<TItem>(stream, ref item, m_itemSerializer, info.ItemInfo ?? MySerializeInfo.Default);
            }
        }
    }
}
