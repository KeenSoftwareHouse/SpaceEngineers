using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Diagnostics;

namespace VRage
{
    public class ByteStream : Stream
    {
        private byte[] m_baseArray;
        private int m_position;
        private int m_length;

        public readonly bool Expandable;
        public readonly bool Resetable;

        /// <summary>
        /// Create non-resetable Stream, optionally expandable
        /// </summary>
        public ByteStream(int capacity, bool expandable = true)
        {
            Expandable = true;
            Resetable = false;
            m_baseArray = new byte[capacity];
            m_length = m_baseArray.Length;
        }

        /// <summary>
        /// Creates resetable Stream
        /// </summary>
        public ByteStream()
        {
            Resetable = true;
            Expandable = false;
        }

        /// <summary>
        /// Creates and initializes resetable Stream
        /// </summary>
        public ByteStream(byte[] newBaseArray, int length)
            : this()
        {
            Reset(newBaseArray, length);
        }

        public byte[] Data
        {
            get { return m_baseArray; }
        }

        public override bool CanRead
        {
            get { return true; }
        }

        public override bool CanSeek
        {
            get { return true; }
        }

        public override bool CanWrite
        {
            get { return true; }
        }

        public override void Flush()
        {
        }

        public override long Length
        {
            get { return m_length; }
        }

        public override long Position
        {
            get
            {
                return m_position;
            }
            set
            {
                m_position = (int)value;
            }
        }

        public void Reset(byte[] newBaseArray, int length)
        {
            if (!Resetable)
            {
                throw new InvalidOperationException("Stream is not created as resetable");
            }
            if (newBaseArray.Length < length)
            {
                throw new ArgumentException("Length must be >= newBaseArray.Length");
            }
            m_baseArray = newBaseArray;
            m_length = length;
            m_position = 0;
        }

        /// <summary>
        /// Original C# implementation
        /// </summary>
        public override int Read(byte[] buffer, int offset, int count)
        {
            int byteCount = (int)(m_length - m_position);
            if (byteCount > count)
            {
                byteCount = count;
            }
            if (byteCount <= 0)
            {
                return 0;
            }
            if (byteCount <= 8)
            {
                int num2 = byteCount;
                while (--num2 >= 0)
                {
                    buffer[offset + num2] = m_baseArray[m_position + num2];
                }
            }
            else
            {
                Buffer.BlockCopy(m_baseArray, m_position, buffer, offset, byteCount);
            }
            m_position += byteCount;
            return byteCount;

        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            switch (origin)
            {
                case SeekOrigin.Begin:
                    {
                        m_position = (int)offset;
                        break;
                    }
                case SeekOrigin.Current:
                    {
                        m_position += (int)offset;
                        break;
                    }
                case SeekOrigin.End:
                    {
                        m_position = m_length + (int)offset;
                        break;
                    }
                default:
                    throw new ArgumentException("Invalid seek origin");
            }
            return m_position;
        }

        public void EnsureCapacity(long minimumSize)
        {
            if (m_length < minimumSize)
            {
                if (Expandable)
                {
                    if (minimumSize < 256)
                        minimumSize = 256;
                    if (minimumSize < m_length * 2)
                        minimumSize = m_length * 2;

                    Resize(minimumSize);
                }
                else
                {
                    throw new EndOfStreamException("ByteSteam is not large enough and is not expandable");
                }
            }
        }

        public void CheckCapacity(long minimumSize)
        {
            if (m_length < minimumSize)
            {
                throw new EndOfStreamException("Stream does not have enough size");
            }
        }

        void Resize(long size)
        {
            Debug.Assert(Expandable, "Only expandable Streams can be resized");
            Array.Resize(ref m_baseArray, (int)size);
            m_length = m_baseArray.Length;
        }

        public override void SetLength(long value)
        {
            if (Expandable)
            {
                Resize((int)value);
            }
            else
            {
                throw new InvalidOperationException("ByteStream is not expandable");
            }
        }

        public new unsafe byte ReadByte()
        {
            CheckCapacity(m_position + 1);
            byte result = m_baseArray[m_position];
            m_position++;
            return result;
        }

        public new unsafe void WriteByte(byte value)
        {
            EnsureCapacity(m_position + 1);
            m_baseArray[m_position] = value;
            m_position++;
        }

        public unsafe ushort ReadUShort()
        {
            CheckCapacity(m_position + sizeof(ushort));
            fixed (byte* ptr = &m_baseArray[m_position])
            {
                m_position += sizeof(ushort);
                return *(ushort*)ptr;
            }
        }

        public unsafe void WriteUShort(ushort value)
        {
            EnsureCapacity(m_position + sizeof(ushort));
            fixed (byte* ptr = &m_baseArray[m_position])
            {
                *(ushort*)ptr = value;
            }
            m_position += sizeof(ushort);
        }

        /// <summary>
        /// Original C# implementation
        /// </summary>
        public override void Write(byte[] buffer, int offset, int count)
        {
            EnsureCapacity(m_position + count);

            int num = m_position + count;
            if ((count <= 8) && (buffer != m_baseArray))
            {
                int num2 = count;
                while (--num2 >= 0)
                {
                    m_baseArray[m_position + num2] = buffer[offset + num2];
                }
            }
            else
            {
                Buffer.BlockCopy(buffer, offset, m_baseArray, m_position, count);
            }
            m_position = num;
        }
    }
}
