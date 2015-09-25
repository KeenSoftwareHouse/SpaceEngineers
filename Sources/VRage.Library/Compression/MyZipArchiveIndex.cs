using System;
using System.Collections.Generic;
using System.Linq;

namespace VRage.Compression
{
    public class MyZipArchiveIndex
    {
        private Dictionary<string, string> m_mixedCaseHelper = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);

        public string ZipPath { get; private set; }

        public MyZipArchiveIndex(string zipPath, IEnumerable<MyZipFileInfo> files)
        {
            ZipPath = zipPath;
            foreach(var file in files)
            {
                var name = file.Name;
                FixName(ref name);
                m_mixedCaseHelper[name] = file.Name;
            }
        }

        private static void FixName(ref string name)
        {
            name = name.Replace('/', '\\');
        }

        public string GetOriginalName(string name)
        {
            FixName(ref name);
            return m_mixedCaseHelper[name];
        }

        public bool FileExists(string name)
        {
            FixName(ref name);
            return m_mixedCaseHelper.ContainsKey(name);
        }

        public IEnumerable<string> FileNames
        {
            get { return m_mixedCaseHelper.Values.OrderBy(p => p); }
        }

        public bool DirectoryExists(string name)
        {
            FixName(ref name);
            foreach (var fileName in m_mixedCaseHelper.Keys)
            {
                if (fileName.StartsWith(name, StringComparison.InvariantCultureIgnoreCase))
                    return true;
            }
            return false;
        }
    }
}