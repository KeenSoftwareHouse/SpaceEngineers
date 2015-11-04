using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using ProtoBuf;
using VRageMath;
using VRage.Serialization;

namespace VRage
{
    [ProtoContract]
    public struct SerializableVector3D
    {
        public double X;
        public double Y;
        public double Z;

        public bool ShouldSerializeX() { return false; }
        public bool ShouldSerializeY() { return false; }
        public bool ShouldSerializeZ() { return false; }

        public SerializableVector3D(double x, double y, double z)
        {
            this.X = x;
            this.Y = y;
            this.Z = z;
        }
        
        public SerializableVector3D(Vector3D v)
        {
            this.X = v.X;
            this.Y = v.Y;
            this.Z = v.Z;
        }

        [ProtoMember, XmlAttribute]
        [NoSerialize]
        public double x { get { return X; } set { X = value; } }

        [ProtoMember, XmlAttribute]
        [NoSerialize]
        public double y { get { return Y; } set { Y = value; } }

        [ProtoMember, XmlAttribute]
        [NoSerialize]
        public double z { get { return Z; } set { Z = value; } }

        public bool IsZero { get { return X == 0.0 && Y == 0.0 && Z == 0.0; }  }

        public static implicit operator Vector3D(SerializableVector3D v)
        {
            return new Vector3D(v.X, v.Y, v.Z);
        }

        public static implicit operator SerializableVector3D(Vector3D v)
        {
            return new SerializableVector3D(v.X, v.Y, v.Z);
        }
    }
}
