using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Security.Cryptography;
using System.Collections;

namespace VRage.Utils
{
    public static class MySerialKey
    {
        private static int m_dataSize = 14;
        private static int m_hashSize = 4;

        public static string[] Generate(short productTypeId, short distributorId, int keyCount)
        {
            byte[] prodBytes = BitConverter.GetBytes(productTypeId);
            byte[] distBytes = BitConverter.GetBytes(distributorId);

            using (RNGCryptoServiceProvider rng = new RNGCryptoServiceProvider())
            using (var sha = new SHA1Managed())
            {
                List<string> result = new List<string>(keyCount);

                byte[] dataAndHash = new byte[m_dataSize + m_hashSize];

                for (int i = 0; i < keyCount; i++)
                {
                    rng.GetBytes(dataAndHash);
                    dataAndHash[0] = prodBytes[0];
                    dataAndHash[1] = prodBytes[1];
                    dataAndHash[2] = distBytes[0];
                    dataAndHash[3] = distBytes[1];

                    // XOR first 4 bytes (dist and prod id)...to hide
                    for (int x = 0; x < 4; x++)
                    {
                        dataAndHash[x] = (byte)(dataAndHash[x] ^ dataAndHash[x + 4]);
                    }

                    var hash = sha.ComputeHash(dataAndHash, 0, m_dataSize);

                    for (int j = 0; j < m_hashSize; j++)
                    {
                        dataAndHash[m_dataSize + j] = hash[j];
                    }

                    result.Add(new string(My5BitEncoding.Default.Encode(dataAndHash.ToArray())) + "X");
                }
                return result.ToArray();
            }
        }

        public static string AddDashes(string key)
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < key.Length; i++)
            {
                if (i % 5 == 0 && i > 0)
                {
                    sb.Append('-');
                }
                sb.Append(key[i]);
            }
            return sb.ToString();
        }

        public static string RemoveDashes(string key)
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < key.Length; i++)
            {
                if ((i + 1) % 6 != 0)
                {
                    sb.Append(key[i]);
                }
            }
            return sb.ToString();
        }

        public static bool ValidateSerial(string serialKey, out int productTypeId, out int distributorId)
        {
            using (RNGCryptoServiceProvider rng = new RNGCryptoServiceProvider())
            using (var sha = SHA1.Create())
            {
                if (serialKey.EndsWith("X"))
                {
                    byte[] dataAndHash = My5BitEncoding.Default.Decode(serialKey.Take(serialKey.Length - 1).ToArray());
                    byte[] data = dataAndHash.Take(dataAndHash.Length - m_hashSize).ToArray();
                    var hash = sha.ComputeHash(data);

                    if (dataAndHash.Skip(data.Length).Take(m_hashSize).SequenceEqual(hash.Take(m_hashSize)))
                    {
                        // XOR first 4 bytes (dist and prod id)...to show again
                        for (int x = 0; x < 4; x++)
                        {
                            data[x] = (byte)(data[x] ^ data[x + 4]);
                        }

                        productTypeId = BitConverter.ToInt16(data, 0);
                        distributorId = BitConverter.ToInt16(data, 2);
                        return true;
                    }
                }

                productTypeId = 0;
                distributorId = 0;
                return false;
            }
        }
    }
}
