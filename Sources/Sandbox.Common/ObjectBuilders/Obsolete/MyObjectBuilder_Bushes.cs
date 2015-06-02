using ProtoBuf;
using Sandbox.Common.ObjectBuilders;
using VRage.ObjectBuilders;

namespace Sandbox.Common.ObjectBuilders
{
    // This class is obsolete and for backward compatibility only! Use MyObjectBuilder_DestroyableItems instead!
    [ProtoContract]
    [MyObjectBuilderDefinition(obsoleteBy: typeof(MyObjectBuilder_DestroyableItems))]
    [MyEnvironmentItems(typeof(MyObjectBuilder_DestroyableItem))]
    public class MyObjectBuilder_Bushes : MyObjectBuilder_EnvironmentItems
    {
    }
}
