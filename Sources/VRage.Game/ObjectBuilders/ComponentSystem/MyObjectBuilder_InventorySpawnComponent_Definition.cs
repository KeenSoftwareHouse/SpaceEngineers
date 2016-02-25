using ProtoBuf;
using VRage.ObjectBuilders;

namespace VRage.Game.ObjectBuilders.ComponentSystem
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_InventorySpawnComponent_Definition : MyObjectBuilder_ComponentDefinitionBase
    {
        public SerializableDefinitionId ContainerDefinition;
    }
}
