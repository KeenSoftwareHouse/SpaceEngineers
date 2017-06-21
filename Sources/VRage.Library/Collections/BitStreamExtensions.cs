using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.Library.Collections;

namespace System
{
	[Unsharper.UnsharperDisableReflection()]
	public static class BitStreamExtensions
    {
        public delegate void SerializeCallback<T>(BitStream stream, ref T item);
        public delegate T Reader<T>(BitStream bs);
        public delegate void Writer<T>(BitStream bs, T value);

        static void Serialize<T>(this BitStream bs, T[] data, int len, SerializeCallback<T> serializer)
        {
            for (int i = 0; i < len; i++)
            {
                serializer(bs, ref data[i]);
            }
        }

        public static void SerializeList<T>(this BitStream bs, ref List<T> list, SerializeCallback<T> serializer)
        {
            if (bs.Writing)
            {
                T item;
                bs.WriteVariant((uint)list.Count);
                for (int i = 0; i < list.Count; i++)
                {
                    item = list[i];
                    serializer(bs, ref item);
                }
            }
            else
            {
                T item = default(T);
                int count = (int)bs.ReadUInt32Variant();
                list = list ?? new List<T>(count);
                list.Clear();
                for (int i = 0; i < count; i++)
                {
                    serializer(bs, ref item);
                    list.Add(item);
                }
            }
        }

        public static void SerializeList<T>(this BitStream bs, ref List<T> list, Reader<T> reader, Writer<T> writer)
        {
            if (bs.Writing)
            {
                bs.WriteVariant((uint)list.Count);
                for (int i = 0; i < list.Count; i++)
                {
                    writer(bs, list[i]);
                }
            }
            else
            {
                int count = (int)bs.ReadUInt32Variant();
                list = list ?? new List<T>(count);
                list.Clear();
                for (int i = 0; i < count; i++)
                {
                    list.Add(reader(bs));
                }
            }
        }

        public static void SerializeList(this BitStream bs, ref List<int> list)
        {
            bs.SerializeList(ref list, b => b.ReadInt32(), (b, v) => b.WriteInt32(v)); // Should not allocate, it's anonymous function cached by compiler
        }

        public static void SerializeList(this BitStream bs, ref List<uint> list)
        {
            bs.SerializeList(ref list, b => b.ReadUInt32(), (b, v) => b.WriteUInt32(v)); // Should not allocate, it's anonymous function cached by compiler
        }

        public static void SerializeList(this BitStream bs, ref List<long> list)
        {
            bs.SerializeList(ref list, b => b.ReadInt64(), (b, v) => b.WriteInt64(v)); // Should not allocate, it's anonymous function cached by compiler
        }

        public static void SerializeList(this BitStream bs, ref List<ulong> list)
        {
            bs.SerializeList(ref list, b => b.ReadUInt64(), (b, v) => b.WriteUInt64(v)); // Should not allocate, it's anonymous function cached by compiler
        }

        public static void SerializeListVariant(this BitStream bs, ref List<int> list)
        {
            bs.SerializeList(ref list, b => b.ReadInt32Variant(), (b, v) => b.WriteVariantSigned(v)); // Should not allocate, it's anonymous function cached by compiler
        }

        public static void SerializeListVariant(this BitStream bs, ref List<uint> list)
        {
            bs.SerializeList(ref list, b => b.ReadUInt32Variant(), (b, v) => b.WriteVariant(v)); // Should not allocate, it's anonymous function cached by compiler
        }

        public static void SerializeListVariant(this BitStream bs, ref List<long> list)
        {
            bs.SerializeList(ref list, b => b.ReadInt64Variant(), (b, v) => b.WriteVariantSigned(v)); // Should not allocate, it's anonymous function cached by compiler
        }

        public static void SerializeListVariant(this BitStream bs, ref List<ulong> list)
        {
            bs.SerializeList(ref list, b => b.ReadUInt64Variant(), (b, v) => b.WriteVariant(v)); // Should not allocate, it's anonymous function cached by compiler
        }
    }
}
