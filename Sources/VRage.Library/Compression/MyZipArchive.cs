using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using VRage.Reflection;

namespace VRage.Compression
{
    public enum CompressionMethodEnum : ushort
    {
        Stored = (ushort)0,
        Deflated = (ushort)8,
    }

    public enum DeflateOptionEnum : byte
    {
        Normal = (byte)0,
        Maximum = (byte)2,
        Fast = (byte)4,
        SuperFast = (byte)6,
        None = (byte)255,
    }

    /// <summary>
    /// Class based on http://www.codeproject.com/Articles/209731/Csharp-use-Zip-archives-without-external-libraries.
    /// </summary>
    public class MyZipArchive : IDisposable
    {
        public struct Enumerator : IEnumerator<MyZipFileInfo>, IEnumerable<MyZipFileInfo>
        {
            public IEnumerator m_enumerator;

            public Enumerator(IEnumerator enumerator) { m_enumerator = enumerator; }
            public MyZipFileInfo Current { get { return new MyZipFileInfo(m_enumerator.Current); } }
            public bool MoveNext() { return m_enumerator.MoveNext(); }
            public void Reset() { m_enumerator.Reset(); }
            object System.Collections.IEnumerator.Current { get { return Current; } }
            void IDisposable.Dispose() { }
            public IEnumerator<MyZipFileInfo> GetEnumerator() { return this; }
            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { return GetEnumerator(); }
        }
        
        private object m_zip;
        public string ZipPath { get; private set; }

        Dictionary<string, string> m_mixedCaseHelper = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);

        private MyZipArchive(object zipObject, string path = null)
        {
            m_zip = zipObject;
            ZipPath = path;
            
            foreach (var file in Files)
            {
                m_mixedCaseHelper[file.Name.Replace('/','\\')] = file.Name;
            }
        }

        public IEnumerable<string> FileNames
        {
            get { return Files.Select(p => p.Name).OrderBy(p => p); }
        }

        public Enumerator Files
        {
            get
            {
                return new Enumerator(((IEnumerable)MyZipArchiveReflection.GetFiles(m_zip)).GetEnumerator());
            }
        }

        private static void FixName(ref string name)
        {
            name = name.Replace('/', '\\');
        }
        
        public static MyZipArchive OpenOnFile(string path, FileMode mode = FileMode.Open, FileAccess access = FileAccess.Read, FileShare share = FileShare.Read, bool streaming = false)
        {
            return new MyZipArchive(MyZipArchiveReflection.OpenOnFile(path, mode, access, share, streaming), path);
        }

        public static MyZipArchive OpenOnStream(Stream stream, FileMode mode = FileMode.OpenOrCreate, FileAccess access = FileAccess.ReadWrite, bool streaming = false)
        {
            return new MyZipArchive(MyZipArchiveReflection.OpenOnStream(stream, mode, access, streaming));
        }

        public MyZipFileInfo AddFile(string path, CompressionMethodEnum compressionMethod = CompressionMethodEnum.Deflated, DeflateOptionEnum deflateOption = DeflateOptionEnum.Normal)
        {
            return new MyZipFileInfo(MyZipArchiveReflection.AddFile(m_zip, path, (ushort)compressionMethod, (byte)deflateOption));
        }

        public void DeleteFile(string name)
        {
            FixName(ref name);
            MyZipArchiveReflection.DeleteFile(m_zip, name);
        }

        public MyZipFileInfo GetFile(string name)
        {
            FixName(ref name);
            return new MyZipFileInfo(MyZipArchiveReflection.GetFile(m_zip, m_mixedCaseHelper[name]));
        }

        public bool FileExists(string name)
        {
            FixName(ref name);
            return m_mixedCaseHelper.ContainsKey(name);
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

        public void Dispose()
        {
            ((IDisposable)m_zip).Dispose();
        }

        public static bool IsHidden(FileInfo f, DirectoryInfo relativeTo)
        {
            if ((f.Attributes & FileAttributes.Hidden) != 0)
                return true;

            // Check parent directories until you reach desired dir
            DirectoryInfo current = f.Directory;
            while (!current.FullName.Equals(relativeTo.FullName, StringComparison.InvariantCultureIgnoreCase))
            {
                if((current.Attributes & FileAttributes.Hidden) != 0)
                    return true;
                current = current.Parent;
            }
            return false;
        }

        public static void CreateFromDirectory(string sourceDirectoryName, string destinationArchiveFileName, DeflateOptionEnum compressionLevel, bool includeBaseDirectory, string[] ignoredExtensions = null, bool includeHidden = true)
        {
            // Some bug in ZipArchive throws exception when file already exists and we try to create it.
            if (File.Exists(destinationArchiveFileName))
            {
                File.Delete(destinationArchiveFileName);
            }

            int len = sourceDirectoryName.Length + 1;

            using (var arc = MyZipArchive.OpenOnFile(destinationArchiveFileName, FileMode.Create, FileAccess.ReadWrite, FileShare.None))
            {
                var rootDir = new DirectoryInfo(sourceDirectoryName);
                foreach(var fileInfo in rootDir.GetFiles("*", SearchOption.AllDirectories))
                {
                    if (!includeHidden && IsHidden(fileInfo, rootDir))
                    {
                        continue;
                    }

                    var file = fileInfo.FullName;
                    if (ignoredExtensions != null && ignoredExtensions.Contains(Path.GetExtension(file), StringComparer.InvariantCultureIgnoreCase))
                        continue;
                    using (var inStream = File.Open(file, FileMode.Open, FileAccess.Read, FileShare.Read))
                    {
                        using (var outStream = arc.AddFile(file.Substring(len), CompressionMethodEnum.Deflated, compressionLevel).GetStream(FileMode.Open, FileAccess.Write))
                        {
                            inStream.CopyTo(outStream, 0x1000);
                        }
                    }
                }
            }
        }

        public static void ExtractToDirectory(string sourceArchiveFileName, string destinationDirectoryName)
        {
            if (!Directory.Exists(destinationDirectoryName))
            {
                Directory.CreateDirectory(destinationDirectoryName);
            }

            using (var arc = MyZipArchive.OpenOnFile(sourceArchiveFileName, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                foreach (var fileName in arc.FileNames)
                {
                    using (var inStream = arc.GetFile(fileName).GetStream(FileMode.Open, FileAccess.Read))
                    {
                        var fullFilePath = Path.Combine(destinationDirectoryName, fileName);
                        var fullDirectoryPath = Path.GetDirectoryName(fullFilePath);
                        if (!Directory.Exists(fullDirectoryPath))
                            Directory.CreateDirectory(fullDirectoryPath);

                        using (var outStream = File.Open(fullFilePath, FileMode.Create, FileAccess.Write))
                        {
                            inStream.CopyTo(outStream, 0x1000);
                        }
                    }
                }
            }
        }
    }
}
