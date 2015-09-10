using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using VRage.Compression;

namespace VRage.FileSystem
{
    public class MyZipFileProvider : IFileProvider
    {
        public readonly char[] Separators = new char[] { Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar };

        /// <summary>
        /// FileShare is ignored
        /// Usage: C:\Users\Data\Archive.zip\InnerFolder\file.txt
        /// </summary>
        public Stream Open(string path, FileMode mode, FileAccess access, FileShare share)
        {
            // Zip file cannot write anything
            if (mode != FileMode.Open || access != FileAccess.Read)
                return null;

            var zipPath = SplitZipFilePath(ref path);
            if(zipPath == null) return null;
            return TryOpen(zipPath, path);
        }
        
        /// <summary>
        /// Given a path like: C:\Users\Data\Archive.zip\InnerFolder\file.txt
        /// this method returns C:\Users\Data\Archive.zip and modifies its filePath parameter to point to InnerFolder\file.txt
        /// Returns null and leaves filePath unchanged if the zip file cannot be identified.
        /// </summary>
        string SplitZipFilePath(ref string filePath)
        {
            // This may need some optimization (allocations), but file open allocates itself, so probably not needed
            int currentPosition = filePath.Length;

            while (currentPosition >= 0)
            {
                string zipFile = filePath.Substring(0, currentPosition);
                if (File.Exists(zipFile))
                {
                    filePath = filePath.Substring(Math.Min(filePath.Length, currentPosition + 1));
                    return zipFile;
                }

                currentPosition = filePath.LastIndexOfAny(Separators, currentPosition - 1);
            }

            return null;
        }

        private Stream TryOpen(string zipFile, string subpath)
        {
            var arc = MyZipArchive.OpenOnFile(zipFile);
            try
            {
                return arc.FileExists(subpath) ? new MyStreamWrapper(arc.GetFile(subpath).GetStream(), arc) : null;
            }
            catch
            {
                arc.Dispose();
                return null;
            }
        }

        public bool DirectoryExists(string path)
        {
            var zipPath = SplitZipFilePath(ref path);
            if(zipPath == null) return false;
            
            return DirectoryExistsInZip(zipPath, path);
        }


        bool DirectoryExistsInZip(string zipFile, string subpath)
        {
            var arc = MyZipArchive.OpenOnFile(zipFile);
            try
            {
                // Root exists when archive can be opened
                return subpath == String.Empty ? true : arc.DirectoryExists(subpath + "/");
            }
            finally
            {
                arc.Dispose();
            }
        }


        private MyZipArchive TryGetZipArchive(string zipFile, string subpath)
        {
            var arc = MyZipArchive.OpenOnFile(zipFile);
            try
            {
                return arc;
            }
            catch
            {
                arc.Dispose();
                return null;
            }
        }


        private string TryGetSubpath(string zipFile, string subpath)
        {
            return subpath;
        }

        public IEnumerable<string> GetFiles(string path, string filter, MySearchOption searchOption)
        {
            var zipPath = SplitZipFilePath(ref path);
            if(zipPath == null) yield break;

            MyZipArchive zipFile = TryGetZipArchive(zipPath, path);
            if (zipFile == null) yield break;
                
            string subpath = path;

            string pattern = Regex.Escape(filter).Replace(@"\*", ".*").Replace(@"\?", ".");
            pattern += "$";
            foreach (var fileName in zipFile.FileNames)
            {
                if (searchOption == MySearchOption.TopDirectoryOnly)
                {
                    if (fileName.Count((x) => x == '\\') != subpath.Count((x) => x == '\\') + 1)
                    {
                        continue;
                    }
                }
                if (Regex.IsMatch(fileName, pattern, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
                    yield return Path.Combine(zipFile.ZipPath, fileName);
            }

            zipFile.Dispose();
        }

        public bool FileExists(string path)
        {
            var zipPath = SplitZipFilePath(ref path);
            if(zipPath == null) return false;
            
            return FileExistsInZip(zipPath, path);
        }

        bool FileExistsInZip(string zipFile, string subpath)
        {
            var arc = MyZipArchive.OpenOnFile(zipFile);
            try
            {
                return arc.FileExists(subpath);
            }
            finally
            {
                arc.Dispose();
            }
        }

        public static bool IsZipFile(string path)
        {
            return !Directory.Exists(path);
        }




        private int m_cachingTokens = 0;
        private bool CanCache()
        {
            return m_cachingTokens > 0;
        }

        public IDisposable EnableCaching()
        {
            return new CachingToken(this);
        }

        private void ClearCaches()
        {
        }

        class CachingToken : IDisposable
        {
            private bool m_disposed;
            private MyZipFileProvider m_provider;
            public CachingToken(MyZipFileProvider provider)
            {
                m_provider = provider;
                Interlocked.Increment(ref m_provider.m_cachingTokens);
            }

            public void Dispose()
            {
                if(!m_disposed)
                {
                    if(Interlocked.Decrement(ref m_provider.m_cachingTokens) == 0) m_provider.ClearCaches();
                    m_disposed = true;
                }
            }
        }
    }
}
