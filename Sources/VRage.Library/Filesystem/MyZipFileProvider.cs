using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
#if !XB1
using System.Text.RegularExpressions;
#endif // !XB1
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

            return TryDoZipAction(path, TryOpen, null);
        }

        T TryDoZipAction<T>(string path, Func<string, string, T> action, T defaultValue)
        {
            // This may need some optimization (allocations), but file open allocates itself, so probably not needed
            int currentPosition = path.Length;

            while (currentPosition >= 0)
            {
                string zipFile = path.Substring(0, currentPosition);
                if (File.Exists(zipFile))
                {
                    return action(zipFile, path.Substring(Math.Min(path.Length, currentPosition + 1)));
                }

                currentPosition = path.LastIndexOfAny(Separators, currentPosition - 1);
            }

            return defaultValue;
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
            return TryDoZipAction(path, DirectoryExistsInZip, false);
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
            MyZipArchive zipFile = TryDoZipAction(path, TryGetZipArchive, null);

            string subpath = "";

            if (searchOption == MySearchOption.TopDirectoryOnly)
            {
                subpath = TryDoZipAction(path, TryGetSubpath, null);
            }

            if (zipFile != null)
            {
#if XB1
                System.Diagnostics.Debug.Assert(false, "TODO for XB1.");
                if (filter == "hgdshfjghjdsghj") { yield return ""; }
#else // !XB1
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
#endif // !XB1

                zipFile.Dispose();
            }
        }

        public bool FileExists(string path)
        {
            return TryDoZipAction(path, FileExistsInZip, false);
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
    }
}
