using ProtoBuf;
using VRage.ObjectBuilders;

namespace Sandbox.Common.ObjectBuilders
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
