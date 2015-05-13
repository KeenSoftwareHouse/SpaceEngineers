using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace VRageMath
{
    public static class MyMortonCode3D
    {

        public static int Encode(ref Vector3I value)
        {
            AssertRange(value);
            Debug.Assert(joinBits(splitBits(value.X)) == value.X);
            Debug.Assert(joinBits(splitBits(value.Y)) == value.Y);
            Debug.Assert(joinBits(splitBits(value.Z)) == value.Z);
            return (splitBits(value.X) << 0) |
                   (splitBits(value.Y) << 1) |
                   (splitBits(value.Z) << 2);
        }

        public static void Decode(int code, out Vector3I value)
        {
            value.X = joinBits(code >> 0);
            value.Y = joinBits(code >> 1);
            value.Z = joinBits(code >> 2);
            AssertRange(value);
        }

        /// <summary>
        /// Split 10 lowest bits of the number so that there are 3 empty slots between them.
        /// </summary>
        private static int splitBits(int x)
        {
            x = (x | (x << 16)) & 0x030000FF;
            x = (x | (x << 8)) & 0x0300F00F;
            x = (x | (x << 4)) & 0x030C30C3;
            x = (x | (x << 2)) & 0x09249249;
            return x;
        }

        /// <summary>
        /// Reverses splitBits operation.
        /// </summary>
        private static int joinBits(int x)
        {
            x &= 0x09249249; // erase any bits data that might get in the way
            x = (x | (x >> 2)) & 0x030C30C3;
            x = (x | (x >> 4)) & 0x0300F00F;
            x = (x | (x >> 8)) & 0x030000FF;
            x = (x | (x >> 16)) & 0x000003FF;
            return x;
        }

        [Conditional("DEBUG")]
        private static void AssertRange(Vector3I value)
        {
            const int MAX = 0x400;
            Debug.Assert(0 <= value.X && value.X < MAX &&
                         0 <= value.Y && value.Y < MAX &&
                         0 <= value.Z && value.Z < MAX,
                         string.Format("Value '{0}' cannot be encoded in Morton code.", value));
        }
    }

}
