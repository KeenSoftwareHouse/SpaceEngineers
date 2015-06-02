using ProtoBuf;
using VRage.ObjectBuilders;
using Sandbox.Common.ObjectBuilders;

namespace Medieval.ObjectBuilders.Blocks
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_LockBlock : MyObjectBuilder_CubeBlock
    {
        [ProtoMember]
        public int State;
    }
}
