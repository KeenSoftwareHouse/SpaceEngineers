using System.ComponentModel;
using ProtoBuf;
using VRage.ObjectBuilders;

namespace Sandbox.Common.ObjectBuilders
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_InteriorLight : MyObjectBuilder_LightingBlock
    {
    }
}
