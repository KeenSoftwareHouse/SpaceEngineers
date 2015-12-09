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
    public struct SerializableVector3I
    {
        public int X;
        public int Y;
        public int Z;

        public bool ShouldSerializeX() { return false; }
        public bool ShouldSerializeY() { return false; }
        public bool ShouldSerializeZ() { return false; }

        public SerializableVector3I(int x, int y, int z)
        {
            this.X = x;
            this.Y = y;
            this.Z = z;
        }

        [ProtoMember, XmlAttribute]
        [NoSerialize]
        public int x { get { return X; } set { X = value; } }

        [ProtoMember, XmlAttribute]
        [NoSerialize]
        public int y { get { return Y; } set { Y = value; } }

        [ProtoMember, XmlAttribute]
        [NoSerialize]
        public int z { get { return Z; } set { Z = value; } }

        public static implicit operator Vector3I(SerializableVector3I v)
        {
            return new Vector3I(v.X, v.Y, v.Z);
        }

        public static implicit operator SerializableVector3I(Vector3I v)
        {
            return new SerializableVector3I(v.X, v.Y, v.Z);
        }

        public static bool operator ==(SerializableVector3I a, SerializableVector3I b)
        {
            return a.X == b.X && a.Y == b.Y && a.Z == b.Z;
        }

        public static bool operator !=(SerializableVector3I a, SerializableVector3I b)
        {
            return a.X != b.X || a.Y != b.Y || a.Z != b.Z;
        }

        public override bool Equals(object obj)
        {
            if (obj is SerializableVector3I)
            {
                return (SerializableVector3I)obj == this;
            }
            return false;
        }

        public override int GetHashCode()
        {
            return X.GetHashCode() * 1610612741 ^ Y.GetHashCode() * 24593 ^ Z.GetHashCode();
        }
    }
}
