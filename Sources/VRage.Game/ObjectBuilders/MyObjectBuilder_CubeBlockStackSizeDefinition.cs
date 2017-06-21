using ProtoBuf;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using VRage.Game;
using VRage.ObjectBuilders;

namespace ObjectBuilders.Definitions
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_CubeBlockStackSizeDefinition: MyObjectBuilder_DefinitionBase
    {
        [ProtoContract]
        public struct BlockStackSizeDef
        {
            [ProtoMember]
            [XmlAttribute("TypeId")]
            public string TypeId;

            [ProtoMember]
            [XmlAttribute("SubtypeId")]
            public string SubtypeId;

            [ProtoMember]
            [XmlAttribute("MaxStackSize"), DefaultValue(1)]
            public int MaxStackSize;
        }

        [ProtoMember]
        [XmlElement("Block")]
        public BlockStackSizeDef[] Blocks;

    }
}
