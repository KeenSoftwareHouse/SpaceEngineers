using ProtoBuf;
using System.ComponentModel;

namespace Sandbox.Common.ObjectBuilders
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_ConveyorConnector : MyObjectBuilder_CubeBlock
    {
    }
}
