using ProtoBuf;
using VRage.ObjectBuilders;

namespace Sandbox.Common.ObjectBuilders
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_GravityProvider : MyObjectBuilder_FunctionalBlock
    {
        [ProtoMember]
        public float? GravityAcceleration = null;
    }
}
