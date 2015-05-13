using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace VRage.Compression
{
    /// <summary>
    /// Stream wrapper which will close both stream and other IDisposable object
    /// </summary>
    public class MyStreamWrapper : Stream
    {
        readonly IDisposable m_obj;
        readonly Stream m_innerStream;

        public MyStreamWrapper(Stream innerStream, IDisposable objectToClose)
        {
            m_innerStream = innerStream;
            m_obj = objectToClose;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if(m_obj != null) m_obj.Dispose();
            }

            base.Dispose(disposing);
        }

        public override bool CanRead
        {
            get { return m_innerStream.CanRead; }
        }

        public override bool CanSeek
        {
            get { return m_innerStream.CanSeek; }
        }

        public override bool CanWrite
        {
            get { return m_innerStream.CanWrite; }
        }

        public override void Flush()
        {
            m_innerStream.Flush();
        }

        public override long Length
        {
            get { return m_innerStream.Length; }
        }

        public override long Position
        {
            get
            {
                return m_innerStream.Position;
            }
            set
            {
                m_innerStream.Position = value;
            }
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            return m_innerStream.Read(buffer, offset, count);
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            return m_innerStream.Seek(offset, origin);
        }

        public override void SetLength(long value)
        {
            m_innerStream.SetLength(value);
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            m_innerStream.Write(buffer, offset, count);
        }
    }
}
