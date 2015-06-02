using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using ProtoBuf;
using VRageMath;

namespace VRage
{
    [ProtoContract]
    public struct SerializableBounds
    {
        [ProtoMember, XmlAttribute]
        public float Min;

        [ProtoMember, XmlAttribute]
        public float Max;

        [ProtoMember, XmlAttribute]
        public float Default;

        public SerializableBounds(float min, float max, float def)
        {
            Min = min;
            Max = max;
            Default = def;
        }

        public static implicit operator MyBounds(SerializableBounds v)
        {
            return new MyBounds(v.Min, v.Max, v.Default);
        }

        public static implicit operator SerializableBounds(MyBounds v)
        {
            return new SerializableBounds(v.Min, v.Max, v.Default);
        }

    }
}
