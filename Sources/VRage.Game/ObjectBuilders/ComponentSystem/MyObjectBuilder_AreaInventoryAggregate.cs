using ProtoBuf;
using VRage.ObjectBuilders;

namespace VRage.Game.ObjectBuilders.ComponentSystem
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_AreaInventoryAggregate : MyObjectBuilder_InventoryAggregate
    {
        [ProtoMember]
        public float Radius;
    }
}
