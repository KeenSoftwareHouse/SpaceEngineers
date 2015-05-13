using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VRageMath
{
    public struct MyShort4
    {
        public short X;
        public short Y;
        public short Z;
        public short W;

        public static unsafe explicit operator ulong(MyShort4 val)
        {
            return *(ulong*)(&val);
        }
        public static unsafe explicit operator MyShort4(ulong val)
        {
            return *(MyShort4*)(&val);
        }
    }
}
