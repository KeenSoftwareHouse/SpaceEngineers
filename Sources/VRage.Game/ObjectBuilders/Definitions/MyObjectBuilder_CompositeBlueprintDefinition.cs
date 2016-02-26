using ProtoBuf;
using System.Xml.Serialization;
using VRage.ObjectBuilders;

namespace VRage.Game
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
