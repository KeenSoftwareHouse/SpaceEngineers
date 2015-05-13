using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VRageMath
{
    public struct MyUShort4
    {
        public ushort X;
        public ushort Y;
        public ushort Z;
        public ushort W;

        public MyUShort4(uint x, uint y, uint z, uint w)
        {
            X = (ushort)x;
            Y = (ushort)y;
            Z = (ushort)z;
            W = (ushort)w;
        }

        public static unsafe explicit operator ulong(MyUShort4 val)
        {
            return *(ulong*)(&val);
        }
        public static unsafe explicit operator MyUShort4(ulong val)
        {
            return *(MyUShort4*)(&val);
        }
    }
}
