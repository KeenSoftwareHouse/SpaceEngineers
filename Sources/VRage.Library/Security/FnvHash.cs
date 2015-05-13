using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VRage.Security
{
    public static class FnvHash
    {
        const uint InitialFNV = 2166136261U;
        const uint FNVMultiple = 16777619;

        /* Fowler / Noll / Vo (FNV) Hash */
        public static uint Compute(string s)
        {
            unchecked
            {
                uint hash = InitialFNV;
                for (int i = 0; i < s.Length; i++)
                {
                    hash = hash ^ (s[i]);       /* xor  the low 8 bits */
                    hash = hash * FNVMultiple;  /* multiply by the magic number */
                }
                return hash;
            }
        }

        public static uint ComputeAscii(string s)
        {
            unchecked
            {
                uint hash = InitialFNV;
                for (int i = 0; i < s.Length; i++)
                {
                    hash = hash ^ ((byte)(s[i]));       /* xor  the low 8 bits */
                    hash = hash * FNVMultiple;  /* multiply by the magic number */
                }
                return hash;
            }
        }
    }
}
