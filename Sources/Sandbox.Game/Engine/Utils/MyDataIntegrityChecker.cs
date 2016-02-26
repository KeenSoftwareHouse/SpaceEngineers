using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using VRage;
using VRage.FileSystem;
using VRage.Library.Utils;
using VRage.Utils;

namespace Sandbox.Engine.Utils
{
    static class MyDataIntegrityChecker
    {
        public const int HASH_SIZE = 20;

        static byte[] m_combinedData = new byte[HASH_SIZE * 2];
        static byte[] m_hash = new byte[HASH_SIZE];

        static StringBuilder m_stringBuilder = new StringBuilder(8);

        public static void ResetHash()
        {
            Array.Clear(m_hash, 0, HASH_SIZE);
        }

        public static void HashInFile(string fileName)
        {
            //MySandboxGame.Log.WriteLine("Hashing file " + fileName + " into data check hash");

            using (var file = MyFileSystem.OpenRead(fileName).UnwrapGZip())
            {
                HashInData(fileName.ToLower(), file);
            }

            MySandboxGame.Log.WriteLine(GetHashHex());
        }

        public static void HashInData(string dataName, Stream data)
        {
            using (HashAlgorithm hashAlg = new SHA1Managed())
            {
                byte[] dataHash = hashAlg.ComputeHash(data);
                byte[] nameHash = hashAlg.ComputeHash(System.Text.Encoding.Unicode.GetBytes(dataName.ToCharArray()));
                Debug.Assert(dataHash.Length == HASH_SIZE);
                Debug.Assert(nameHash.Length == HASH_SIZE);

                Array.Copy(dataHash, m_combinedData, HASH_SIZE);
                Array.Copy(nameHash, 0, m_combinedData, HASH_SIZE, HASH_SIZE);
                byte[] hash = hashAlg.ComputeHash(m_combinedData);

                for (int i = 0; i < HASH_SIZE; ++i)
                {
                    m_hash[i] ^= hash[i];
                }
            }
        }

        public static string GetHashHex()
        {
            uint result = 0x00;

            m_stringBuilder.Clear();
            foreach (byte b in m_hash)
            {
                m_stringBuilder.AppendFormat("{0:x2}", b);
                result += (uint)b;
            }

            Debug.Assert(result != 0, "Returned game data hash was all zeroes. This is VERY probably a problem.");

            return m_stringBuilder.ToString();
        }

        public static string GetHashBase64()
        {
            return Convert.ToBase64String(m_hash);
        }
    }
}
