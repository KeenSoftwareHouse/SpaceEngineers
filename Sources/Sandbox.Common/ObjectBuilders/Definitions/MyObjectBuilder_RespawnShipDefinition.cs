using ProtoBuf;
using VRage.ObjectBuilders;

namespace Sandbox.Common.ObjectBuilders.Definitions
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_RespawnShipDefinition : MyObjectBuilder_DefinitionBase
    {
        [ProtoMember]
        public string Prefab;

        [ProtoMember]
        public int CooldownSeconds;
    }
}
