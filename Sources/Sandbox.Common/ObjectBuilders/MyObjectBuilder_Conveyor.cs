using ProtoBuf;

namespace Sandbox.Common.ObjectBuilders
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_Conveyor : MyObjectBuilder_CubeBlock
    {
    }
}
