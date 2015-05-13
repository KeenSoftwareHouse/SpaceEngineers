using ProtoBuf;

namespace Sandbox.Common.ObjectBuilders
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_RemoteControl : MyObjectBuilder_ShipController
    {
        [ProtoMember(1)]
        public long? PreviousControlledEntityId = null;
    }
}
