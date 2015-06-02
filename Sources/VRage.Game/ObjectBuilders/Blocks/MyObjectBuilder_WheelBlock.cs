using ProtoBuf;
using Sandbox.Common.ObjectBuilders;
using VRage.ObjectBuilders;

namespace Medieval.ObjectBuilders.Blocks
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_WheelBlock : MyObjectBuilder_CubeBlock
    {
    }
}
