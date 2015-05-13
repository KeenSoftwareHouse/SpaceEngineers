using System;
using System.Collections.Generic;

namespace VRageMath
{
    //  Integer version of Vector4, not yet fully implemented
    [ProtoBuf.ProtoContract]
    public struct Vector4I
    {
        [ProtoBuf.ProtoMember(1)]
        public int X;
        [ProtoBuf.ProtoMember(2)]
        public int Y;
        [ProtoBuf.ProtoMember(3)]
        public int Z;
        [ProtoBuf.ProtoMember(4)]
        public int W;

        public Vector4I(int x, int y, int z, int w)
        {
            X = x;
            Y = y;
            Z = z;
            W = w;
        }

        public override string ToString()
        {
            return X + ", " + Y + ", " + Z + ", " + W;
        }
    }
}
