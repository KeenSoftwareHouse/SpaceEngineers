using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.Library.Collections;

namespace VRage.Serialization
{
    public class MySerializerHashSet<TItem> : MySerializer<HashSet<TItem>>
    {
        MySerializer<TItem> m_itemSerializer = MyFactory.GetSerializer<TItem>();

        public override void Clone(ref HashSet<TItem> value)
        {
            TItem item;
            var copy = new HashSet<TItem>();
            foreach (var obj in value)
            {
                item = obj;
                m_itemSerializer.Clone(ref item);
                copy.Add(item);
            }
            value = copy;
        }

        public override bool Equals(ref HashSet<TItem> a, ref HashSet<TItem> b)
        {
            if (ReferenceEquals(a, b))
                return true;
            else if (AnyNull(a, b))
                return false;
            else if (a.Count != b.Count)
                return false;
            else
            {
                foreach (var item in a)
                {
                    // TODO: b equality comparer is used, not sure if it's correct (but it's fast)
                    if (!b.Contains(item))
                        return false;
                }
                return true;
            }
        }

        public override void Read(BitStream stream, out HashSet<TItem> value, MySerializeInfo info)
        {
            TItem item;
            int num = (int)stream.ReadUInt32Variant();
            value = new HashSet<TItem>();
            for (int i = 0; i < num; i++)
            {
                MySerializationHelpers.CreateAndRead<TItem>(stream, out item, m_itemSerializer, info.ItemInfo ?? MySerializeInfo.Default);
                value.Add(item);
            }
        }

        public override void Write(BitStream stream, ref HashSet<TItem> value, MySerializeInfo info)
        {
            TItem item;
            int num = value.Count;
            stream.WriteVariant((uint)num);
            foreach (var obj in value)
            {
                item = obj;
                MySerializationHelpers.Write<TItem>(stream, ref item, m_itemSerializer, info.ItemInfo ?? MySerializeInfo.Default);
            }
        }
    }
}
