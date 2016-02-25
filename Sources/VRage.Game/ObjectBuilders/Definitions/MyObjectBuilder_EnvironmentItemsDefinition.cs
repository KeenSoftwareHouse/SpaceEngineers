using VRage.ObjectBuilders;
using ProtoBuf;

namespace VRage.Game
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_EnvironmentItemsDefinition : MyObjectBuilder_DefinitionBase
    {
        [ProtoMember]
        public int Channel;

        [ProtoMember]
        public float MaxViewDistance;

        [ProtoMember]
        public float SectorSize;

        [ProtoMember]
        public string PhysicalMaterial;

        [ProtoMember]
        public float ItemSize;
    }
}
