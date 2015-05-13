using Medieval.ObjectBuilders.Definitions;
using ProtoBuf;
using Sandbox.Common.ObjectBuilders;

namespace Sandbox.Common.ObjectBuilders
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    [MyEnvironmentItems(typeof(MyObjectBuilder_Tree))]
    public class MyObjectBuilder_Trees : MyObjectBuilder_EnvironmentItems
    {

    }
}
