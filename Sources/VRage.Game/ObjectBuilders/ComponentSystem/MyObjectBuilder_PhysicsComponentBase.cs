using ProtoBuf;
using VRage.ObjectBuilders;

namespace VRage.Game.ObjectBuilders.ComponentSystem
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public abstract class MyObjectBuilder_PhysicsComponentBase : MyObjectBuilder_ComponentBase
    {
        public SerializableVector3 LinearVelocity;
        public SerializableVector3 AngularVelocity;
    }
}
