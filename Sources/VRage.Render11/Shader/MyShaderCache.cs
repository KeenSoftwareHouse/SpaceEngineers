using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using VRage.FileSystem;
using VRageRender;

namespace VRage.Render11.Shader
{
    internal class MyShaderCache
    {
        private static readonly MD5 m_md5 = MD5.Create();

        static MyShaderCache()
        {
            Directory.CreateDirectory(Path.Combine(MyFileSystem.UserDataPath, MyShadersDefines.CachePath));
        }

        internal static MyShaderIdentity ComputeShaderIdentity(string source, MyShaderProfile profile)
        {
            var bytes = Encoding.UTF8.GetBytes(source);
            byte[] compressed;
            using (var ms = new MemoryStream())
            {
                using (var gz = new GZipStream(ms, CompressionMode.Compress))
                {
                    gz.Write(bytes, 0, bytes.Length);
                }
                compressed = ms.ToArray();
            }

            byte[] hash = m_md5.ComputeHash(bytes);


            var builder = new StringBuilder(hash.Length);
            for (int i = 0; i < hash.Length; i++)
            {
                builder.Append(hash[i].ToString("X2"));
            }

            return new MyShaderIdentity(builder.ToString(), compressed, profile);
        }

        internal static bool TryFetch(MyShaderIdentity identity, out byte[] cache)
        {
            bool found;
            found = TryFetch(identity, MyFileSystem.ContentPath, out cache);
            if (found)
                return true;

            found = TryFetch(identity, MyFileSystem.UserDataPath, out cache);
            if (found)
                return true;

            return false;
        }

        private static bool TryFetch(MyShaderIdentity identity, string basePath, out byte[] cache)
        {
            cache = null;
            var sourceFilename = Path.Combine(basePath, MyShadersDefines.CachePath, identity.SourceFilename);
            var cacheFilename = Path.Combine(basePath, MyShadersDefines.CachePath, identity.CacheFilename);
            if (File.Exists(sourceFilename) && File.Exists(cacheFilename))
            {
                byte[] compressed = File.ReadAllBytes(sourceFilename);
                if (compressed.Compare(identity.Source))
                {
                    cache = File.ReadAllBytes(cacheFilename);
                    return true;
                }
                else
                {
                    // Non match should happen in case of I/O error or extremely unlikely occurrence
                    MyRender11.Log.WriteLine("Shader with hash " + identity.Hash + " didn't match.");
                    File.Delete(cacheFilename);
                }
            }

            return false;
        }

        internal static void Store(MyShaderIdentity identity, byte[] compiled)
        {
            // Store first the compiled code assuming no I/O error, so we can just check compressed source later
            string fileName = Path.Combine(MyFileSystem.UserDataPath, MyShadersDefines.CachePath, identity.CacheFilename);
            using (var writer = new FileStream(fileName, FileMode.CreateNew))
            {
                writer.Write(compiled, 0, compiled.Length);
            }

            fileName = Path.Combine(MyFileSystem.UserDataPath, MyShadersDefines.CachePath, identity.SourceFilename);
            if (!File.Exists(fileName))
            {
                using (var stream = new FileStream(fileName, FileMode.CreateNew))
                {
                    stream.Write(identity.Source, 0, identity.Source.Length);
                }
            }
        }
    }

    class MyShaderIdentity
    {
        public MyShaderIdentity(string hash, byte[] source, MyShaderProfile profile)
        {
            Hash = hash;
            Source = source;
            Profile = profile;
        }

        /// <summary>Compressed source bytes</summary>
        public byte[] Source { get; private set; }

        /// <summary>Compressed source hash</summary>
        public string Hash { get; private set; }

        public MyShaderProfile Profile { get; private set; }

        public string SourceFilename
        {
            get { return Hash + ".hlsl.gz"; }
        }

        public string CacheFilename
        {
            get { return Hash + "." + MyShaders.ProfileToString(Profile) + ".cache"; }
        }
    }
}
