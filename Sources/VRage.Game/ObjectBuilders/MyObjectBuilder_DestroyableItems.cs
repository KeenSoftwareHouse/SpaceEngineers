using ProtoBuf;
using VRage.ObjectBuilders;

namespace Sandbox.Common.ObjectBuilders
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    [MyEnvironmentItems(typeof(MyObjectBuilder_DestroyableItem))]
    public class MyObjectBuilder_DestroyableItems : MyObjectBuilder_EnvironmentItems
    {
    }
}
