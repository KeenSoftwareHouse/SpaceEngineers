using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace VRage.FileSystem
{
    public class MyClassicFileProvider : IFileProvider
    {
        public Stream Open(string path, FileMode mode, FileAccess access, FileShare share)
        {
            if (!File.Exists(path))
                return null;

            try
            {
                return File.Open(path, mode, access, share);
            }
            catch(Exception)
            {
                Debug.Fail(String.Format("File exists, but cannot be opened: {0}, {1}, {2}, {3}", path, mode, access, share));
                return null;
            }
        }

        public bool DirectoryExists(string path)
        {
            return Directory.Exists(path);
        }

        public IEnumerable<string> GetFiles(string path, string filter, MySearchOption searchOption)
        {
            if (!Directory.Exists(path))
                return null;

//NotYet #if XB1
//NotYet            System.Diagnostics.Debug.Assert(false, "TODO for XB1.");
//NotYet            return null;
//NotYet #else // !XB1
            return Directory.GetFiles(path, filter, (System.IO.SearchOption)searchOption);
//NotYet #endif // !XB1
        }

        public bool FileExists(string path)
        {
            return File.Exists(path);
        }
    }
}
