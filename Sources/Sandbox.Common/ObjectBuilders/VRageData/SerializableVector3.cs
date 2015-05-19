using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using ProtoBuf;
using VRageMath;

namespace Sandbox.Common.ObjectBuilders.VRageData
{
    [ProtoContract]
    public struct SerializableVector3
    {
        public float X;
        public float Y;
        public float Z;

        public bool ShouldSerializeX() { return false; }
        public bool ShouldSerializeY() { return false; }
        public bool ShouldSerializeZ() { return false; }

        public SerializableVector3(float x, float y, float z)
        {
            this.X = x;
            this.Y = y;
            this.Z = z;
        }

        [ProtoMember, XmlAttribute]
        public float x { get { return X; } set { X = value; } }

        [ProtoMember, XmlAttribute]
        public float y { get { return Y; } set { Y = value; } }

        [ProtoMember, XmlAttribute]
        public float z { get { return Z; } set { Z = value; } }

        public bool IsZero { get { return X == 0.0f && Y == 0.0f && Z == 0.0f; }  }

        public static implicit operator Vector3(SerializableVector3 v)
        {
            return new Vector3(v.X, v.Y, v.Z);
        }

        public static implicit operator SerializableVector3(Vector3 v)
        {
            return new SerializableVector3(v.X, v.Y, v.Z);
        }
    }
}
