using ProtoBuf;
using Sandbox.Common.ObjectBuilders;
using VRage.ObjectBuilders;

namespace Medieval.ObjectBuilders.Blocks
{
    [ProtoContract]
    [MyObjectBuilderDefinition]

    public class MyObjectBuilder_FireLightBlock : MyObjectBuilder_CubeBlock
    {
        [ProtoMember]
        public bool Enabled = true;
    }
}
