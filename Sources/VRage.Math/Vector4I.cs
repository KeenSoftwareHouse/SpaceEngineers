using System;
using System.Collections.Generic;

namespace VRageMath
{
    //  Integer version of Vector4, not yet fully implemented
    [ProtoBuf.ProtoContract]
    public struct Vector4I
    {
        [ProtoBuf.ProtoMember]
        public int X;
        [ProtoBuf.ProtoMember]
        public int Y;
        [ProtoBuf.ProtoMember]
        public int Z;
        [ProtoBuf.ProtoMember]
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
