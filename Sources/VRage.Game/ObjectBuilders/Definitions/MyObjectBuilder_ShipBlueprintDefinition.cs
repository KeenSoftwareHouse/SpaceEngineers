using ProtoBuf;
using VRage.ObjectBuilders;

namespace VRage.Game
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_ShipBlueprintDefinition : MyObjectBuilder_PrefabDefinition
    {
        [ProtoMember]
        public ulong WorkshopId;

        [ProtoMember]
        public ulong OwnerSteamId;

        [ProtoMember]
        public ulong Points;
    }
}
