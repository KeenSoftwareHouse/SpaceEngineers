using ProtoBuf;
using VRage.ObjectBuilders;

namespace VRage.Game
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
