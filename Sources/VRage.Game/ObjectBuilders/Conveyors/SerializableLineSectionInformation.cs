using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using VRageMath;

namespace Sandbox.Common.ObjectBuilders.Conveyors
{
    [ProtoContract]
    public struct SerializableLineSectionInformation
    {
        [ProtoMember, XmlAttribute]
        public Base6Directions.Direction Direction;

        [ProtoMember, XmlAttribute]
        public int Length;
    }
}
