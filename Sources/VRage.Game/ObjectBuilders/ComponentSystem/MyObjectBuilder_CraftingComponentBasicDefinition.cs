using ProtoBuf;
using System;
using VRage.ObjectBuilders;

namespace VRage.Game
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_CraftingComponentBasicDefinition : MyObjectBuilder_ComponentDefinitionBase
    {
        [ProtoMember]
        public String AvailableBlueprintClasses;

        [ProtoMember]
        public String ActionSound;

        [ProtoMember]
        public float CraftingSpeedMultiplier = 1.0f;
    }
}
