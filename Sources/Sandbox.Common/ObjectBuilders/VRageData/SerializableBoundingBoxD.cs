using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRageMath;

namespace Sandbox.Common.ObjectBuilders.VRageData
{
    [ProtoContract]
    public struct SerializableBoundingBoxD
    {
        [ProtoMember(1)]
        public SerializableVector3D Min;

        [ProtoMember(2)]
        public SerializableVector3D Max;

        public static implicit operator BoundingBoxD(SerializableBoundingBoxD v)
        {
            return new BoundingBoxD(v.Min, v.Max);
        }

        public static implicit operator SerializableBoundingBoxD(BoundingBoxD v)
        {
            return new SerializableBoundingBoxD { Min = v.Min, Max = v.Max };
        }

    }

}
