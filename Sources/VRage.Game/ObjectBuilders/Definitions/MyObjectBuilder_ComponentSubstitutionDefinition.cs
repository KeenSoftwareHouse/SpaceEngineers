using ProtoBuf;
using Sandbox.Common.ObjectBuilders.Definitions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using VRage.ObjectBuilders;

namespace Sandbox.Common.ObjectBuilders.Definitions
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_ComponentSubstitutionDefinition : MyObjectBuilder_DefinitionBase
    {
        [ProtoContract]
        public struct ProvidingComponent
        {
            [ProtoMember]            
            public SerializableDefinitionId Id;

            [ProtoMember]            
            public int Amount;
        }

        [ProtoMember]
        public SerializableDefinitionId RequiredComponentId;

        [XmlArrayItem("ProvidingComponent")]
        [ProtoMember]
        public ProvidingComponent[] ProvidingComponents;
    }
}
