using ProtoBuf;

namespace Sandbox.Common.ObjectBuilders
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_RadioAntenna : MyObjectBuilder_FunctionalBlock
    {
        [ProtoMember]
        public float BroadcastRadius;
        [ProtoMember]
        public bool ShowShipName;
        [ProtoMember]
        public bool EnableBroadcasting = true;
    }
}
