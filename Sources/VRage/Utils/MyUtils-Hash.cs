using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VRage.Utils
{
    public static partial class MyUtils
    {//FNV-1a hash alg.
        const int HashSeed = -2128831035; // 0x811C9DC5

        private static int HashStep(int value, int hash)
        {
            hash = hash ^ value;
            hash *= 16777619;
            return hash;
        }

        public unsafe static int GetHash(double d, int hash = HashSeed)
        {
            if (d == 0)
                return hash;//both positive and negative zeros go to same hash
            UInt64 value = *(UInt64*)(&d);
            unchecked
            {
                hash = HashStep((int)value, HashStep((int)(value >> 32), hash));
            }
            return hash; 
        }

        public static int GetHash(string str, int hash = HashSeed)
        {
            //two chars per int32
            if (str != null)
            {
                int i = 0;
                for (; i < str.Length - 1; i += 2)
                {
                    hash = HashStep(((int)str[i] << 16) + (int)str[i + 1], hash);
                }
                if ((str.Length & 1) != 0)
                {//last char
                    hash = HashStep((int)str[i], hash);
                }
            }
            return hash;
        }

        //public static Int32 GetHash(string text, Int32 seed = 0)
        //{
        //    int hash = seed;
        //    unchecked
        //    {
        //        if (text != null)
        //        {
        //            foreach (char c in text)
        //            {
        //                hash = hash * 37 + c;
        //            }
        //        }
        //        return hash;
        //    }
        //}
    }
}

