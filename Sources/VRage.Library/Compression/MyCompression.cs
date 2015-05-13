using System;
using System.IO;
using System.IO.Compression;

//  Static class for compression and decompression of byte arrays. Uses .NET's GZipStream and result are 70-80% of RAR's compression ratios.
//  After compression, the result contains length of original data (not-compressed) at array's beginning. It's used for decompression.

namespace VRage
{
    public static class MyCompression
    {
        public static byte[] Compress(byte[] buffer)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                using (GZipStream zip = new GZipStream(ms, CompressionMode.Compress, true))
                {
                    zip.Write(buffer, 0, buffer.Length);
                    zip.Close();
                    ms.Position = 0;

                    byte[] compressed = new byte[ms.Length + 4];
                    ms.Read(compressed, 4, (int) ms.Length);
                    Buffer.BlockCopy(BitConverter.GetBytes(buffer.Length), 0, compressed, 0, 4);

                    return compressed;
                }
            }
        }

        static byte[] m_buffer = new byte[16384];

        public static void CompressFile(string fileName)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                FileInfo f = new FileInfo(fileName);
                Buffer.BlockCopy(BitConverter.GetBytes(f.Length), 0, m_buffer, 0, 4);
                ms.Write(m_buffer, 0, 4); //writing length because of backward compatibility

                using (GZipStream zip = new GZipStream(ms, CompressionMode.Compress, true))
                {
                    using (FileStream fs = File.OpenRead(fileName))
                    {
                        int bytesRead = fs.Read(m_buffer, 0, m_buffer.Length);
                        while (bytesRead > 0)
                        {
                            zip.Write(m_buffer, 0, bytesRead);
                            bytesRead = fs.Read(m_buffer, 0, m_buffer.Length);
                        }
                    }

                    zip.Close();

                    ms.Position = 0;

                    using (FileStream fs = File.Create(fileName))
                    {
                        int bytesRead = ms.Read(m_buffer, 0, m_buffer.Length);
                        while (bytesRead > 0)
                        {
                            fs.Write(m_buffer, 0, bytesRead);
                            fs.Flush();
                            bytesRead = ms.Read(m_buffer, 0, m_buffer.Length);
                        }
                    }
                }
            }
        }

        public static byte[] Decompress(byte[] gzBuffer)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                int msgLength = BitConverter.ToInt32(gzBuffer, 0);
                ms.Write(gzBuffer, 4, gzBuffer.Length - 4);
                ms.Position = 0;

                byte[] buffer = new byte[msgLength];

                using (GZipStream zip = new GZipStream(ms, CompressionMode.Decompress))
                {
                    zip.Read(buffer, 0, buffer.Length);

                    return buffer;
                }
            }
        }

        public static void DecompressFile(string fileName)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                using (FileStream fs = File.OpenRead(fileName))
                {
                    fs.Read(m_buffer, 0, 4); //reading length because of backward compatibility

                    using (GZipStream zip = new GZipStream(fs, CompressionMode.Decompress))
                    {
                        int readBytes = zip.Read(m_buffer, 0, m_buffer.Length);
                        while (readBytes > 0)
                        {
                            ms.Write(m_buffer, 0, readBytes);
                            readBytes = zip.Read(m_buffer, 0, m_buffer.Length);
                        }
                    }
                }

                ms.Position = 0;

                using (FileStream fs = File.Create(fileName))
                {
                    int bytesRead = ms.Read(m_buffer, 0, m_buffer.Length);
                    while (bytesRead > 0)
                    {
                        fs.Write(m_buffer, 0, bytesRead);
                        fs.Flush();
                        bytesRead = ms.Read(m_buffer, 0, m_buffer.Length);
                    }
                }
            }        
        }
    }
}
