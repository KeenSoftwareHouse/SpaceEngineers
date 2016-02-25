using System.IO;
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

        internal static string CalculateKey(string source, string function, string profile)
        {
            StringBuilder builder = new StringBuilder();
            builder.Append(function);
            builder.Append(profile);
            builder.Append(source);

            byte[] inputBytes = Encoding.ASCII.GetBytes(builder.ToString());
            byte[] hash = m_md5.ComputeHash(inputBytes);

            builder.Clear();

            for (int i = 0; i < hash.Length; i++)
            {
                builder.Append(hash[i].ToString("X2"));
            }
            return builder.ToString();
        }

        internal static byte[] TryFetch(string key)
        {
            if (key == null)
                return null;

            var filename = Path.Combine(MyFileSystem.ContentPath, MyShadersDefines.CachePath, Path.GetFileName(key + ".cache"));
            if (File.Exists(filename))
                return File.ReadAllBytes(filename);

            filename = Path.Combine(MyFileSystem.UserDataPath, MyShadersDefines.CachePath, Path.GetFileName(key + ".cache"));
            if (File.Exists(filename))
                return File.ReadAllBytes(filename);

            return null;
        }

        internal static void Store(string key, byte[] value)
        {
            if (key == null)
                return;

            using (var writer = new BinaryWriter(File.OpenWrite(Path.Combine(MyFileSystem.UserDataPath, MyShadersDefines.CachePath, Path.GetFileName(key + ".cache")))))
            {
                writer.Write(value);
            }
        }
    }
}
