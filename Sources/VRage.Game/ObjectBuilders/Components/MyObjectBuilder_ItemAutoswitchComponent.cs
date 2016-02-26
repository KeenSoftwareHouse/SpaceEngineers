using ProtoBuf;
using VRage.ObjectBuilders;

namespace VRage.Game.ObjectBuilders.Components
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_ItemAutoswitchComponent : MyObjectBuilder_SessionComponent
    {

        [ProtoMember]
        public SerializableDefinitionId? AutoswitchTargetDefinition;

    }
}
