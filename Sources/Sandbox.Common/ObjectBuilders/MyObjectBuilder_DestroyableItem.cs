using ProtoBuf;
using Sandbox.Common.ObjectBuilders;

namespace Sandbox.Common.ObjectBuilders
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_DestroyableItem : MyObjectBuilder_EntityBase
    {
    }
}
