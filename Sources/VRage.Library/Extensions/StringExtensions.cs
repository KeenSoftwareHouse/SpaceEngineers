using System;
using System.Collections.Generic;
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
    }
}
