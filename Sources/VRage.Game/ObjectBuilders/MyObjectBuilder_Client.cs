using ProtoBuf;
using VRage.ObjectBuilders;

namespace Sandbox.Common.ObjectBuilders
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
