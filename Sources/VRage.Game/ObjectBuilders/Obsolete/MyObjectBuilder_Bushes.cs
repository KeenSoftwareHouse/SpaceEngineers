using ProtoBuf;
using VRage.ObjectBuilders;

namespace VRage.Game
{
    // This class is obsolete and for backward compatibility only! Use MyObjectBuilder_DestroyableItems instead!
    [ProtoContract]
    [MyObjectBuilderDefinition(obsoleteBy: typeof(MyObjectBuilder_DestroyableItems))]
    [MyEnvironmentItems(typeof(MyObjectBuilder_DestroyableItem))]
    public class MyObjectBuilder_Bushes : MyObjectBuilder_EnvironmentItems
    {
    }
}
