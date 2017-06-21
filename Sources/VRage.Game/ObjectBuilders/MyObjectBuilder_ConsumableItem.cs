using ProtoBuf;
using VRage.Game.ObjectBuilders;
using VRage.ObjectBuilders;

namespace VRage.Game
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_ConsumableItem : MyObjectBuilder_UsableItem
    {
    }
}
