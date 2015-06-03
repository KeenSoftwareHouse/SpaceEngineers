using ProtoBuf;
using VRage.ObjectBuilders;

namespace Sandbox.Common.ObjectBuilders.Definitions
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_ShipControllerDefinition : MyObjectBuilder_CubeBlockDefinition
    {
        [ProtoMember]
        public bool EnableFirstPerson;
        [ProtoMember]
        public bool EnableShipControl;
    }
}
