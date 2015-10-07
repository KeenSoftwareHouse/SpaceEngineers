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
    public struct SerializableQuaternion
    {
        public float X;
        public float Y;
        public float Z;
        public float W;

        public bool ShouldSerializeX() { return false; }
        public bool ShouldSerializeY() { return false; }
        public bool ShouldSerializeZ() { return false; }
        public bool ShouldSerializeW() { return false; }

        public SerializableQuaternion(float x, float y, float z, float w)
        {
            this.X = x;
            this.Y = y;
            this.Z = z;
            this.W = w;
        }

        [ProtoMember, XmlAttribute]
        [NoSerialize]
        public float x { get { return X; } set { X = value; } }

        [ProtoMember, XmlAttribute]
        [NoSerialize]
        public float y { get { return Y; } set { Y = value; } }

        [ProtoMember, XmlAttribute]
        [NoSerialize]
        public float z { get { return Z; } set { Z = value; } }

        [ProtoMember, XmlAttribute]
        [NoSerialize]
        public float w { get { return W; } set { W = value; } }

        public static implicit operator Quaternion(SerializableQuaternion q)
        {
            return new Quaternion(q.X, q.Y, q.Z, q.W);
        }

        public static implicit operator SerializableQuaternion(Quaternion q)
        {
            return new SerializableQuaternion(q.X, q.Y, q.Z, q.W);
        }

    }
}
