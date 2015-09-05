using ProtoBuf;
using VRage.ObjectBuilders;

namespace Sandbox.Common.ObjectBuilders.Definitions
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_ProjectorDefinition : MyObjectBuilder_CubeBlockDefinition
    {
        [ProtoMember]
        public float RequiredPowerInput;
    }
}
