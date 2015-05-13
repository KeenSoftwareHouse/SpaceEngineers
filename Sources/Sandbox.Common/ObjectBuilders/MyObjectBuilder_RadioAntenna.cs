using ProtoBuf;

namespace Sandbox.Common.ObjectBuilders
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_RadioAntenna : MyObjectBuilder_FunctionalBlock
    {
        [ProtoMember(1)]
        public float BroadcastRadius;
        [ProtoMember(2)]
        public bool ShowShipName;
        [ProtoMember(3)]
        public bool EnableBroadcasting = true;
    }
}
