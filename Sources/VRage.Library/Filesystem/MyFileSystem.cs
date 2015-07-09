using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace VRage.FileSystem
{
    public static class MyFileSystem
    {
        public static readonly Assembly MainAssembly = Assembly.GetEntryAssembly() ?? Assembly.GetCallingAssembly();
        public static readonly string MainAssemblyName = MainAssembly.GetName().Name;
        public static string ExePath = new FileInfo(MainAssembly.Location).DirectoryName; // OM: Need to be able to alter this due to starting game from tools

        private static string m_contentPath;
        private static string m_modsPath;
        private static string m_userDataPath;
        private static string m_savesPath;

        public static string ContentPath { get { CheckInitialized();  return m_contentPath; } }
        public static string ModsPath { get { CheckInitialized(); return m_modsPath; } }
        public static string UserDataPath { get { CheckInitialized(); return m_userDataPath; } }        
        public static string SavesPath { get { CheckUserSpecificInitialized(); return m_savesPath; } }

        public static IFileVerifier FileVerifier = new MyNullVerifier();
        static MyFileProviderAggregator m_fileProvider = new MyFileProviderAggregator
            (
                new MyClassicFileProvider(),
                new MyZipFileProvider()
            );

        private static void CheckInitialized()
        {
            if (m_contentPath == null)
                throw new InvalidOperationException("Paths are not initialized, call 'Init'");
        }

        private static void CheckUserSpecificInitialized()
        {
            if (m_userDataPath == null)
                throw new InvalidOperationException("User specific path not initialized, call 'InitUserSpecific'");
        }

        public static void Init(string contentPath, string userData, string modDirName = "Mods")
        {
            if (m_contentPath != null)
                throw new InvalidOperationException("Paths already initialized");

            m_contentPath = contentPath;
            m_userDataPath = userData;
            m_modsPath = Path.Combine(m_userDataPath, modDirName);
            Directory.CreateDirectory(m_modsPath);
        }

        public static void InitUserSpecific(string userSpecificName, string saveDirName = "Saves")
        {
            CheckInitialized();

            if (m_savesPath != null)
                throw new InvalidOperationException("User specific paths already initialized");

            m_savesPath = Path.Combine(m_userDataPath, saveDirName, userSpecificName ?? String.Empty);

            Directory.CreateDirectory(m_savesPath);
        }

        public static void Reset()
        {
            m_contentPath = m_modsPath = m_userDataPath = m_savesPath = null;
        }
        
        public static Stream Open(string path, FileMode mode, FileAccess access, FileShare share)
        {
            // Verifier is enable only when opening files with mode Open and access Read or ReadWrite
            bool verify = (mode == FileMode.Open) && (access != FileAccess.Write);

            var stream = m_fileProvider.Open(path, mode, access, share);
            return verify && stream != null? FileVerifier.Verify(path, stream) : stream;
        }

        /// <summary>
        /// Opens file for reading
        /// </summary>
        public static Stream OpenRead(string path)
        {
            return Open(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        }

        /// <summary>
        /// Opens file for reading, convenient method with two paths to combine
        /// </summary>
        public static Stream OpenRead(string path, string subpath)
        {
            return OpenRead(Path.Combine(path, subpath));
        }

        /// <summary>
        /// Creates or overwrites existing file
        /// </summary>
        public static Stream OpenWrite(string path, FileMode mode = FileMode.Create)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            return File.Open(path, mode, FileAccess.Write, FileShare.Read);
        }

        /// <summary>
        /// Creates or overwrites existing file, convenient method with two paths to combine
        /// </summary>
        public static Stream OpenWrite(string path, string subpath, FileMode mode = FileMode.Create)
        {
            return OpenWrite(Path.Combine(path, subpath), mode);
        }

        /// <summary>
        /// Checks write access for file
        /// </summary>
        public static bool CheckFileWriteAccess(string path)
        {
            try
            {
                using (var stream = OpenWrite(path, FileMode.Append))
                    return true;
            }
            catch
            {
                return false;
            }
        }

        public static bool FileExists(string path)
        {
            return m_fileProvider.FileExists(path);
        }

        public static bool DirectoryExists(string path)
        {
            return m_fileProvider.DirectoryExists(path);
        }

        public static IEnumerable<string> GetFiles(string path, string filter = "*", VRage.FileSystem.MySearchOption searchOption = VRage.FileSystem.MySearchOption.AllDirectories)
        {
            return m_fileProvider.GetFiles(path, filter, searchOption);
        }
    }
}
