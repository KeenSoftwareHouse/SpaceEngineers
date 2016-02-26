using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace System
{
    public static class StringExtensions
    {
        public static unsafe bool Equals(this String text, char* compareTo, int length)
        {
            int len = Math.Min(length, text.Length);
            for (int i = 0; i < len; i++)
            {
                if (text[i] != compareTo[i])
                    return false;
            }
            if (length > len)
                return compareTo[len] == 0;
            return true;
        }

        public static bool Contains(this String text, string testSequence, StringComparison comparison)
        {
            return text.IndexOf(testSequence, comparison) != -1;
        }

        // Get 64bit hash code. Inspired by original implementation for string.
        public static unsafe long GetHashCode64(this String self)
        {
            fixed (char* src = self)
            {
                Debug.Assert(src[self.Length] == '\0', "src[this.Length] == '\\0'");
                Debug.Assert(((int)src) % 4 == 0, "Managed string should start at 4 bytes boundary");

                int len = self.Length;
                long* data = (long*)src;

                const long OTHER_PRIME = 1301077;
                long hash = OTHER_PRIME * OTHER_PRIME;

                ushort* hvals = (ushort*)src;

                for (; len >= 4; len -= 4)
                {
                    hash = (hash << 5) + hash + (hash >> 59);
                    hash ^= *data;

                    ++data;
                    hvals += 4;
                }

                if (len > 0)
                {
                    long suppl = 0;

                    for (; len > 0; --len)
                    {
                        suppl = (suppl << 16) | (ushort)*(hvals++);
                    }

                    hash = (hash << 5) + hash + (hash >> 59);
                    hash ^= suppl;
                }

                return hash;
            }
        }
    }
}
