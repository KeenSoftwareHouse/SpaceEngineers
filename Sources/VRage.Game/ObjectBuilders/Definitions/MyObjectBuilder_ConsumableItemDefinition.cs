using ProtoBuf;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using VRage.ObjectBuilders;

namespace Sandbox.Common.ObjectBuilders.Definitions
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_ConsumableItemDefinition : MyObjectBuilder_PhysicalItemDefinition
    {
        [ProtoContract]
        public class StatValue
        {
            [ProtoMember]
            [XmlAttribute]
            public string Name;

            [ProtoMember]
            [XmlAttribute]
            public float Value;

            [ProtoMember]
            [XmlAttribute]
            public float Time; // s
        }

        [XmlArrayItem("Stat")]
        [ProtoMember]
        public StatValue[] Stats;

        [ProtoMember]
        public string EatingSound;
    }
}
