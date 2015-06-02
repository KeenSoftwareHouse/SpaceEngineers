using ProtoBuf;
using VRage.ObjectBuilders;

namespace Sandbox.Common.ObjectBuilders.Definitions
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_OxygenTankDefinition : MyObjectBuilder_ProductionBlockDefinition
    {
        [ProtoMember]
        public float Capacity;
    }
}
