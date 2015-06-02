using ProtoBuf;
using VRage.ObjectBuilders;

namespace Sandbox.Common.ObjectBuilders
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_AirVent : MyObjectBuilder_FunctionalBlock
    {
        [ProtoMember]
        public bool IsDepressurizing;
    }
}
