using ProtoBuf;
using VRage.ObjectBuilders;

namespace VRage.Game
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    [MyEnvironmentItems(typeof(MyObjectBuilder_DestroyableItem))]
    public class MyObjectBuilder_DestroyableItems : MyObjectBuilder_EnvironmentItems
    {
    }
}
