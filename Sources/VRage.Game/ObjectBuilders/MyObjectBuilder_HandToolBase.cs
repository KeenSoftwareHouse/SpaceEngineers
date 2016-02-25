using ProtoBuf;
using VRage.ObjectBuilders;

namespace VRage.Game
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_HandToolBase : MyObjectBuilder_EntityBase
    {
    }

    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_HandTool : MyObjectBuilder_HandToolBase
    {
    }

    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_GoodAIControlHandTool : MyObjectBuilder_HandToolBase
    {
    }
}