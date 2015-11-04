using VRage.ObjectBuilders;
using ProtoBuf;

namespace Sandbox.Common.ObjectBuilders.Definitions
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_AssemblerDefinition : MyObjectBuilder_ProductionBlockDefinition
    {
        [ProtoMember]
        public float AssemblySpeed = 1.0f;
    }
}
