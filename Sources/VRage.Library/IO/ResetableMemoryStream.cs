using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace VRage
{
    public class ResetableMemoryStream : Stream
    {
        private byte[] m_baseArray;
        private int m_position;
        private int m_length;

        public ResetableMemoryStream()
        {
        }

        public ResetableMemoryStream(byte[] baseArray, int length)
        {
            Reset(baseArray, length);
        }

        public void Reset(byte[] newBaseArray, int length)
        {
            if (newBaseArray.Length < length)
            {
                throw new ArgumentException("Length must be >= newBaseArray.Length");
            }
            m_baseArray = newBaseArray;
            m_length = length;
            m_position = 0;
        }

        public byte[] GetInternalBuffer()
        {
            return m_baseArray;
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

        public override void SetLength(long value)
        {
            throw new InvalidOperationException("Operation not supported");
        }

        /// <summary>
        /// Original C# implementation
        /// </summary>
        public override void Write(byte[] buffer, int offset, int count)
        {
            if (m_length < (m_position + count))
            {
                throw new EndOfStreamException();
            }

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
