using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRageMath;

namespace VRage.Serialization
{
    [ProtoContract]
    public class SerializableBoundingSphereD
    {
        [ProtoMember]
        public SerializableVector3D Center;

        [ProtoMember]
        public double Radius;

        public static implicit operator BoundingSphereD(SerializableBoundingSphereD v)
        {
            return new BoundingSphereD(v.Center, v.Radius);
        }

        public static implicit operator SerializableBoundingSphereD(BoundingSphereD v)
        {
            return new SerializableBoundingSphereD { Center = v.Center, Radius = v.Radius };
        }

    }
}
