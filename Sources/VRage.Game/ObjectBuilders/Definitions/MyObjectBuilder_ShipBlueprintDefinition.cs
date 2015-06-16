using ProtoBuf;
using VRage.ObjectBuilders;
using System.Xml.Serialization;

namespace Sandbox.Common.ObjectBuilders.Definitions
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
