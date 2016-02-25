using VRage.ObjectBuilders;
using ProtoBuf;

namespace VRage.Game
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_PhysicalMaterialDefinition : MyObjectBuilder_DefinitionBase
    {
        [ProtoMember]
        public float Density = 32000;

        [ProtoMember]
        public float HorisontalTransmissionMultiplier = 1;

        [ProtoMember]
        public float HorisontalFragility = 1;

        [ProtoMember]
        public float SupportMultiplier = 1;

        [ProtoMember]
        public float CollisionMultiplier = 1;

        [ProtoMember]
        public string DamageDecal = null;
    }
}
