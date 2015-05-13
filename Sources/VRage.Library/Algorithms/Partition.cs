using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VRage
{
    public static class Partition
    {
        private static readonly String[] m_letters = Enumerable.Range((int)'A', (int)('Z' - 'A') + 1).Select(s => new String((char)s, 1)).ToArray();

        public static T Select<T>(int num, T a, T b)
        {
            return num % 2 == 0 ? a : b;
        }

        public static T Select<T>(int num, T a, T b, T c)
        {
            uint i = ((uint)num) % 3;
            return i == 0 ? a : (i == 1 ? b : c);
        }

        public static T Select<T>(int num, T a, T b, T c, T d)
        {
            uint i = ((uint)num) % 4;
            return i == 0 ? a : (i == 1 ? b : (i == 2 ? c : d));
        }

        public static T Select<T>(int num, T a, T b, T c, T d, T e)
        {
            uint i = ((uint)num) % 5;
            return i == 0 ? a : (i == 1 ? b : (i == 2 ? c : (i == 3 ? d : e)));
        }

        public static T Select<T>(int num, T a, T b, T c, T d, T e, T f)
        {
            uint i = ((uint)num) % 6;
            switch (i)
            {
                case 0: return a;
                case 1: return b;
                case 2: return c;
                case 3: return d;
                case 4: return e;
                default: return f;
            }
        }

        public static T Select<T>(int num, T a, T b, T c, T d, T e, T f, T g)
        {
            uint i = ((uint)num) % 7;
            switch (i)
            {
                case 0: return a;
                case 1: return b;
                case 2: return c;
                case 3: return d;
                case 4: return e;
                case 5: return f;
                default: return g;
            }
        }

        public static T Select<T>(int num, T a, T b, T c, T d, T e, T f, T g, T h)
        {
            uint i = ((uint)num) % 8;
            switch (i)
            {
                case 0: return a;
                case 1: return b;
                case 2: return c;
                case 3: return d;
                case 4: return e;
                case 5: return f;
                case 6: return g;
                default: return h;
            }
        }

        public static T Select<T>(int num, T a, T b, T c, T d, T e, T f, T g, T h, T i)
        {
            uint x = ((uint)num) % 9;
            switch (x)
            {
                case 0: return a;
                case 1: return b;
                case 2: return c;
                case 3: return d;
                case 4: return e;
                case 5: return f;
                case 6: return g;
                case 7: return h;
                default: return i;
            }
        }

        public static string SelectStringByLetter(char c)
        {
            if ((c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z'))
            {
                c = char.ToUpperInvariant(c);
                return m_letters[c - 'A'];
            }
            else if (c >= '0' && c <= '9')
            {
                return "0-9";
            }
            else
                return "Non-letter";
        }

        public static string SelectStringGroupOfTenByLetter(char c)
        {
            c = char.ToUpperInvariant(c);
            if (c >= '0' && c <= '9') return "0-9";
            else if (c == 'A' || c == 'B' || c == 'C') return "A-C";
            else if (c == 'D' || c == 'E' || c == 'F') return "D-F";
            else if (c == 'G' || c == 'H' || c == 'I') return "G-I";
            else if (c == 'J' || c == 'K' || c == 'L') return "J-L";
            else if (c == 'M' || c == 'N' || c == 'O') return "M-O";
            else if (c == 'P' || c == 'Q' || c == 'R') return "P-R";
            else if (c == 'S' || c == 'T' || c == 'U' || c == 'V') return "S-V";
            else if (c == 'W' || c == 'X' || c == 'Y' || c == 'Z') return "W-Z";
            else return "Non-letter";
        }
    }
}
