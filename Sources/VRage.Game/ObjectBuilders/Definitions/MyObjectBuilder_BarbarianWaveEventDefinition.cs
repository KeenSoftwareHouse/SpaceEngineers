using ProtoBuf;
using Sandbox.Common.ObjectBuilders.Definitions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using VRage.ObjectBuilders;

namespace VRage.Game.ObjectBuilders.Definitions
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_BarbarianWaveEventDefinition : MyObjectBuilder_GlobalEventDefinition
    {
        [ProtoContract]
        public class BotDef
        {
            [ProtoMember]
            [XmlAttribute]
            public string SubtypeName;   
        }

        [ProtoContract]
        public class WaveDef
        {
            [ProtoMember]
            [XmlAttribute]
            public int Day;

            [XmlArrayItem("Bot")]
            [ProtoMember]
            public BotDef[] Bots;
        }

        [XmlArrayItem("Wave")]
        [ProtoMember]
        public WaveDef[] Waves;
    }
}
