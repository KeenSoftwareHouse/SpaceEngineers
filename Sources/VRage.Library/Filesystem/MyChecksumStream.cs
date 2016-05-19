using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

#if XB1
using System.Diagnostics;
#endif


namespace VRage.Common.Utils
{
    internal class MyCheckSumStream : Stream
    {
#if !XB1
        private MyRSA m_verifier;
#endif
        private Stream m_stream;
        private string m_filename;
        private byte[] m_signedHash;
        private byte[] m_publicKey;
        private Action<string, string> m_failHandler;
        private long m_lastPosition = 0;

        private byte[] m_tmpArray = new byte[1];

        internal MyCheckSumStream(Stream stream, string filename, byte[] signedHash, byte[] publicKey, Action<string, string> failHandler)
        {
            m_stream = stream;
#if !XB1
            m_verifier = new MyRSA();
#endif
            m_signedHash = signedHash;
            m_publicKey = publicKey;
            m_filename = filename;
            m_failHandler = failHandler;
        }

        protected override void Dispose(bool disposing)
        {
            System.Diagnostics.Debug.Assert(disposing, "Finalizer in place.");

            if (disposing)
            {
#if XB1
			Debug.Assert(false, "Change verification method");
#else
                m_verifier.HashObject.TransformFinalBlock(new byte[0], 0, 0);

                if (!m_verifier.VerifyHash(m_verifier.HashObject.Hash, m_signedHash, m_publicKey))
                {
                    m_failHandler(m_filename, Convert.ToBase64String(m_verifier.HashObject.Hash));
                }
#endif
				m_stream.Dispose();
            }
            base.Dispose(disposing);
        }

        public override int Read(byte[] array, int offset, int count)
        {
            int skip = (int)(m_lastPosition - m_stream.Position);
            var bytesRead = m_stream.Read(array, offset, count);
            int checkOffset = offset + skip;
            int checkCount = bytesRead - skip;
#if XB1
			Debug.Assert(false, "Change verification method");
#else
            if (checkCount > 0 && checkOffset > 0)
            {
                m_verifier.HashObject.TransformBlock(array, offset + skip, bytesRead - skip, null, 0);
            }
#endif
            m_lastPosition = m_stream.Position;

            return bytesRead;
        }

        #region Stream implementation

        public override bool CanRead { get { return m_stream.CanRead; } }
        public override bool CanSeek { get { return m_stream.CanSeek; } }
        public override bool CanWrite { get { return m_stream.CanWrite; } }
        public override long Length { get { return m_stream.Length; } }
        public override long Position { get { return m_stream.Position; } set { m_stream.Position = value; } }

        public override void Flush() { m_stream.Flush(); }
        public override int ReadByte() 
        {
            if (this.Read(m_tmpArray, 0, 1) == 0)
                return -1;
            else
                return (int)m_tmpArray[0];
        }
        public override long Seek(long offset, SeekOrigin origin) { return m_stream.Seek(offset, origin); }
        public override void SetLength(long value) { m_stream.SetLength(value); }
        public override void Write(byte[] array, int offset, int count) { m_stream.Write(array, offset, count); }
        public override void WriteByte(byte value) { m_stream.WriteByte(value); }
        #endregion
    }
}
