using ProtoBuf;
using VRage.ObjectBuilders;

namespace VRage.Game.ObjectBuilders.ComponentSystem
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_TimerComponentDefinition : MyObjectBuilder_ComponentDefinitionBase
    {
        [ProtoMember]
        public float TimeToRemoveMin;
    }
}
