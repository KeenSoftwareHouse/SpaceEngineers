using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.FileSystem;

namespace VRage.Filesystem
{
    class MyFileChecksumWatcher : IDisposable
    {
        public bool ChecksumFound { get; private set; }
        public bool ChecksumValid { get; private set; }

        public MyFileChecksumWatcher()
        {
            ChecksumFound = true;
            ChecksumValid = true;
            MyFileSystem.FileVerifier.ChecksumFailed += FileVerifier_ChecksumFailed;
            MyFileSystem.FileVerifier.ChecksumNotFound += FileVerifier_ChecksumNotFound;
        }

        public void Reset()
        {
            ChecksumValid = true;
            ChecksumFound = true;
        }

        void FileVerifier_ChecksumNotFound(VRage.FileSystem.IFileVerifier arg1, string arg2)
        {
            ChecksumFound = false;
            ChecksumValid = false;
        }

        void FileVerifier_ChecksumFailed(string arg1, string arg2)
        {
            ChecksumFound = true;
            ChecksumValid = false;
        }

        void IDisposable.Dispose()
        {
            MyFileSystem.FileVerifier.ChecksumFailed -= FileVerifier_ChecksumFailed;
            MyFileSystem.FileVerifier.ChecksumNotFound -= FileVerifier_ChecksumNotFound;
        }
    }
}
