using ProtoBuf;
using VRage.ObjectBuilders;

namespace VRage.Game
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public abstract class MyObjectBuilder_ToolbarItemDefinition : MyObjectBuilder_ToolbarItem
    {
        [ProtoMember]
        public SerializableDefinitionId DefinitionId;
    }
}
