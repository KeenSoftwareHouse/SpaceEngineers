using ProtoBuf;
using VRage.ObjectBuilders;

namespace VRage.Game
{
    // This class is obsolete and for backward compatibility only! Use MyObjectBuilder_Trees instead!
    [ProtoContract]
    [MyObjectBuilderDefinition(obsoleteBy: typeof(MyObjectBuilder_Trees))]
    [MyEnvironmentItems(typeof(MyObjectBuilder_Tree))]
    public class MyObjectBuilder_TreesMedium : MyObjectBuilder_Trees
    {

    }
}
