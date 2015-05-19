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
    public struct SerializableVector3UByte
    {
        public byte X;
        public byte Y;
        public byte Z;

        public bool ShouldSerializeX() { return false; }
        public bool ShouldSerializeY() { return false; }
        public bool ShouldSerializeZ() { return false; }

        public SerializableVector3UByte(byte x, byte y, byte z)
        {
            this.X = x;
            this.Y = y;
            this.Z = z;
        }

        [ProtoMember, XmlAttribute]
        public byte x { get { return X; } set { X = value; } }

        [ProtoMember, XmlAttribute]
        public byte y { get { return Y; } set { Y = value; } }

        [ProtoMember, XmlAttribute]
        public byte z { get { return Z; } set { Z = value; } }

        public static implicit operator Vector3UByte(SerializableVector3UByte v)
        {
            return new Vector3UByte(v.X, v.Y, v.Z);
        }

        public static implicit operator SerializableVector3UByte(Vector3UByte v)
        {
            return new SerializableVector3UByte(v.X, v.Y, v.Z);
        }
    }
}
