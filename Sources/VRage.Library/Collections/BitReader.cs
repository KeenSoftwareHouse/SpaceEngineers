using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VRage.Library.Collections
{
    /// <summary>
    /// Lightweight bit reader which works on native pointer.
    /// Stores bit length and current position.
    /// </summary>
    public unsafe struct BitReader
    {
        ulong* m_buffer;
        int m_bitLength;

        public int BitPosition;

        public BitReader(IntPtr data, int bitLength)
        {
            m_buffer = (ulong*)(void*)data;
            m_bitLength = bitLength;
            BitPosition = 0;
        }

        public void Reset(IntPtr data, int bitLength)
        {
            m_buffer = (ulong*)(void*)data;
            m_bitLength = bitLength;
            BitPosition = 0;
        }

        private const long Int64Msb = ((long)1) << 63;
        private const int Int32Msb = ((int)1) << 31;

        ulong ReadInternal(int bitSize)
        {
            if (m_bitLength < BitPosition + bitSize)
                throw new BitStreamException(new System.IO.EndOfStreamException("Cannot read from bit stream, end of steam"));

            int longOffsetStart = BitPosition >> 6;
            int longOffsetEnd = (BitPosition + bitSize - 1) >> 6;

            ulong basemask = (ulong.MaxValue >> (64 - bitSize));
            int placeOffset = BitPosition & ~0x40;

            ulong value = (m_buffer[longOffsetStart] >> placeOffset);

            // Unused bits are cut at the end (value & basemask)
            //value = value & basemask; // Cut unused bits
            //m_buffer[longOffsetStart] |= value << placeOffset;

            if (longOffsetEnd != longOffsetStart)
            {
                // Read second part of data
                value |= m_buffer[longOffsetEnd] << (64 - placeOffset);
                //m_buffer[longOffsetEnd] = value >> (64 - placeOffset);
            }
            BitPosition += bitSize;
            return value & basemask;
        }

        public double ReadDouble()
        {
            ulong val = ReadInternal(sizeof(double) * 8);
            return *(double*)&val;
        }

        public float ReadFloat()
        {
            ulong val = ReadInternal(sizeof(float) * 8);
            return *(float*)&val;
        }

        public decimal ReadDecimal()
        {
            decimal result;
            ((ulong*)&result)[0] = ReadInternal(sizeof(ulong) * 8);
            ((ulong*)&result)[1] = ReadInternal(sizeof(ulong) * 8);
            return result;
        }

        public bool ReadBool()
        {
            return ReadInternal(1) != 0;
        }

        public sbyte ReadSByte(int bitCount = sizeof(sbyte) * 8)
        {
            return unchecked((sbyte)ReadInternal(bitCount));
        }

        public short ReadInt16(int bitCount = sizeof(short) * 8)
        {
            return unchecked((short)ReadInternal(bitCount));
        }

        public int ReadInt32(int bitCount = sizeof(int) * 8)
        {
            return unchecked((int)ReadInternal(bitCount));
        }

        public long ReadInt64(int bitCount = sizeof(long) * 8)
        {
            return unchecked((long)ReadInternal(bitCount));
        }

        public byte ReadByte(int bitCount = sizeof(byte) * 8)
        {
            return unchecked((byte)ReadInternal(bitCount));
        }

        public ushort ReadUInt16(int bitCount = sizeof(ushort) * 8)
        {
            return unchecked((ushort)ReadInternal(bitCount));
        }

        public uint ReadUInt32(int bitCount = sizeof(uint) * 8)
        {
            return unchecked((uint)ReadInternal(bitCount));
        }

        public ulong ReadUInt64(int bitCount = sizeof(ulong) * 8)
        {
            return unchecked((ulong)ReadInternal(bitCount));
        }

        private static int Zag(uint ziggedValue)
        {
            int value = (int)ziggedValue;
            return (-(value & 0x01)) ^ ((value >> 1) & ~Int32Msb);
        }

        private static long Zag(ulong ziggedValue)
        {
            long value = (long)ziggedValue;
            return (-(value & 0x01L)) ^ ((value >> 1) & ~Int64Msb);
        }

        public int ReadInt32Variant()
        {
            return Zag(ReadUInt32Variant());
        }

        public long ReadInt64Variant()
        {
            return Zag(ReadUInt64Variant());
        }

        public uint ReadUInt32Variant()
        {
            uint value = ReadByte();
            if ((value & 0x80) == 0) return value;
            value &= 0x7F;

            uint chunk = ReadByte();
            value |= (chunk & 0x7F) << 7;
            if ((chunk & 0x80) == 0) return value;

            chunk = ReadByte();
            value |= (chunk & 0x7F) << 14;
            if ((chunk & 0x80) == 0) return value;

            chunk = ReadByte();
            value |= (chunk & 0x7F) << 21;
            if ((chunk & 0x80) == 0) return value;

            chunk = ReadByte();
            value |= chunk << 28; // can only use 4 bits from this chunk
            if ((chunk & 0xF0) == 0) return value;

            throw new BitStreamException(new OverflowException("Error when deserializing variant uint32"));
        }

        public ulong ReadUInt64Variant()
        {
            ulong value = ReadByte();
            if ((value & 0x80) == 0) return value;
            value &= 0x7F;

            ulong chunk = ReadByte();
            value |= (chunk & 0x7F) << 7;
            if ((chunk & 0x80) == 0) return value;

            chunk = ReadByte();
            value |= (chunk & 0x7F) << 14;
            if ((chunk & 0x80) == 0) return value;

            chunk = ReadByte();
            value |= (chunk & 0x7F) << 21;
            if ((chunk & 0x80) == 0) return value;

            chunk = ReadByte();
            value |= (chunk & 0x7F) << 28;
            if ((chunk & 0x80) == 0) return value;

            chunk = ReadByte();
            value |= (chunk & 0x7F) << 35;
            if ((chunk & 0x80) == 0) return value;

            chunk = ReadByte();
            value |= (chunk & 0x7F) << 42;
            if ((chunk & 0x80) == 0) return value;


            chunk = ReadByte();
            value |= (chunk & 0x7F) << 49;
            if ((chunk & 0x80) == 0) return value;

            chunk = ReadByte();
            value |= (chunk & 0x7F) << 56;
            if ((chunk & 0x80) == 0) return value;

            chunk = ReadByte();
            value |= chunk << 63; // can only use 1 bit from this chunk

            if ((chunk & ~(ulong)0x01) != 0)
                throw new BitStreamException(new OverflowException("Error when deserializing variant uint64"));
            return value;
        }

        public char ReadChar(int bitCount = sizeof(char) * 8)
        {
            return unchecked((char)ReadInternal(bitCount));
        }

        public void ReadMemory(IntPtr ptr, int bitSize)
        {
            ReadMemory((void*)ptr, bitSize);
        }

        public void ReadMemory(void* ptr, int bitSize)
        {
            int numLongs = bitSize / 8 / sizeof(ulong);
            ulong* p = (ulong*)ptr;
            for (int i = 0; i < numLongs; i++)
            {
                p[i] = ReadUInt64();
            }
            int remainingBits = bitSize - numLongs * sizeof(ulong) * 8;
            byte* bptr = (byte*)&p[numLongs];
            while (remainingBits > 0)
            {
                int readBits = Math.Min(remainingBits, 8);
                *bptr = ReadByte(readBits);
                remainingBits -= readBits;
                bptr++;
            }
        }

        public string ReadPrefixLengthString(Encoding encoding)
        {
            int byteCount = (int)ReadUInt32Variant();

            if (byteCount <= 1024)
            {
                byte* bytes = stackalloc byte[byteCount];
                ReadMemory(bytes, byteCount * 8);
                int charCount = encoding.GetCharCount(bytes, byteCount);
                char* chars = stackalloc char[charCount];
                encoding.GetChars(bytes, byteCount, chars, charCount);
                return new string(chars, 0, charCount);
            }
            else
            {
                // Heap alloc
                byte[] data = new byte[byteCount];
                fixed (byte* ptr = data)
                {
                    ReadMemory(ptr, byteCount * 8);
                }
                return new string(encoding.GetChars(data));
            }
        }

        public void ReadBytes(byte[] bytes, int start, int count)
        {
            fixed (byte* ptr = bytes)
            {
                ReadMemory(&ptr[start], sizeof(byte) * count * 8);
            }
        }
    }
}
