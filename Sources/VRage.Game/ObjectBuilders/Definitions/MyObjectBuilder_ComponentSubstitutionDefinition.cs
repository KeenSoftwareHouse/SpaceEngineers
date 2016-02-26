using ProtoBuf;
using System.Xml.Serialization;
using VRage.ObjectBuilders;

namespace VRage.Game
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
