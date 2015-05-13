using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace VRage.FileSystem
{
    public interface IFileVerifier
    {
        event Action<IFileVerifier, string> ChecksumNotFound;
        event Action<string, string> ChecksumFailed;

        Stream Verify(string filename, Stream stream);
    }

    public static class MyFileVerifierExtensions
    {
        public static Stream Verify(this IFileVerifier verifier, string path, Stream stream)
        {
            return verifier.Verify(path, stream);
        }
    }
}
