using ProtoBuf;
using VRage.ObjectBuilders;

namespace VRage.Game
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_Client : MyObjectBuilder_Base
    {
        [ProtoMember]
        public ulong SteamId;

        [ProtoMember]
        public string Name;

        [ProtoMember]
        public bool IsAdmin;
    }
}
