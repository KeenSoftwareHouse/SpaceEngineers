using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VRage.Security
{
    public static class Md5
    {
        /// <summary>
        /// Represent digest with ABCD
        /// </summary>
        public class Hash
        {
            public uint A;
            public uint B;
            public uint C;
            public uint D;

            public override string ToString()
            {
                string st;
                st = ReverseByte(A).ToString("X8") +
                    ReverseByte(B).ToString("X8") +
                    ReverseByte(C).ToString("X8") +
                    ReverseByte(D).ToString("X8");
                return st;
            }

            public string ToLowerString()
            {
                string st;
                st = ReverseByte(A).ToString("x8") +
                    ReverseByte(B).ToString("x8") +
                    ReverseByte(C).ToString("x8") +
                    ReverseByte(D).ToString("x8");
                return st;
            }

            /// <summary>
            /// perform a ByteReversal on a number
            /// </summary>
            /// <param name="uiNumber">value to be reversed</param>
            /// <returns>reversed value</returns>
            public static uint ReverseByte(uint uiNumber)
            {
                return (((uiNumber & 0x000000ff) << 24) | (uiNumber >> 24) |
                        ((uiNumber & 0x00ff0000) >> 8) | ((uiNumber & 0x0000ff00) << 8));
            }
        }

        /// <summary>
        /// Left rotates the input word
        /// </summary>
        /// <param name="uiNumber">a value to be rotated</param>
        /// <param name="shift">no of bits to be rotated</param>
        /// <returns>the rotated value</returns>
        static uint RotateLeft(uint uiNumber, ushort shift)
        {
            return ((uiNumber >> (32 - shift)) | (uiNumber << shift));
        }

        /// <summary>
        /// lookup table 4294967296*sin(i)
        /// </summary>
        public static readonly uint[] T = new uint[64] 
			{	0xd76aa478,0xe8c7b756,0x242070db,0xc1bdceee,
				0xf57c0faf,0x4787c62a,0xa8304613,0xfd469501,
                0x698098d8,0x8b44f7af,0xffff5bb1,0x895cd7be,
                0x6b901122,0xfd987193,0xa679438e,0x49b40821,
				0xf61e2562,0xc040b340,0x265e5a51,0xe9b6c7aa,
                0xd62f105d,0x2441453,0xd8a1e681,0xe7d3fbc8,
                0x21e1cde6,0xc33707d6,0xf4d50d87,0x455a14ed,
				0xa9e3e905,0xfcefa3f8,0x676f02d9,0x8d2a4c8a,
                0xfffa3942,0x8771f681,0x6d9d6122,0xfde5380c,
                0xa4beea44,0x4bdecfa9,0xf6bb4b60,0xbebfbc70,
                0x289b7ec6,0xeaa127fa,0xd4ef3085,0x4881d05,
				0xd9d4d039,0xe6db99e5,0x1fa27cf8,0xc4ac5665,
                0xf4292244,0x432aff97,0xab9423a7,0xfc93a039,
                0x655b59c3,0x8f0ccc92,0xffeff47d,0x85845dd1,
                0x6fa87e4f,0xfe2ce6e0,0xa3014314,0x4e0811a1,
				0xf7537e82,0xbd3af235,0x2ad7d2bb,0xeb86d391};

        /// <summary>
        /// calculat md5 signature of the string in Input
        /// </summary>
        /// <returns> Digest: the finger print of msg</returns>
        public static unsafe Hash ComputeHash(byte[] input)
        {
            var hash = new Hash();
            ComputeHash(input, hash);
            return hash;
        }

        /// <summary>
        /// calculat md5 signature of the string in Input
        /// </summary>
        /// <returns> Digest: the finger print of msg</returns>
        public static unsafe void ComputeHash(byte[] input, Hash dg)
        {
            uint* tmp = stackalloc uint[16];

            // Default values for algorithm
            dg.A = 0x67452301;
            dg.B = 0xEFCDAB89;
            dg.C = 0x98BADCFE;
            dg.D = 0X10325476;

            uint N = (uint)(input.Length * 8) / 32;

            for (uint i = 0; i < N / 16; i++)
            {
                CopyBlock(input, i, tmp);
                PerformTransformation(ref dg.A, ref dg.B, ref dg.C, ref dg.D, tmp);
            }

            if (input.Length % 64 >= 56)
            {
                CopyLastBlock(input, tmp);
                PerformTransformation(ref dg.A, ref dg.B, ref dg.C, ref dg.D, tmp);
                for (int i = 0; i < 16; i++)
                {
                    tmp[i] = 0;
                }
                ((ulong*)tmp)[7] = (ulong)input.Length * 8;
                PerformTransformation(ref dg.A, ref dg.B, ref dg.C, ref dg.D, tmp);
            }
            else
            {
                CopyLastBlock(input, tmp);
                ((ulong*)tmp)[7] = (ulong)input.Length * 8;
                PerformTransformation(ref dg.A, ref dg.B, ref dg.C, ref dg.D, tmp);
            }
        }

        /// <summary>
        /// perform transformatio using f(((b&c) | (~(b)&d))
        /// </summary>
        static unsafe void TransF(ref uint a, uint b, uint c, uint d, uint k, ushort s, uint i, uint* X)
        {
            a = b + Md5.RotateLeft((a + ((b & c) | (~(b) & d)) + X[k] + T[i - 1]), s);
        }

        /// <summary>
        /// perform transformatio using g((b&d) | (c & ~d) )
        /// </summary>
        static unsafe void TransG(ref uint a, uint b, uint c, uint d, uint k, ushort s, uint i, uint* X)
        {
            a = b + Md5.RotateLeft((a + ((b & d) | (c & ~d)) + X[k] + T[i - 1]), s);
        }

        /// <summary>
        /// perform transformatio using h(b^c^d)
        /// </summary>
        static unsafe void TransH(ref uint a, uint b, uint c, uint d, uint k, ushort s, uint i, uint* X)
        {
            a = b + Md5.RotateLeft((a + (b ^ c ^ d) + X[k] + T[i - 1]), s);
        }

        /// <summary>
        /// perform transformatio using i (c^(b|~d))
        /// </summary>
        static unsafe void TransI(ref uint a, uint b, uint c, uint d, uint k, ushort s, uint i, uint* X)
        {
            a = b + Md5.RotateLeft((a + (c ^ (b | ~d)) + X[k] + T[i - 1]), s);
        }

        /// <summary>
        /// Perform All the transformation on the data
        /// </summary>
        /// <param name="A">A</param>
        /// <param name="B">B </param>
        /// <param name="C">C</param>
        /// <param name="D">D</param>
        static unsafe void PerformTransformation(ref uint A, ref uint B, ref uint C, ref uint D, uint* X)
        {
            //// saving  ABCD  to be used in end of loop

            uint AA, BB, CC, DD;

            AA = A;
            BB = B;
            CC = C;
            DD = D;

            /* Round 1 
                * [ABCD  0  7  1]  [DABC  1 12  2]  [CDAB  2 17  3]  [BCDA  3 22  4]
                * [ABCD  4  7  5]  [DABC  5 12  6]  [CDAB  6 17  7]  [BCDA  7 22  8]
                * [ABCD  8  7  9]  [DABC  9 12 10]  [CDAB 10 17 11]  [BCDA 11 22 12]
                * [ABCD 12  7 13]  [DABC 13 12 14]  [CDAB 14 17 15]  [BCDA 15 22 16]
                *  * */
            TransF(ref A, B, C, D, 0, 7, 1, X); TransF(ref D, A, B, C, 1, 12, 2, X); TransF(ref C, D, A, B, 2, 17, 3, X); TransF(ref B, C, D, A, 3, 22, 4, X);
            TransF(ref A, B, C, D, 4, 7, 5, X); TransF(ref D, A, B, C, 5, 12, 6, X); TransF(ref C, D, A, B, 6, 17, 7, X); TransF(ref B, C, D, A, 7, 22, 8, X);
            TransF(ref A, B, C, D, 8, 7, 9, X); TransF(ref D, A, B, C, 9, 12, 10, X); TransF(ref C, D, A, B, 10, 17, 11, X); TransF(ref B, C, D, A, 11, 22, 12, X);
            TransF(ref A, B, C, D, 12, 7, 13, X); TransF(ref D, A, B, C, 13, 12, 14, X); TransF(ref C, D, A, B, 14, 17, 15, X); TransF(ref B, C, D, A, 15, 22, 16, X);
            /** rOUND 2
                **[ABCD  1  5 17]  [DABC  6  9 18]  [CDAB 11 14 19]  [BCDA  0 20 20]
                *[ABCD  5  5 21]  [DABC 10  9 22]  [CDAB 15 14 23]  [BCDA  4 20 24]
                *[ABCD  9  5 25]  [DABC 14  9 26]  [CDAB  3 14 27]  [BCDA  8 20 28]
                *[ABCD 13  5 29]  [DABC  2  9 30]  [CDAB  7 14 31]  [BCDA 12 20 32]
            */
            TransG(ref A, B, C, D, 1, 5, 17, X); TransG(ref D, A, B, C, 6, 9, 18, X); TransG(ref C, D, A, B, 11, 14, 19, X); TransG(ref B, C, D, A, 0, 20, 20, X);
            TransG(ref A, B, C, D, 5, 5, 21, X); TransG(ref D, A, B, C, 10, 9, 22, X); TransG(ref C, D, A, B, 15, 14, 23, X); TransG(ref B, C, D, A, 4, 20, 24, X);
            TransG(ref A, B, C, D, 9, 5, 25, X); TransG(ref D, A, B, C, 14, 9, 26, X); TransG(ref C, D, A, B, 3, 14, 27, X); TransG(ref B, C, D, A, 8, 20, 28, X);
            TransG(ref A, B, C, D, 13, 5, 29, X); TransG(ref D, A, B, C, 2, 9, 30, X); TransG(ref C, D, A, B, 7, 14, 31, X); TransG(ref B, C, D, A, 12, 20, 32, X);
            /*  rOUND 3
                * [ABCD  5  4 33]  [DABC  8 11 34]  [CDAB 11 16 35]  [BCDA 14 23 36]
                * [ABCD  1  4 37]  [DABC  4 11 38]  [CDAB  7 16 39]  [BCDA 10 23 40]
                * [ABCD 13  4 41]  [DABC  0 11 42]  [CDAB  3 16 43]  [BCDA  6 23 44]
                * [ABCD  9  4 45]  [DABC 12 11 46]  [CDAB 15 16 47]  [BCDA  2 23 48]
             * */
            TransH(ref A, B, C, D, 5, 4, 33, X); TransH(ref D, A, B, C, 8, 11, 34, X); TransH(ref C, D, A, B, 11, 16, 35, X); TransH(ref B, C, D, A, 14, 23, 36, X);
            TransH(ref A, B, C, D, 1, 4, 37, X); TransH(ref D, A, B, C, 4, 11, 38, X); TransH(ref C, D, A, B, 7, 16, 39, X); TransH(ref B, C, D, A, 10, 23, 40, X);
            TransH(ref A, B, C, D, 13, 4, 41, X); TransH(ref D, A, B, C, 0, 11, 42, X); TransH(ref C, D, A, B, 3, 16, 43, X); TransH(ref B, C, D, A, 6, 23, 44, X);
            TransH(ref A, B, C, D, 9, 4, 45, X); TransH(ref D, A, B, C, 12, 11, 46, X); TransH(ref C, D, A, B, 15, 16, 47, X); TransH(ref B, C, D, A, 2, 23, 48, X);
            /*ORUNF  4
                *[ABCD  0  6 49]  [DABC  7 10 50]  [CDAB 14 15 51]  [BCDA  5 21 52]
                *[ABCD 12  6 53]  [DABC  3 10 54]  [CDAB 10 15 55]  [BCDA  1 21 56]
                *[ABCD  8  6 57]  [DABC 15 10 58]  [CDAB  6 15 59]  [BCDA 13 21 60]
                *[ABCD  4  6 61]  [DABC 11 10 62]  [CDAB  2 15 63]  [BCDA  9 21 64]
                         * */
            TransI(ref A, B, C, D, 0, 6, 49, X); TransI(ref D, A, B, C, 7, 10, 50, X); TransI(ref C, D, A, B, 14, 15, 51, X); TransI(ref B, C, D, A, 5, 21, 52, X);
            TransI(ref A, B, C, D, 12, 6, 53, X); TransI(ref D, A, B, C, 3, 10, 54, X); TransI(ref C, D, A, B, 10, 15, 55, X); TransI(ref B, C, D, A, 1, 21, 56, X);
            TransI(ref A, B, C, D, 8, 6, 57, X); TransI(ref D, A, B, C, 15, 10, 58, X); TransI(ref C, D, A, B, 6, 15, 59, X); TransI(ref B, C, D, A, 13, 21, 60, X);
            TransI(ref A, B, C, D, 4, 6, 61, X); TransI(ref D, A, B, C, 11, 10, 62, X); TransI(ref C, D, A, B, 2, 15, 63, X); TransI(ref B, C, D, A, 9, 21, 64, X);

            A = A + AA;
            B = B + BB;
            C = C + CC;
            D = D + DD;
        }

        /// <summary>
        /// Copies a 512 bit block into X as 16 32 bit words
        /// </summary>
        /// <param name="bMsg"> source buffer</param>
        /// <param name="block">no of block to copy starting from 0</param>
        static unsafe void CopyBlock(byte[] bMsg, uint block, uint* X)
        {
            block = block << 6;
            for (uint j = 0; j < 61; j += 4)
            {
                X[j >> 2] = (((uint)bMsg[block + (j + 3)]) << 24) |
                        (((uint)bMsg[block + (j + 2)]) << 16) |
                        (((uint)bMsg[block + (j + 1)]) << 8) |
                        (((uint)bMsg[block + (j)]));
            }
        }

        static unsafe void CopyLastBlock(byte[] bMsg, uint* X)
        {
            int i;
            long start = (bMsg.LongLength / 64) * 64;

            byte* x = (byte*)X;
            for (i = 0; i < bMsg.Length - start; i++)
            {
                x[i] = bMsg[start + i];
            }
            x[i] = 0x80;
            i++;
            for (; i < 64; i++)
            {
                x[i] = 0;
            }
        }
    }
}
