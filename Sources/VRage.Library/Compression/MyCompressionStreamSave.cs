//#define OLD_SAVING
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;

namespace VRage
{
    public class MyCompressionStreamSave : IMyCompressionSave
    {
#if OLD_SAVING
        MemoryStream m_notCompressed;
#else
        private MemoryStream m_output;
        private GZipStream m_gz;
        private BufferedStream m_buffer;
#endif

        public MyCompressionStreamSave()
        {
#if OLD_SAVING
            m_notCompressed = new MemoryStream();
#else
            m_output = new MemoryStream();
            m_output.Write(BitConverter.GetBytes(0), 0, 4); // Write 4 zero bytes. This was originally used for size of the file.
            m_gz     = new GZipStream(m_output, CompressionMode.Compress);
            m_buffer = new BufferedStream(m_gz, 16384);
#endif
        }

        public byte[] Compress()
        {
#if OLD_SAVING
            return MyCompression.Compress(m_notCompressed.GetBuffer());
#else
            byte[] data = null;
            if (m_output != null)
            {
                try { m_buffer.Close(); }
                finally { m_buffer = null; }

                try { m_gz.Close(); }
                finally { m_gz = null; }

                try { m_output.Close(); }
                finally
                {
                    data = m_output.ToArray();
                    m_output = null;
                }
            }

            return data;
#endif
        }

        public void Dispose()
        {
#if OLD_SAVING
            if (m_notCompressed != null)
            {
                m_notCompressed.Dispose();
                m_notCompressed = null;
            }
#else
            Compress();
#endif
        }

        //  Add value to byte array (float, int, string, etc)
        public void Add(byte[] value)
        {
#if OLD_SAVING
            m_notCompressed.Write(value, 0, value.Length);
#else
            m_buffer.Write(value, 0, value.Length);
#endif
        }

        // Add count bytes from value to byte array.
        public void Add(byte[] value, int count)
        {
#if OLD_SAVING
            m_notCompressed.Write(value, 0, count);
#else
            m_buffer.Write(value, 0, count);
#endif
        }

        //  Add value to byte array (float, int, string, etc)
        public void Add(float value)
        {
            Add(BitConverter.GetBytes(value));
        }

        //  Add value to byte array (float, int, string, etc)
        public void Add(int value)
        {
            Add(BitConverter.GetBytes(value));
        }

        //  Add value to byte array (float, int, string, etc)
        public void Add(byte value)
        {
#if OLD_SAVING
            m_notCompressed.WriteByte(value);
#else
            m_buffer.WriteByte(value);
#endif
        }
    }
}
