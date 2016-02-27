using ProtoBuf;
using VRage.ObjectBuilders;

namespace VRage.Game.ObjectBuilders.ComponentSystem
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_PhysicsBodyComponentDefinition : MyObjectBuilder_PhysicsComponentDefinitionBase
    {
        [ProtoMember]
        public bool CreateFromCollisionObject;
    }
}
