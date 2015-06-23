using ProtoBuf;
using VRage.ObjectBuilders;

namespace Sandbox.Common.ObjectBuilders
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_GravityProvider : MyObjectBuilder_FunctionalBlock
    {
        [ProtoMember]
        public float GravityAcceleration = float.NaN; // NaN is used as an initialization check in MyGravityGenerator.Init(), then replaced with the default value.
    }
}
