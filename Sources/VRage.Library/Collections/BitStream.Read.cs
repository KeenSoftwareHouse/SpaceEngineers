using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using VRage.Library.Utils;
using SharpDX;
using SharpDX.Mathematics;

namespace VRage.Library.Collections
{
	[Unsharper.UnsharperDisableReflection()]
    public unsafe partial class BitStream
    {
        private const long Int64Msb = ((long)1) << 63;
        private const int Int32Msb = ((int)1) << 31;

        [System.Runtime.ExceptionServices.HandleProcessCorruptedStateExceptions]
        [System.Security.SecurityCriticalAttribute]
        ulong ReadInternal(int bitSize)
        {
            Debug.Assert(!m_writing, "Trying to read from non-reading stream");
            //if (m_bitLength < m_bitPosition + bitSize)
              //  throw new BitStreamException(new System.IO.EndOfStreamException("Cannot read from bit stream, end of steam"));

            int longOffsetStart = m_bitPosition >> 6;
            int longOffsetEnd = (m_bitPosition + bitSize - 1) >> 6;

            ulong basemask = (ulong.MaxValue >> (64 - bitSize));
            int placeOffset = m_bitPosition & 0x3F;

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
            m_bitPosition += bitSize;
            return value & basemask;
        }

        public void SetBitPositionRead(int newReadBitPosition)
        {
            Debug.Assert(!m_writing, "This method is supposed to be called on read stream");
            m_bitPosition = newReadBitPosition;
        }

        public double ReadDouble()
        {
            ulong val = ReadInternal(sizeof(double) * 8);
            return *(double*)&val;
        }

        public float ReadHalf()
        {
            return new Half(ReadUInt16());
        }

        public float ReadFloat()
        {
            ulong val = ReadInternal(sizeof(float) * 8);
            return *(float*)&val;
        }
        
        /// <summary>
        /// Reads uniform-spaced float within -1,1 range with specified number of bits.
        /// </summary>
        public float ReadNormalizedSignedFloat(int bits)
        {
            return MyLibraryUtils.DenormalizeFloatCenter(ReadUInt32(bits), -1, 1, bits);
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

            if(byteCount == 0)
            {
                return String.Empty;
            }
            else if (byteCount <= 1024)
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

        /// <summary>
        /// Reads prefixed length string, returns nubmer of characters read.
        /// Passed array is automatically resized when needed.
        /// </summary>
        /// <returns>Nubmer of characters read.</returns>
        public int ReadPrefixLengthString(ref char[] value, Encoding encoding)
        {
            int byteCount = (int)ReadUInt32Variant();
            if(byteCount == 0)
            {
                return 0;
            }
            else if (byteCount <= 1024)
            {
                byte* bytes = stackalloc byte[byteCount];
                return ReadChars(bytes, byteCount, ref value, encoding);
            }
            else
            {
                // Heap alloc
                Debug.Fail("Consider writing better read method");
                byte[] bytes = new byte[byteCount];
                fixed (byte* ptr = bytes)
                {
                    return ReadChars(ptr, byteCount, ref value, encoding);
                }
            }
        }

        private int ReadChars(byte* tmpBuffer, int byteCount, ref char[] outputArray, Encoding encoding)
        {
            ReadMemory(tmpBuffer, byteCount * 8);
            int charCount = encoding.GetCharCount(tmpBuffer, byteCount);
            if (charCount > outputArray.Length)
                outputArray = new char[Math.Max(charCount, outputArray.Length * 2)];
            fixed (char* chars = &outputArray[0])
            {
                encoding.GetChars(tmpBuffer, byteCount, chars, charCount);
            }
            return charCount;
        }

        public void ReadBytes(byte[] bytes, int start, int count)
        {
            fixed (byte* ptr = bytes)
            {
                ReadMemory(&ptr[start], sizeof(byte) * count * 8);
            }
        }

        public byte[] ReadPrefixBytes()
        {
            var len = (int)ReadUInt32Variant();
            byte[] result = new byte[len];
            ReadBytes(result, 0, len);
            return result;
        }
    }
}