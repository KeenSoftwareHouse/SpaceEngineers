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
    public struct SerializableVector2
    {
        public float X;
        public float Y;

        public bool ShouldSerializeX() { return false; }
        public bool ShouldSerializeY() { return false; }

        public SerializableVector2(float x, float y)
        {
            this.X = x;
            this.Y = y;
        }

        [ProtoMember, XmlAttribute]
        [NoSerialize]
        public float x { get { return X; } set { X = value; } }

        [ProtoMember, XmlAttribute]
        [NoSerialize]
        public float y { get { return Y; } set { Y = value; } }

        public static implicit operator Vector2(SerializableVector2 v)
        {
            return new Vector2(v.X, v.Y);
        }

        public static implicit operator SerializableVector2(Vector2 v)
        {
            return new SerializableVector2(v.X, v.Y);
        }
    }
}
