//#define OLD_SAVING
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;

namespace VRage
{
    public class MyCompressionFileSave : IMyCompressionSave 
    {
#if OLD_SAVING
        string m_targetFile;
        FileStream m_notCompressed;
#else
        private int m_uncompressedSize;
        private FileStream m_output;
        private GZipStream m_gz;
        private BufferedStream m_buffer;
#endif

        public MyCompressionFileSave(string targetFile)
        {
#if OLD_SAVING
            m_targetFile = targetFile;
            m_notCompressed = new FileStream(targetFile, FileMode.Create, FileAccess.Write);
#else
            m_output = new FileStream(targetFile, FileMode.Create, FileAccess.Write);
            // Prepare space for size at the beginning. Will be replaced by size of uncompressed data written.
            for (int i = 0; i < sizeof(int); ++i)
                m_output.WriteByte(0);
            m_gz     = new GZipStream(m_output, CompressionMode.Compress, leaveOpen: true);
            m_buffer = new BufferedStream(m_gz, 16384);
#endif
        }

        public void Dispose()
        {
#if OLD_SAVING
            if (m_notCompressed != null)
            {
                m_notCompressed.Dispose();
                m_notCompressed = null;
                MyCompression.CompressFile(m_targetFile);
            }
#else
            if (m_output != null)
            {
                try { m_buffer.Close(); }
                finally { m_buffer = null; }

                try { m_gz.Close(); }
                finally { m_gz = null; }

                m_output.Position = 0;
                WriteUncompressedSize(m_output, m_uncompressedSize);

                try { m_output.Close(); }
                finally { m_output = null; }
            }
#endif
        }

        //  Add value to byte array (float, int, string, etc)
        public void Add(byte[] value)
        {
            Add(value, value.Length);
        }

        // Add count bytes from value to byte array.
        public void Add(byte[] value, int count)
        {
#if OLD_SAVING
            m_notCompressed.Write(value, 0, count);
#else
            m_buffer.Write(value, 0, count);
            m_uncompressedSize += count;
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
            m_uncompressedSize += 1;
#endif
        }

        private static unsafe void WriteUncompressedSize(FileStream output, int uncompressedSize)
        {
            byte* pBytes = (byte*)&uncompressedSize;
            for (int i = 0; i < sizeof(int); ++i)
                output.WriteByte(pBytes[i]);
        }

    }
}
