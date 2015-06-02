using Medieval.ObjectBuilders.Definitions;
using ProtoBuf;
using VRage.ObjectBuilders;

namespace Sandbox.Common.ObjectBuilders
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    [MyEnvironmentItems(typeof(MyObjectBuilder_Tree))]
    public class MyObjectBuilder_Trees : MyObjectBuilder_EnvironmentItems
    {

    }
}
