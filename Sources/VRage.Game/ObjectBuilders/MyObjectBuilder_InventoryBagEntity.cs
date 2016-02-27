using ProtoBuf;
using VRage.ObjectBuilders;

namespace VRage.Game.ObjectBuilders
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_InventoryBagEntity : MyObjectBuilder_EntityBase
    {
        public SerializableVector3 LinearVelocity;
        public SerializableVector3 AngularVelocity;
        public float Mass = 5.0f;
    }
}
