using ProtoBuf;
using VRage.ObjectBuilders;

namespace VRage.Game.ObjectBuilders.ComponentSystem
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_EntityDurabilityComponent : MyObjectBuilder_ComponentBase
    {
        [ProtoMember]
        public float EntityHP = 100.0f;
    }
}
