using VRage.ObjectBuilders;
using ProtoBuf;
using VRage;

namespace Sandbox.Common.ObjectBuilders
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_Ore : MyObjectBuilder_PhysicalObject
    {
    }
}
