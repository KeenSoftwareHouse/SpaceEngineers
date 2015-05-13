using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Security;
using System.IO;

namespace VRage.Utils
{
    public class MyBinaryReader : BinaryReader
    {
        private Decoder m_decoder;
        private int m_maxCharsSize;
        private byte[] m_charBytes;
        private char[] m_charBuffer;

        public MyBinaryReader(Stream stream)
            : this(stream, new UTF8Encoding())
        {
        }

        public MyBinaryReader(Stream stream, Encoding encoding)
            : base(stream, encoding)
        {
            this.m_decoder = encoding.GetDecoder();
            this.m_maxCharsSize = encoding.GetMaxCharCount(0x80);
            int maxByteCount = encoding.GetMaxByteCount(1);
            if (maxByteCount < 0x10)
            {
                maxByteCount = 0x10;
            }
        }

        public new int Read7BitEncodedInt()
        {
            byte num3;
            int num = 0;
            int num2 = 0;
            do
            {
                if (num2 == 0x23)
                {
                    return -1;
                }
                num3 = this.ReadByte();
                num |= (num3 & 0x7f) << num2;
                num2 += 7;
            }
            while ((num3 & 0x80) != 0);
            return num;
        }

        [SecuritySafeCritical]
        public string ReadStringIncomplete(out bool isComplete)
        {
            if (BaseStream == null)
            {
                isComplete = false;
                return string.Empty;
            }
            int num = 0;
            int capacity = this.Read7BitEncodedInt();
            if (capacity < 0)
            {
                isComplete = false;
                return string.Empty;
            }
            if (capacity == 0)
            {
                isComplete = true;
                return string.Empty;
            }
            if (this.m_charBytes == null)
            {
                this.m_charBytes = new byte[0x80];
            }
            if (this.m_charBuffer == null)
            {
                this.m_charBuffer = new char[this.m_maxCharsSize];
            }
            StringBuilder builder = null;
            do
            {
                int count = ((capacity - num) > 0x80) ? 0x80 : (capacity - num);
                int byteCount = BaseStream.Read(this.m_charBytes, 0, count);
                if (byteCount == 0)
                {
                    //throw new EndOfStreamException(Environment.GetResourceString("IO.EOF_ReadBeyondEOF"));
                    isComplete = false;
                    return builder != null ? builder.ToString() : String.Empty;
                }
                int length = this.m_decoder.GetChars(this.m_charBytes, 0, byteCount, this.m_charBuffer, 0);
                if ((num == 0) && (byteCount == capacity))
                {
                    isComplete = true;
                    return new string(this.m_charBuffer, 0, length);
                }
                if (builder == null)
                {
                    builder = new StringBuilder(capacity);
                }
                builder.Append(this.m_charBuffer, 0, length);
                num += byteCount;
            }
            while (num < capacity);
            isComplete = true;
            return builder.ToString();
        }
    }

}
