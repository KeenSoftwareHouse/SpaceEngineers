using ProtoBuf;
using System.Collections.Generic;
using System.Xml.Serialization;
using VRage.ObjectBuilders;

namespace VRage.Game.ObjectBuilders.Components
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_SpaceFaunaComponent : MyObjectBuilder_SessionComponent
    {
        [ProtoContract]
        public class SpawnInfo
        {
            [ProtoMember]
            [XmlAttribute]
            public double X;

            [ProtoMember]
            [XmlAttribute]
            public double Y;

            [ProtoMember]
            [XmlAttribute]
            public double Z;
            
            [ProtoMember]
            [XmlAttribute("S")]
            public int SpawnTime;
            
            [ProtoMember]
            [XmlAttribute("A")]
            public int AbandonTime;
        }

        [ProtoContract]
        public class TimeoutInfo
        {
            [ProtoMember]
            [XmlAttribute]
            public double X;

            [ProtoMember]
            [XmlAttribute]
            public double Y;

            [ProtoMember]
            [XmlAttribute]
            public double Z;
            
            [ProtoMember]
            [XmlAttribute("T")]
            public int Timeout;
        }

        [XmlArrayItem("Info")]
        [ProtoMember]
        public List<SpawnInfo> SpawnInfos = new List<SpawnInfo>();

        [XmlArrayItem("Info")]
        [ProtoMember]
        public List<TimeoutInfo> TimeoutInfos = new List<TimeoutInfo>();
    }
}
