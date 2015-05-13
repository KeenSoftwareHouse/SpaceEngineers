//#define OLD_LOADING
using System;
using System.IO;
using System.IO.Compression;

namespace VRage
{
    public class MyCompressionFileLoad : IMyCompressionLoad, IDisposable
    {
        [ThreadStatic]
        private static byte[] m_intBytesBuffer;

#if OLD_LOADING
        byte[] m_decompressed;
        int m_readIndex;
#else
        private FileStream m_input;
        private GZipStream m_gz;
        private BufferedStream m_buffer;
#endif

        public MyCompressionFileLoad(string fileName)
        {
#if OLD_LOADING
            using (FileStream fs = File.OpenRead(fileName))
            {
                using (BinaryReader br = new BinaryReader(fs))
                {
                    //  Read whole file to byte array and then decompress
                    m_decompressed = MyCompression.Decompress(br.ReadBytes((int)fs.Length));

                    //  Reset reading index
                    m_readIndex = 0;
                }
                fs.Close();
            }
#else
            if (m_intBytesBuffer == null)
                m_intBytesBuffer = new byte[sizeof(Int32)];

            m_input = File.OpenRead(fileName);
            // Read uncompressed size; Unused but kept for backward compatibility.
            m_input.Read(m_intBytesBuffer, 0, sizeof(Int32));
            m_gz = new GZipStream(m_input, CompressionMode.Decompress);
            m_buffer = new BufferedStream(m_gz, 16384);
#endif
        }

        public void Dispose()
        {
#if OLD_LOADING
#else
            if (m_buffer != null)
            {
                try { m_buffer.Close(); }
                finally { m_buffer = null; }

                try { m_gz.Close(); }
                finally { m_gz = null; }

                try { m_input.Close(); }
                finally { m_input = null; }
            }
#endif
        }

        //  Reads value (int, float, ...) from decompressed buffer
        public int GetInt32()
        {
#if OLD_LOADING
            int ret = BitConverter.ToInt32(m_decompressed, m_readIndex);
            m_readIndex += sizeof(Int32);
#else
            m_buffer.Read(m_intBytesBuffer, 0, sizeof(Int32));
            int ret = BitConverter.ToInt32(m_intBytesBuffer, 0);
#endif
            return ret;
        }

        //  Reads value (int, float, ...) from decompressed buffer
        public byte GetByte()
        {
#if OLD_LOADING
            byte ret = m_decompressed[m_readIndex];
            m_readIndex += sizeof(byte);
#else
            byte ret = (byte)m_buffer.ReadByte();
#endif
            return ret;
        }

        //  Copy raw bytes
        public int GetBytes(int bytes, byte[] output)
        {
#if OLD_LOADING
            System.Buffer.BlockCopy(m_decompressed, m_readIndex, output, 0, bytes);
            m_readIndex += bytes;
#else
            return m_buffer.Read(output, 0, bytes);
#endif
        }

        //  Signalizes if we haven't reached the end of un-compressed file by series of Get***() calls.
        public bool EndOfFile()
        {
#if OLD_LOADING
            return m_readIndex >= m_decompressed.Length;
#else
            return m_input.Position == m_input.Length;
#endif
        }


        public byte[] GetCompressedBuffer()
        {
            m_input.Position = 0;
            byte[] buffer = new byte[m_input.Length];
            m_input.Read(buffer, 0, (int)m_input.Length);
            return buffer;
        }
    }
}
