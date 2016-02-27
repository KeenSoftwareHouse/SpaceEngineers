using ProtoBuf;

namespace VRage.ObjectBuilders
{
    /// <summary>
    /// This object builder is old and is for "MyInventoryBagEntity". Do not use it as base class or for anything. It is here only for backward compatibility.
    /// </summary>
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_ReplicableEntity : MyObjectBuilder_EntityBase
    {
        public SerializableVector3 LinearVelocity;
        public SerializableVector3 AngularVelocity;
        public float Mass = 5.0f;
    }
}
