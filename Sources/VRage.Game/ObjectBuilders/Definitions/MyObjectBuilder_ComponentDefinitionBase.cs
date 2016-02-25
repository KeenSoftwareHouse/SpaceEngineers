using ProtoBuf;
using VRage.ObjectBuilders;

namespace VRage.Game
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_ComponentDefinitionBase : MyObjectBuilder_DefinitionBase
    {
        [ProtoMember]
        public string ComponentType;
    }
}
