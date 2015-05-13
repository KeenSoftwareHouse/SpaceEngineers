using System.ComponentModel;
using ProtoBuf;

namespace Sandbox.Common.ObjectBuilders
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_InteriorLight : MyObjectBuilder_LightingBlock
    {
    }
}
