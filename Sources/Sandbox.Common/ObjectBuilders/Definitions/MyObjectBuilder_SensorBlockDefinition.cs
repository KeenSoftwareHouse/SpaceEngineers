using ProtoBuf;

namespace Sandbox.Common.ObjectBuilders.Definitions
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_SensorBlockDefinition : MyObjectBuilder_CubeBlockDefinition
    {
        [ProtoMember]
        public float RequiredPowerInput;

        [ProtoMember]
        public float MaxRange = 50.0f; // Default value used as magic number till now
    }
}
