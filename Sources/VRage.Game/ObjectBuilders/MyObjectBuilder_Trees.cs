using ProtoBuf;
using VRage.ObjectBuilders;

namespace VRage.Game
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    [MyEnvironmentItems(typeof(MyObjectBuilder_Tree))]
    public class MyObjectBuilder_Trees : MyObjectBuilder_EnvironmentItems
    {

    }
}
