using ProtoBuf;
using VRage.ObjectBuilders;
using VRage.Serialization;

namespace VRage.Game
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_Client : MyObjectBuilder_Base
    {
        [ProtoMember]
        public ulong SteamId;

        [ProtoMember]
        [Serialize(MyObjectFlags.Nullable)] 
        public string Name;

        [ProtoMember]
        public bool IsAdmin;
    }
}
