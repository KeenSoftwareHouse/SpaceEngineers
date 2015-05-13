using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using VRage.Common.Utils;

namespace VRage.FileSystem
{
    public class MyChecksumVerifier : IFileVerifier
    {
        public readonly string BaseChecksumDir;
        public readonly byte[] PublicKey;

        Dictionary<string, string> m_checksums;

        public event Action<IFileVerifier, string> ChecksumNotFound;
        public event Action<string, string> ChecksumFailed;

        public MyChecksumVerifier(MyChecksums checksums, string baseChecksumDir)
        {
            PublicKey = checksums.PublicKeyAsArray;
            BaseChecksumDir = baseChecksumDir;
            m_checksums = checksums.Items.Dictionary;
        }

        public Stream Verify(string filename, Stream stream)
        {
            var failed = ChecksumFailed;
            var notFound = ChecksumNotFound;
            if ((failed != null || notFound != null) && filename.StartsWith(BaseChecksumDir, StringComparison.InvariantCultureIgnoreCase))
            {
                string relativePath = filename.Substring(BaseChecksumDir.Length + 1);
                string checksum;
                if (m_checksums.TryGetValue(relativePath, out checksum))
                {
                    if (failed != null)
                    {
                        return new MyCheckSumStream(stream, filename, Convert.FromBase64String(checksum), PublicKey, failed);
                    }
                }
                else if(notFound != null)
                {
                    notFound(this, filename);
                }
            }
            return stream;
        }
    }
}
