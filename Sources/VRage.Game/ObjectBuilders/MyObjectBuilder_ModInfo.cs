using ProtoBuf;
using VRage.ObjectBuilders;

namespace VRage.Game
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_ModInfo : MyObjectBuilder_Base
    {
        [ProtoMember]
        public ulong SteamIDOwner;

        [ProtoMember]
        public ulong WorkshopId;
    }
}
