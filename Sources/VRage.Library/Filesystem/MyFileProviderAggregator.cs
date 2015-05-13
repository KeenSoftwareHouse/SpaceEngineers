using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using VRage.Collections;

namespace VRage.FileSystem
{
    public class MyFileProviderAggregator : IFileProvider
    {
        HashSet<IFileProvider> m_providers = new HashSet<IFileProvider>();

        public MyFileProviderAggregator(params IFileProvider[] providers)
        {
            foreach (var p in providers)
                AddProvider(p);
        }

        public void AddProvider(IFileProvider provider)
        {
            m_providers.Add(provider);
        }

        public void RemoveProvider(IFileProvider provider)
        {
            m_providers.Remove(provider);
        }

        public HashSetReader<IFileProvider> Providers
        {
            get { return new HashSetReader<IFileProvider>(m_providers); }
        }

        public Stream OpenRead(string path)
        {
            return Open(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        }

        public Stream OpenWrite(string path, FileMode mode = FileMode.OpenOrCreate)
        {
            return Open(path, mode, FileAccess.Write, FileShare.Read);
        }

        /// <summary>
        /// Opens file, returns null when file does not exists or cannot be opened
        /// </summary>
        public Stream Open(string path, FileMode mode, FileAccess access, FileShare share)
        {
            foreach (var p in m_providers)
            {
                try
                {
                    var stream = p.Open(path, mode, access, share);
                    if (stream != null)
                        return stream;
                }
                catch { }
            }
            return null;
        }

        public bool DirectoryExists(string path)
        {
            foreach (var p in m_providers)
            {
                try
                {
                    if (p.DirectoryExists(path))
                        return true;
                }
                catch { }
            }
            return false;
        }

        public IEnumerable<string> GetFiles(string path, string filter, MySearchOption searchOption)
        {
            foreach (var p in m_providers)
            {
                try
                {
                    var files = p.GetFiles(path, filter, searchOption);
                    if (files != null)
                        return files;
                }
                catch { }

            }
            return null;
        }

        public bool FileExists(string path)
        {
            foreach (var p in m_providers)
            {
                try
                {
                    if (p.FileExists(path))
                        return true;
                }
                catch { }
            }
            return false;
        }

    }
}
