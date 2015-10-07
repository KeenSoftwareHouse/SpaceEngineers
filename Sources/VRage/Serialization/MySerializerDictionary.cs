using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.Library.Collections;

namespace VRage.Serialization
{
    public class MySerializerDictionary<TKey, TValue> : MySerializer<Dictionary<TKey, TValue>>
    {
        MySerializer<TKey> m_keySerializer = MyFactory.GetSerializer<TKey>();
        MySerializer<TValue> m_valueSerializer = MyFactory.GetSerializer<TValue>();

        public override void Clone(ref Dictionary<TKey, TValue> obj)
        {
            TKey key;
            TValue value;
            var clone = new Dictionary<TKey, TValue>(obj.Count);
            foreach (var pair in obj)
            {
                key = pair.Key;
                value = pair.Value;
                m_keySerializer.Clone(ref key);
                m_valueSerializer.Clone(ref value);
                clone.Add(key, value);
            }
            obj = clone;
        }

        public override bool Equals(ref Dictionary<TKey, TValue> a, ref Dictionary<TKey, TValue> b)
        {
            if (ReferenceEquals(a, b))
                return true;
            else if (AnyNull(a, b))
                return false;
            else if (a.Count != b.Count)
                return false;
            else
            {
                TValue valA;
                TValue valB;
                foreach (var pair in a)
                {
                    // TODO: b dictionary key comparer is used, not sure if it's correct (but it's fast)
                    valA = pair.Value;
                    if (!b.TryGetValue(pair.Key, out valB) || !m_valueSerializer.Equals(ref valA, ref valB))
                        return false;
                }
                return true;
            }
        }

        public override void Read(BitStream stream, out Dictionary<TKey, TValue> obj, MySerializeInfo info)
        {
            TKey key;
            TValue value;
            int num = (int)stream.ReadUInt32Variant();
            obj = new Dictionary<TKey, TValue>(num);
            for (int i = 0; i < num; i++)
            {
                MySerializationHelpers.CreateAndRead(stream, out key, m_keySerializer, info.KeyInfo ?? MySerializeInfo.Default);
                MySerializationHelpers.CreateAndRead(stream, out value, m_valueSerializer, info.ItemInfo ?? MySerializeInfo.Default);
                obj.Add(key, value);
            }
        }

        public override void Write(BitStream stream, ref Dictionary<TKey, TValue> obj, MySerializeInfo info)
        {
            TKey key;
            TValue value;
            int num = obj.Count;
            stream.WriteVariant((uint)num);
            foreach (var item in obj)
            {
                key = item.Key;
                value = item.Value;
                MySerializationHelpers.Write(stream, ref key, m_keySerializer, info.KeyInfo ?? MySerializeInfo.Default);
                MySerializationHelpers.Write(stream, ref value, m_valueSerializer, info.ItemInfo ?? MySerializeInfo.Default);
            }
        }
    }
}
