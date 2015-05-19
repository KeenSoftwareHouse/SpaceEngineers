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
    public struct SerializableVector2I
    {
        public int X;
        public int Y;

        public bool ShouldSerializeX() { return false; }
        public bool ShouldSerializeY() { return false; }

        public SerializableVector2I(int x, int y)
        {
            this.X = x;
            this.Y = y;
        }

        [ProtoMember, XmlAttribute]
        public int x { get { return X; } set { X = value; } }

        [ProtoMember, XmlAttribute]
        public int y { get { return Y; } set { Y = value; } }

        public static implicit operator Vector2I(SerializableVector2I v)
        {
            return new Vector2I(v.X, v.Y);
        }

        public static implicit operator SerializableVector2I(Vector2I v)
        {
            return new SerializableVector2I(v.X, v.Y);
        }
    }
}
