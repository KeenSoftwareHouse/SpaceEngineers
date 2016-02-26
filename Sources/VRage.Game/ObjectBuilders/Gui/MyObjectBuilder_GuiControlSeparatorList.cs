using VRage.ObjectBuilders;
using ProtoBuf;
using System.ComponentModel;
using System.Xml.Serialization;
using System.Collections.Generic;

namespace VRage.Game
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_GuiControlSeparatorList : MyObjectBuilder_GuiControlBase
    {
        [ProtoContract]
        public struct Separator
        {
            [ProtoMember, DefaultValue(0f), XmlAttribute]
            public float StartX { get; set; }

            [ProtoMember, DefaultValue(0f), XmlAttribute]
            public float StartY { get; set; }

            [ProtoMember, DefaultValue(0f), XmlAttribute]
            public float SizeX { get; set; }

            [ProtoMember, DefaultValue(0f), XmlAttribute]
            public float SizeY { get; set; }
        }

        [ProtoMember]
        public List<Separator> Separators = new List<Separator>();
    }
}
