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
    public unsafe partial class BitStream
    {
        void WriteInternal(ulong value, int bitSize)
        {
            Debug.Assert(m_writing, "Trying to write into non-writing stream");
            EnsureSize(m_bitPosition + bitSize);

            int longOffsetStart = m_bitPosition >> 6; // == / 64
            int longOffsetEnd = (m_bitPosition + bitSize - 1) >> 6;

            ulong basemask = (ulong.MaxValue >> (64 - bitSize));
            int placeOffset = m_bitPosition & 0x3F; // & 63

            value = value & basemask; // Cut unused bits of value (IMPORTANT!)
            m_buffer[longOffsetStart] |= value << placeOffset;

            if (longOffsetEnd != longOffsetStart)
            {
                // Write second part of data
                m_buffer[longOffsetEnd] = value >> (64 - placeOffset);
            }
            m_bitPosition += bitSize;
        }

        void Clear(int fromPosition)
        {
            int longOffsetStart = fromPosition >> 6; // Get start item in array of longs
            int placeOffset = fromPosition & 0x3F; // Find value bit offset (how many LSB to skip)
            m_buffer[longOffsetStart] &= ~(ulong.MaxValue << placeOffset); // Take all ones, shift them left and negate

            // Clear the rest
            int bufSize = m_bitLength / 64;
            int startClear = longOffsetStart + 1;
            for (int i = startClear; i < bufSize; i++)
            {
                m_buffer[i] = 0;
            }
        }

        /// <summary>
        /// Use when you need to overwrite part of the data.
        /// Sets new bit position and clears everything from min(old position, new position) to end of stream.
        /// </summary>
        public void SetBitPositionWrite(int newBitPosition)
        {
            Debug.Assert(m_writing, "This method is supposed to be called on write stream");

            Clear(Math.Min(m_bitPosition, newBitPosition));
            m_bitPosition = newBitPosition;
        }

        public void WriteDouble(double value)
        {
            WriteInternal(*(ulong*)&value, sizeof(double) * 8);
        }

        public void WriteHalf(float value)
        {
            Half h = new Half(value);
            WriteUInt16(h.RawValue);
        }

        public void WriteFloat(float value)
        {
            WriteInternal(*(uint*)&value, sizeof(float) * 8);
        }

        /// <summary>
        /// Writes uniform-spaced float within -1,1 range with specified number of bits.
        /// </summary>
        public void WriteNormalizedSignedFloat(float value, int bits)
        {
            WriteUInt32(MyLibraryUtils.NormalizeFloatCenter(value, -1, 1, bits), bits);
        }

        public void WriteDecimal(decimal value)
        {
            WriteInternal(((ulong*)&value)[0], sizeof(ulong) * 8);
            WriteInternal(((ulong*)&value)[1], sizeof(ulong) * 8);
        }

        public void WriteBool(bool value)
        {
            WriteInternal(value ? ulong.MaxValue : 0, 1);
        }

        public void WriteSByte(sbyte value, int bitCount = sizeof(sbyte) * 8)
        {
            WriteInternal(unchecked((ulong)value), bitCount);
        }

        public void WriteInt16(short value, int bitCount = sizeof(short) * 8)
        {
            WriteInternal(unchecked((ulong)value), bitCount);
        }

        public void WriteInt32(int value, int bitCount = sizeof(int) * 8)
        {
            WriteInternal(unchecked((ulong)value), bitCount);
        }

        public void WriteInt64(long value, int bitCount = sizeof(long) * 8)
        {
            WriteInternal(unchecked((ulong)value), bitCount);
        }

        public void WriteByte(byte value, int bitCount = sizeof(byte) * 8)
        {
            WriteInternal(value, bitCount);
        }

        public void WriteUInt16(ushort value, int bitCount = sizeof(ushort) * 8)
        {
            WriteInternal(value, bitCount);
        }

        public void WriteUInt32(uint value, int bitCount = sizeof(uint) * 8)
        {
            WriteInternal(value, bitCount);
        }

        public void WriteUInt64(ulong value, int bitCount = sizeof(ulong) * 8)
        {
            WriteInternal(value, bitCount);
        }

        internal static uint Zig(int value)
        {
            return (uint)((value << 1) ^ (value >> 31));
        }

        internal static ulong Zig(long value)
        {
            return (ulong)((value << 1) ^ (value >> 63));
        }

        /// <summary>
        /// Efficiently writes small integers. Closer to zero, less bytes.
        /// From -64 to 63 (inclusive), 8 bits.
        /// From -8 192 to 8 191 (inclusive), 16 bits.
        /// From -1 048 576 to 1 048 575, 24 bits.
        /// From -134 217 728 to 134 217 727, 32 bits.
        /// Otherwise 40 bits.
        /// </summary>
        public void WriteVariantSigned(int value)
        {
            WriteVariant(Zig(value));
        }

        /// <summary>
        /// Efficiently writes small integers. Closer to zero, less bytes.
        /// From -64 to 63 (inclusive), 8 bits.
        /// From -8192 to 8191 (inclusive), 16 bits.
        /// From -1048576 to 1048575, 24 bits.
        /// From -134217728 to 134217727, 32 bits.
        /// Etc...
        /// </summary>
        public void WriteVariantSigned(long value)
        {
            WriteVariant(Zig(value));
        }

        /// <summary>
        /// Efficiently writes small integers. Closer to zero, less bytes.
        /// 0 - 127, 8 bits.
        /// 128 - 16383, 16 bits.
        /// 16384 - 2097151, 24 bits.
        /// 2097152 - 268435455, 32 bits.
        /// Otherwise 40 bits.
        /// </summary>
        public void WriteVariant(uint value)
        {
            ulong data;
            byte* bytes = (byte*)&data;
            int count = 0;
            int index = 0;
            do
            {
                bytes[index++] = (byte)((value & 0x7F) | 0x80);
                count++;
            } while ((value >>= 7) != 0);
            bytes[index - 1] &= 0x7F;
            WriteInternal(data, count * 8); // TODO: possible optimization for largest numbers, uses only 4 bits of last byte
        }

        /// <summary>
        /// Efficiently writes small integers. Closer to zero, less bytes.
        /// 0 - 127, 8 bits.
        /// 128 - 16383, 16 bits.
        /// 16384 - 2097151, 24 bits.
        /// 2097152 - 268435455, 32 bits.
        /// Etc...
        /// </summary>
        public void WriteVariant(ulong value)
        {
            byte* bytes = stackalloc byte[16];
            int count = 0;
            int index = 0;
            do
            {
                bytes[index++] = (byte)((value & 0x7F) | 0x80);
                count++;
            } while ((value >>= 7) != 0);
            bytes[index - 1] &= 0x7F;
            if (count > 8) // TODO: possible optimization for largest numbers, uses only 1 bit of last byte
            {
                WriteInternal(((ulong*)bytes)[0], 8 * 8);
                WriteInternal(((ulong*)bytes)[1], (count - 8) * 8);
            }
            else
            {
                WriteInternal(((ulong*)bytes)[0], count * 8);
            }
        }

        public void WriteChar(char value, int bitCount = sizeof(char) * 8)
        {
            WriteInternal(value, bitCount);
        }

        public void WriteBitStream(BitStream readStream)
        {
            // Make better, preferably by reading to aligned area
            int numBits = readStream.BitLength - readStream.m_bitPosition;
            while (numBits > 0)
            {
                int readBits = Math.Min(64, numBits);
                ulong value = readStream.ReadUInt64(readBits);
                WriteUInt64(value, readBits);
                numBits -= readBits;
            }
        }

        public void WriteMemory(IntPtr ptr, int bitSize)
        {
            WriteMemory((void*)ptr, bitSize);
        }

        public void WriteMemory(void* ptr, int bitSize)
        {
            int numLongs = bitSize / 8 / sizeof(ulong);
            ulong* p = (ulong*)ptr;
            for (int i = 0; i < numLongs; i++)
            {
                WriteUInt64(p[i]);
            }
            int remainingBits = bitSize - numLongs * sizeof(ulong) * 8;
            byte* bptr = (byte*)&p[numLongs];
            while (remainingBits > 0)
            {
                int writeBits = Math.Min(remainingBits, 8);
                WriteByte(*bptr, writeBits);
                remainingBits -= writeBits;
                bptr++;
            }
        }

        public unsafe void WritePrefixLengthString(string str, int characterStart, int characterCount, Encoding encoding)
        {
            fixed (char* ptr = str)
            {
                WritePrefixLengthString(characterStart, characterCount, encoding, ptr);
            }
        }

        public unsafe void WritePrefixLengthString(char[] str, int characterStart, int characterCount, Encoding encoding)
        {
            fixed (char* ptr = str)
            {
                WritePrefixLengthString(characterStart, characterCount, encoding, ptr);
            }
        }

        private unsafe void WritePrefixLengthString(int characterStart, int characterCount, Encoding encoding, char* ptr)
        {
            char* curr = &ptr[characterStart];
            int totalByteCount = encoding.GetByteCount(curr, characterCount);
            WriteVariant((uint)totalByteCount);

            // May not be optimal
            byte* bytes = stackalloc byte[256];
            int maxChars = 256 / encoding.GetMaxByteCount(1);

            while (characterCount > 0)
            {
                int readChars = Math.Min(maxChars, characterCount);
                int byteCount = encoding.GetBytes(curr, readChars, bytes, 256);
                WriteMemory(bytes, byteCount * 8);
                curr += readChars;
                characterCount -= readChars;
            }
        }

        public void WriteBytes(byte[] bytes, int start, int count)
        {
            fixed (byte* ptr = bytes)
            {
                WriteMemory(&ptr[start], sizeof(byte) * count * 8);
            }
        }

        public void WritePrefixBytes(byte[] bytes, int start, int count)
        {
            WriteVariant((uint)count);
            WriteBytes(bytes, start, count);
        }
    }
}