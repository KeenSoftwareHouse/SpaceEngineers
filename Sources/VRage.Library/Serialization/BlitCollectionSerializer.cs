using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VRage.Serialization
{
    /// <summary>
    /// This is not optimal in terms of allocations, but works fine
    /// </summary>
    public class BlitCollectionSerializer<T, TData> : ISerializer<T>
        where T : ICollection<TData>, new()
    {
        public static readonly BlitCollectionSerializer<T, TData> Default = new BlitCollectionSerializer<T, TData>();
        public static readonly BlitSerializer<TData> InnerSerializer = BlitSerializer<TData>.Default;

        public BlitCollectionSerializer()
        {
        }

        public void Serialize(ByteStream destination, ref T data)
        {
            destination.Write7BitEncodedInt(data.Count);
            foreach (var item in data)
            {
                TData copy = item;
                InnerSerializer.Serialize(destination, ref copy);
            }
        }

        public void Deserialize(ByteStream source, out T data)
        {
            data = new T();
            int count = source.Read7BitEncodedInt();
            for (int i = 0; i < count; i++)
            {
                TData item;
                InnerSerializer.Deserialize(source, out item);
                data.Add(item);
            }
        }
    }
}
