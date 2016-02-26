using ProtoBuf;
using System;
using VRage.ObjectBuilders;
using System.Xml.Serialization;

namespace VRage.Game
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_CraftingComponentBlockDefinition : MyObjectBuilder_ComponentDefinitionBase
    {
        [ProtoMember]
        public String AvailableBlueprintClasses;

        [ProtoMember]
        public float CraftingSpeedMultiplier = 1.0f;

        [ProtoMember, XmlArrayItem("OperatingItem")]
        public SerializableDefinitionId[] AcceptedOperatingItems = new SerializableDefinitionId[0];
    }
}
