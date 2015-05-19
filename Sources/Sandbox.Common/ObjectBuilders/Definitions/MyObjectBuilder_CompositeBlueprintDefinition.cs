using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace Sandbox.Common.ObjectBuilders.Definitions
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_CompositeBlueprintDefinition : MyObjectBuilder_DefinitionBase
    {
        [ProtoMember]
        [XmlArrayItem("Blueprint")]
        public BlueprintItem[] Blueprints;
    }
}
