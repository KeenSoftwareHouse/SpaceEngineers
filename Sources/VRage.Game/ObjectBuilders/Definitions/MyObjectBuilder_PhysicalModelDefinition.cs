using VRage.ObjectBuilders;
using ProtoBuf;
using VRage.Data;

namespace VRage.Game
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_PhysicalModelDefinition : MyObjectBuilder_DefinitionBase
    {
        [ProtoMember]
        [ModdableContentFile("mwm")]
        public string Model;

        [ProtoMember]
        public string PhysicalMaterial;

        [ProtoMember]
        public float Mass = 0;
    }
}
