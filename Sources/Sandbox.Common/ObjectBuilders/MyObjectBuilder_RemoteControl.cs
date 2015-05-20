using ProtoBuf;

namespace Sandbox.Common.ObjectBuilders
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_RemoteControl : MyObjectBuilder_ShipController
    {
        [ProtoMember]
        public long? PreviousControlledEntityId = null;
    }
}
