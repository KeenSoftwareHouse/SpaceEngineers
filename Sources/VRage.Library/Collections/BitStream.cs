//#define USE_TERMINATORS

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using VRage.Library.Utils;
using SharpDX;
using SharpDX.Mathematics;

namespace VRage.Library.Collections
{
    /// <summary>
    /// Stream which writes data based on bits.
    /// When writing, buffer must be reset to zero to write values correctly, this is done by ResetWrite() methods or SetPositionAndClearForward()
    /// </summary>
    public unsafe partial class BitStream : IDisposable
    {
        ulong* m_ownedBuffer;
        int m_ownedBufferBitLength;

        ulong* m_buffer;
        int m_bitLength;

        bool m_writing;
        int m_defaultByteSize;
        
        int m_bitPosition;

        private Crc32 m_hash = new Crc32();

        /// <summary>
        /// Read/write bit position in stream.
        /// </summary>
        public int BitPosition { get { return m_bitPosition; } }

        /// <summary>
        /// Length of valid data (when reading) or buffer (when writing) in bits.
        /// </summary>
        public int BitLength { get { return m_bitLength; } }

        /// <summary>
        /// Position in stream round up to whole bytes.
        /// </summary>
        public int BytePosition { get { return MyLibraryUtils.GetDivisionCeil(m_bitPosition, 8); } }

        /// <summary>
        /// Length of valid data (when reading) or buffer (when writing) round up to whole bytes
        /// </summary>
        public int ByteLength { get { return MyLibraryUtils.GetDivisionCeil(BitLength, 8); } }

        /// <summary>
        /// Returns true when owns buffers, always true when writing.
        /// May or may not own buffer when reading.
        /// </summary>
        public bool OwnsBuffer { get { return m_ownedBuffer == m_buffer; } }

        public bool Reading { get { return !m_writing; } }

        /// <summary>
        /// True when stream is for writing.
        /// </summary>
        public bool Writing { get { return m_writing; } }

        public IntPtr DataPointer
        {
            get { return (IntPtr)(void*)m_buffer; }
        }

        public BitStream(int defaultByteSize = 1536)
        {
            // At least 16 bytes
            m_defaultByteSize = Math.Max(16, MyLibraryUtils.GetDivisionCeil(defaultByteSize, 8) * 8);
        }

        public void Dispose()
        {
            ReleaseInternalBuffer();
            GC.SuppressFinalize(this);
        }

        ~BitStream()
        {
         //   Debug.Fail("Undisposed BitStream");
            ReleaseInternalBuffer();
        }

        void EnsureSize(int bitCount)
        {
            if (m_bitLength < bitCount)
            {
                Resize(bitCount);
            }
        }

        void Resize(int bitSize)
        {
            if (!OwnsBuffer)
                throw new BitStreamException("BitStream cannot write more data. Buffer is full and it's not owned by BitStream", new System.IO.EndOfStreamException());

            // Always at least double the size
            int newBitSize = Math.Max(m_bitLength * 2, bitSize);
            int newByteLen = MyLibraryUtils.GetDivisionCeil(newBitSize, 64) * 8;

            var newBuffer = SharpDX.Utilities.AllocateClearedMemory(newByteLen);
            SharpDX.Utilities.CopyMemory(newBuffer, (IntPtr)(void*)m_buffer, BytePosition);
            SharpDX.Utilities.FreeMemory((IntPtr)(void*)m_buffer);

            m_buffer = (ulong*)(void*)newBuffer;
            m_bitLength = newBitSize;

            m_ownedBuffer = m_buffer;
            m_ownedBufferBitLength = m_bitLength;
        }

        public void ReleaseInternalBuffer()
        {
            if (m_ownedBuffer != null)
            {
                if (m_buffer == m_ownedBuffer)
                {
                    m_buffer = null;
                    m_bitLength = 0;
                }
                SharpDX.Utilities.FreeMemory((IntPtr)(void*)m_ownedBuffer);
                m_ownedBuffer = null;
                m_ownedBufferBitLength = 0;
            }
        }

        /// <summary>
        /// Resets stream for reading (reads what was written so far).
        /// </summary>
        public void ResetRead()
        {
            m_bitLength = m_bitPosition;
            m_buffer = m_ownedBuffer;
            m_writing = false;
            m_bitPosition = 0;
        }

        /// <summary>
        /// Resets stream for reading and copies data.
        /// </summary>
        public void ResetRead(byte[] data, int bitLength)
        {
            ResetRead(data, 0, bitLength);
        }

        /// <summary>
        /// Resets stream for reading and copies data.
        /// </summary>
        public void ResetRead(byte[] data, int byteOffset, int bitLength, bool copy = true)
        {
            fixed (byte* ptr = &data[byteOffset])
            {
                ResetRead((IntPtr)(void*)ptr, bitLength, copy);
            }
        }

        /// <summary>
        /// Resets stream for reading.
        /// </summary>
        public void ResetRead(IntPtr buffer, int bitLength, bool copy)
        {
            if (copy)
            {
                int byteLen = MyLibraryUtils.GetDivisionCeil(bitLength, 8);
                int allocByteSize = Math.Max(byteLen, m_defaultByteSize);

                if (m_ownedBuffer == null)
                {
                    m_ownedBuffer = (ulong*)(void*)SharpDX.Utilities.AllocateClearedMemory(allocByteSize);
                    m_ownedBufferBitLength = allocByteSize * 8;
                }
                else if (m_ownedBufferBitLength < bitLength)
                {
                    SharpDX.Utilities.FreeMemory((IntPtr)(void*)m_ownedBuffer);
                    m_ownedBuffer = (ulong*)(void*)SharpDX.Utilities.AllocateClearedMemory(allocByteSize);
                    m_ownedBufferBitLength = allocByteSize * 8;
                }
                SharpDX.Utilities.CopyMemory((IntPtr)(void*)m_ownedBuffer, buffer, byteLen);

                m_buffer = m_ownedBuffer;
                m_bitLength = bitLength;
                m_bitPosition = 0;
                m_writing = false;
            }
            else
            {
                m_buffer = (ulong*)(void*)buffer;
                m_bitLength = bitLength;
                m_bitPosition = 0;
                m_writing = false;
            }
        }

        /// <summary>
        /// Resets stream for writing.
        /// Uses internal buffer for writing, it's available as DataPointer.
        /// </summary>
        public void ResetWrite()
        {
            if (m_ownedBuffer == null)
            {
                m_ownedBuffer = (ulong*)(void*)SharpDX.Utilities.AllocateClearedMemory(m_defaultByteSize);
                m_ownedBufferBitLength = m_defaultByteSize * 8;
            }
            else
            {
                SharpDX.Utilities.ClearMemory((IntPtr)(void*)m_ownedBuffer, 0, MyLibraryUtils.GetDivisionCeil(m_ownedBufferBitLength, 8));
            }

            m_buffer = m_ownedBuffer;
            m_bitLength = m_ownedBufferBitLength;
            m_bitPosition = 0;
            m_writing = true;
        }

        public void ResetWrite(BitStream stream)
        {
            int bitLength = stream.m_writing ? stream.m_bitPosition : stream.BitLength;
            if (m_ownedBuffer != null && m_ownedBufferBitLength < bitLength)
            {
                SharpDX.Utilities.FreeMemory((IntPtr)(void*)m_ownedBuffer);
                m_ownedBuffer = null;
            }
            if (m_ownedBuffer == null)
            {
                int byteSize = Math.Max(MyLibraryUtils.GetDivisionCeil(bitLength, 64) * 8, m_defaultByteSize);
                m_ownedBuffer = (ulong*)(void*)SharpDX.Utilities.AllocateClearedMemory(byteSize);
                m_ownedBufferBitLength = byteSize * 8;
            }
            m_buffer = m_ownedBuffer;
            m_bitLength = m_ownedBufferBitLength;
            SharpDX.Utilities.CopyMemory(DataPointer, stream.DataPointer, MyLibraryUtils.GetDivisionCeil(bitLength, 8));
            m_bitPosition = bitLength;
            m_writing = true;
        }

        public void Serialize(ref double value)
        {
            if (m_writing) WriteDouble(value);
            else value = ReadDouble();
        }

        public void Serialize(ref float value)
        {
            if (m_writing) WriteFloat(value);
            else value = ReadFloat();
        }

        public void Serialize(ref decimal value)
        {
            if (m_writing) WriteDecimal(value);
            else value = ReadDecimal();
        }

        public void Serialize(ref bool value)
        {
            if (m_writing) WriteBool(value);
            else value = ReadBool();
        }

        public void Serialize(ref sbyte value, int bitCount = sizeof(sbyte) * 8)
        {
            if (m_writing) WriteSByte(value, bitCount);
            else value = ReadSByte(bitCount);
        }

        public void Serialize(ref short value, int bitCount = sizeof(short) * 8)
        {
            if (m_writing) WriteInt16(value, bitCount);
            else value = ReadInt16(bitCount);
        }

        public void Serialize(ref int value, int bitCount = sizeof(int) * 8)
        {
            if (m_writing) WriteInt32(value, bitCount);
            else value = ReadInt32(bitCount);
        }

        public void Serialize(ref long value, int bitCount = sizeof(long) * 8)
        {
            if (m_writing) WriteInt64(value, bitCount);
            else value = ReadInt64(bitCount);
        }

        public void Serialize(ref byte value, int bitCount = sizeof(byte) * 8)
        {
            if (m_writing) WriteByte(value, bitCount);
            else value = ReadByte(bitCount);
        }

        public void Serialize(ref ushort value, int bitCount = sizeof(ushort) * 8)
        {
            if (m_writing) WriteUInt16(value, bitCount);
            else value = ReadUInt16(bitCount);
        }

        public void Serialize(ref uint value, int bitCount = sizeof(uint) * 8)
        {
            if (m_writing) WriteUInt32(value, bitCount);
            else value = ReadUInt32(bitCount);
        }

        public void Serialize(ref ulong value, int bitCount = sizeof(ulong) * 8)
        {
            if (m_writing) WriteUInt64(value, bitCount);
            else value = ReadUInt64(bitCount);
        }

        /// <summary>
        /// Efficiently serializes small integers. Closer to zero, less bytes.
        /// From -64 to 63 (inclusive), 8 bits.
        /// From -8 192 to 8 191 (inclusive), 16 bits.
        /// From -1 048 576 to 1 048 575, 24 bits.
        /// From -134 217 728 to 134 217 727, 32 bits.
        /// Otherwise 40 bits.
        /// </summary>
        public void SerializeVariant(ref int value)
        {
            if (m_writing) WriteVariantSigned(value);
            else value = ReadInt32Variant();
        }

        /// <summary>
        /// Efficiently serializes small integers. Closer to zero, less bytes.
        /// From -64 to 63 (inclusive), 8 bits.
        /// From -8192 to 8191 (inclusive), 16 bits.
        /// From -1048576 to 1048575, 24 bits.
        /// From -134217728 to 134217727, 32 bits.
        /// Etc...
        /// </summary>
        public void SerializeVariant(ref long value)
        {
            if (m_writing) WriteVariantSigned(value);
            else value = ReadInt64Variant();
        }

        /// <summary>
        /// Efficiently serializes small integers. Closer to zero, less bytes.
        /// 0 - 127, 8 bits.
        /// 128 - 16383, 16 bits.
        /// 16384 - 2097151, 24 bits.
        /// 2097152 - 268435455, 32 bits.
        /// Otherwise 40 bits.
        /// </summary>
        public void SerializeVariant(ref uint value)
        {
            if (m_writing) WriteVariant(value);
            else value = ReadUInt32Variant();
        }

        /// <summary>
        /// Efficiently serializes small integers. Closer to zero, less bytes.
        /// 0 - 127, 8 bits.
        /// 128 - 16383, 16 bits.
        /// 16384 - 2097151, 24 bits.
        /// 2097152 - 268435455, 32 bits.
        /// Etc...
        /// </summary>
        public void SerializeVariant(ref ulong value)
        {
            if (m_writing) WriteVariant(value);
            else value = ReadUInt64Variant();
        }

        /// <summary>
        /// Writes char as UTF16, 2-byte value.
        /// For ASCII or different encoding, use SerializeByte() or SerializeBytes().
        /// </summary>
        public void Serialize(ref char value)
        {
            if (m_writing) WriteChar(value);
            else value = ReadChar();
        }

        public void Serialize(StringBuilder value, ref char[] tmpArray, Encoding encoding)
        {
            if(m_writing)
            {
                if (value.Length > tmpArray.Length)
                    tmpArray = new char[Math.Max(value.Length, tmpArray.Length * 2)];
                value.CopyTo(0, tmpArray, 0, value.Length);
                WritePrefixLengthString(tmpArray, 0, value.Length, encoding);
            }
            else 
            {
                value.Clear();
                int charCount = ReadPrefixLengthString(ref tmpArray, encoding);
                value.Append(tmpArray, 0, charCount);
            }
        }

        /// <summary>
        /// Serializes fixed size memory region.
        /// </summary>
        public void SerializeMemory(IntPtr ptr, int bitSize)
        {
            if (m_writing) WriteMemory(ptr, bitSize);
            else ReadMemory(ptr, bitSize);
        }

        /// <summary>
        /// Serializes fixed size memory region.
        /// </summary>
        public unsafe void SerializeMemory(void* ptr, int bitSize)
        {
            if (m_writing) WriteMemory(ptr, bitSize);
            else ReadMemory(ptr, bitSize);
        }

        /// <summary>
        /// Serializes string length (as UInt32 variant) and string itself in defined encoding.
        /// </summary>
        public void SerializePrefixString(ref string str, Encoding encoding)
        {
            if (m_writing) WritePrefixLengthString(str, 0, str.Length, encoding);
            else str = ReadPrefixLengthString(encoding);
        }

        /// <summary>
        /// Serializes string length (as UInt32 variant) and string itself encoded with ASCII encoding.
        /// </summary>
        public void SerializePrefixStringAscii(ref string str)
        {
            SerializePrefixString(ref str, Encoding.ASCII);
        }

        /// <summary>
        /// Serializes string length (as UInt32 variant) and string itself encoded with UTF8 encoding.
        /// </summary>
        /// <param name="str"></param>
        public void SerializePrefixStringUtf8(ref string str)
        {
            SerializePrefixString(ref str, Encoding.UTF8);
        }

        /// <summary>
        /// Serializes byte array length (as UInt32 variant) and bytes.
        /// </summary>
        public void SerializePrefixBytes(ref byte[] bytes)
        {
            if (m_writing)
            {
                WriteVariant((uint)bytes.Length);
                WriteBytes(bytes, 0, bytes.Length);
            }
            else
            {
                int len = (int)ReadUInt32Variant();
                bytes = new byte[len];
                ReadBytes(bytes, 0, len);
            }
        }

        /// <summary>
        /// Serializes fixed-size byte array or it's part (length is NOT serialized).
        /// </summary>
        public void SerializeBytes(ref byte[] bytes, int start, int count)
        {
            if (m_writing) WriteBytes(bytes, start, count);
            else ReadBytes(bytes, start, count);
        }

        const uint TERMINATOR = 0xc8b9;
        public void Terminate()
        {
#if USE_TERMINATORS
            WriteUInt32(TERMINATOR);
#endif
        }
        public void CheckTerminator()
        {
#if USE_TERMINATORS
            var value = ReadInt32();
            if (value != TERMINATOR)
                throw new EndOfStreamException("Invalid BitStream terminator");
#endif
        }
    }
}