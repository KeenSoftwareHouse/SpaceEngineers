using ProtoBuf;
using VRage.Data;
using VRage.ObjectBuilders;

namespace VRage.Game
{
    public enum MyDebrisType
    {
        Model,
        Voxel
    }

    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_DebrisDefinition : MyObjectBuilder_DefinitionBase
    {
        [ProtoMember]
        [ModdableContentFile("mwm")]
        public string Model;

        [ProtoMember]
        public MyDebrisType Type;
    }
}