using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Text;
using VRage;
using VRage.Common.Utils;

namespace System
{
	[Unsharper.UnsharperDisableReflection()]
	public static class StreamExtensions
    {
        [ThreadStatic]
        private static byte[] m_buffer;
        private static byte[] Buffer
        {
            get
            {
                if (m_buffer == null)
                    m_buffer = new byte[256];
                return m_buffer;
            }
        }

        public static bool CheckGZipHeader(this Stream stream)
        {
            Debug.Assert(stream.CanRead, "Cannot check the header of the stream - the stream is not readable");
            Debug.Assert(stream.CanSeek, "Cannot check the header of the stream - the stream is not seekable");

            long currentPosition = stream.Position;

            byte[] magicBytes = new byte[2];
            stream.Seek(0, SeekOrigin.Begin);
            stream.Read(magicBytes, 0, 2);
            if (magicBytes[0] == 0x1f && magicBytes[1] == 0x8b)
            {
                stream.Seek(currentPosition, SeekOrigin.Begin);
                return true;
            }
            else
            {
                stream.Seek(currentPosition, SeekOrigin.Begin);
                return false;
            }
        }

        /// <summary>
        /// Checks for GZip header and if found, returns decompressed Stream, otherwise original Stream
        /// </summary>
        public static Stream UnwrapGZip(this Stream stream)
        {
            return stream.CheckGZipHeader() ? new GZipStream(stream, CompressionMode.Decompress, false) : stream;
        }

        /// <summary>
        /// Wraps stream into GZip compression stream resulting in writing compressed stream
        /// </summary>
        public static Stream WrapGZip(this Stream stream, bool buffered = true)
        {
            // BufferedStream is necessary, because we don't want to compress byte-by-byte
            var gz = new GZipStream(stream, CompressionMode.Compress, false);
            return buffered ? new BufferedStream(gz, 32 * 1024) : (Stream)gz; // BufferedStream closes inner stream when closed, this is safe
        }

        public static int Read7BitEncodedInt(this Stream stream)
        {
            var buffer = Buffer;
            int num1 = 0;
            int num2 = 0;
            while (num2 != 35)
            {
                if (stream.Read(buffer, 0, 1) == 0)
                    throw new EndOfStreamException();
                byte num3 = buffer[0];
                num1 |= ((int)num3 & (int)sbyte.MaxValue) << num2;
                num2 += 7;
                if (((int)num3 & 128) == 0)
                    return num1;
            }
            throw new FormatException("Bad string length. 7bit Int32 format");
        }

        public static void Write7BitEncodedInt(this Stream stream, int value)
        {
            var buffer = Buffer;
            int writtenCount = 0;
            uint num = (uint)value;
            while (num >= 128U)
            {
                buffer[writtenCount++] = (byte)(num | 128U);
                num >>= 7;
                if (writtenCount == buffer.Length)
                {
                    stream.Write(buffer, 0, writtenCount);
                    writtenCount = 0;
                }
            }
            buffer[writtenCount++] = (byte)num;
            stream.Write(buffer, 0, writtenCount);
        }

        public static byte ReadByteNoAlloc(this Stream stream)
        {
            var buffer = Buffer;
            if (stream.Read(buffer, 0, 1) == 0)
                throw new EndOfStreamException();
            return buffer[0];
        }

        public static unsafe void WriteNoAlloc(this Stream stream, byte* bytes, int offset, int count)
        {
            var buffer = Buffer;
            int writtenCount = 0;
            int read = offset;
            int readEnd = offset + count;
            while (read != readEnd)
            {
                buffer[writtenCount++] = bytes[read++];
                if (writtenCount == buffer.Length)
                {
                    stream.Write(buffer, 0, writtenCount);
                    writtenCount = 0;
                }
            }

            if (writtenCount != 0)
                stream.Write(buffer, 0, writtenCount);
        }

        public static unsafe void ReadNoAlloc(this Stream stream, byte* bytes, int offset, int count)
        {
            var buffer = Buffer;
            int readCount = 0;
            int write = offset;
            int writeEnd = offset + count;
            while (write != writeEnd)
            {
                readCount = Math.Min(count, buffer.Length);
                stream.Read(buffer, 0, readCount);
                count -= readCount;
                for (int i = 0; i < readCount; ++i)
                {
                    Debug.Assert(write < writeEnd);
                    bytes[write++] = buffer[i];
                }
            }
        }

        /// <summary>
        /// Writes byte count prefixed encoded text into the file. Byte count is written as 7-bit encoded 32-bit int.
        /// If no encoding is specified, UTF-8 will be used. Byte count prefix specifies number of bytes taken up by
        /// the string, not length of the string itself.
        /// Note that this method may allocate if the size of encoded string exceeds size of prepared buffer.
        /// </summary>
        public static void WriteNoAlloc(this Stream stream, string text, Encoding encoding = null)
        {
            encoding = encoding ?? Encoding.UTF8;
            int byteCount = encoding.GetByteCount(text);
            stream.Write7BitEncodedInt(byteCount);

            var buffer = Buffer;
            if (byteCount > buffer.Length)
            {
                // mk:TODO: Make this work even when encoded string has to be written in parts (prepared buffer is not large enough).
                buffer = new byte[byteCount];
            }
            int bytesWritten = encoding.GetBytes(text, 0, text.Length, buffer, 0);
            Debug.Assert(bytesWritten == byteCount);
            stream.Write(buffer, 0, bytesWritten);
        }

        public static string ReadString(this Stream stream, Encoding encoding = null)
        {
            encoding = encoding ?? Encoding.UTF8;
            int byteCount = stream.Read7BitEncodedInt();

            var buffer = Buffer;
            if (byteCount > buffer.Length)
                buffer = new byte[byteCount];

            stream.Read(buffer, 0, byteCount);
            return encoding.GetString(buffer, 0, byteCount);
        }

        public static void WriteNoAlloc(this Stream stream, byte value) { var buffer = Buffer; buffer[0] = value; stream.Write(buffer, 0, 1); }
        public static unsafe void WriteNoAlloc(this Stream stream, Int16 v) { stream.WriteNoAlloc((byte*)&v, 0, sizeof(Int16)); }
        public static unsafe void WriteNoAlloc(this Stream stream, Int32 v) { stream.WriteNoAlloc((byte*)&v, 0, sizeof(Int32)); }
        public static unsafe void WriteNoAlloc(this Stream stream, Int64 v) { stream.WriteNoAlloc((byte*)&v, 0, sizeof(Int64)); }
        public static unsafe void WriteNoAlloc(this Stream stream, UInt16 v) { stream.WriteNoAlloc((byte*)&v, 0, sizeof(UInt16)); }
        public static unsafe void WriteNoAlloc(this Stream stream, UInt32 v) { stream.WriteNoAlloc((byte*)&v, 0, sizeof(UInt32)); }
        public static unsafe void WriteNoAlloc(this Stream stream, UInt64 v) { stream.WriteNoAlloc((byte*)&v, 0, sizeof(UInt64)); }
        public static unsafe void WriteNoAlloc(this Stream stream, float v) { stream.WriteNoAlloc((byte*)&v, 0, sizeof(float)); }
        public static unsafe void WriteNoAlloc(this Stream stream, double v) { stream.WriteNoAlloc((byte*)&v, 0, sizeof(double)); }
        public static unsafe void WriteNoAlloc(this Stream stream, decimal v) { stream.WriteNoAlloc((byte*)&v, 0, sizeof(decimal)); }

        public static unsafe Int16 ReadInt16(this Stream stream) { Int16 v; stream.ReadNoAlloc((byte*)&v, 0, sizeof(Int16)); return v; }
        public static unsafe Int32 ReadInt32(this Stream stream) { Int32 v; stream.ReadNoAlloc((byte*)&v, 0, sizeof(Int32)); return v; }
        public static unsafe Int64 ReadInt64(this Stream stream) { Int64 v; stream.ReadNoAlloc((byte*)&v, 0, sizeof(Int64)); return v; }
        public static unsafe UInt16 ReadUInt16(this Stream stream) { UInt16 v; stream.ReadNoAlloc((byte*)&v, 0, sizeof(UInt16)); return v; }
        public static unsafe UInt32 ReadUInt32(this Stream stream) { UInt32 v; stream.ReadNoAlloc((byte*)&v, 0, sizeof(UInt32)); return v; }
        public static unsafe UInt64 ReadUInt64(this Stream stream) { UInt64 v; stream.ReadNoAlloc((byte*)&v, 0, sizeof(UInt64)); return v; }
        public static unsafe float ReadFloat(this Stream stream) { float v; stream.ReadNoAlloc((byte*)&v, 0, sizeof(float)); return v; }
        public static unsafe double ReadDouble(this Stream stream) { double v; stream.ReadNoAlloc((byte*)&v, 0, sizeof(double)); return v; }
        public static unsafe decimal ReadDecimal(this Stream stream) { decimal v; stream.ReadNoAlloc((byte*)&v, 0, sizeof(decimal)); return v; }

        public static void SkipBytes(this Stream stream, int byteCount)
        {
            var buffer = Buffer;
            while (byteCount > 0)
            {
                int toRead= (byteCount > buffer.Length) ? buffer.Length : byteCount;
                stream.Read(buffer, 0, toRead);
                byteCount -= toRead;
            }
        }

    }
}
