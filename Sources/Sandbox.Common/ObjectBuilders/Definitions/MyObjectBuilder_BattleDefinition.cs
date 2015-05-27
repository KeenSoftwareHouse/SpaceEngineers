using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using ProtoBuf;

namespace Sandbox.Common.ObjectBuilders.Definitions
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_BattleDefinition : MyObjectBuilder_DefinitionBase
    {
        [ProtoMember]
        public MyObjectBuilder_Toolbar DefaultToolbar;

        [XmlArrayItem("Block")]
        [ProtoMember, DefaultValue(null)]
        public SerializableDefinitionId[] SpawnBlocks = null;
    }
}
