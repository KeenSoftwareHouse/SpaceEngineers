#if !XB1

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using VRage.Cryptography;
using VRage.Security;

namespace VRage.Input
{
    public class MyKeyHasher
    {
        public List<MyKeys> Keys = new List<MyKeys>(10);
        public Md5.Hash Hash = new Md5.Hash();

        SHA256 m_hasher = MySHA256.Create();
        byte[] m_tmpHashData = new byte[256];

        public unsafe void ComputeHash(string salt)
        {
            Keys.Sort(EnumComparer<MyKeys>.Instance);
            int index = 0;
            foreach (var key in Keys)
            {
                m_tmpHashData[index++] = (byte)key;
            }
            foreach (var c in salt)
            {
                m_tmpHashData[index++] = (byte)c;
                m_tmpHashData[index++] = (byte)(c >> 8);
            }
            Md5.ComputeHash(m_tmpHashData, Hash);
        }

        static byte HexToByte(char c)
        {
            if (c >= 'a') return (byte)(10 + c - 'a');
            else if (c >= 'A') return (byte)(10 + c - 'A');
            else return (byte)(c - '0');
        }

        static byte HexToByte(char c1, char c2)
        {
            return (byte)(HexToByte(c1) * 16 + HexToByte(c2));
        }

        public unsafe bool TestHash(string hash, string salt)
        {
            uint* data = stackalloc uint[4];
            for (int i = 0; i < Math.Min(hash.Length, 32) / 2; i++)
            {
                ((byte*)data)[i] = HexToByte(hash[i * 2], hash[i * 2 + 1]);
            }
            return TestHash(data[0], data[1], data[2], data[3], salt);
        }

        public bool TestHash(uint h0, uint h1, uint h2, uint h3, string salt)
        {
            ComputeHash(salt);
            return Hash.A == h0 && Hash.B == h1 && Hash.C == h2 && Hash.D == h3;
        }
    }
}

#endif
