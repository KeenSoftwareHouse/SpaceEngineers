using VRage.ObjectBuilders;
using ProtoBuf;

namespace Sandbox.Common.ObjectBuilders
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_MedicalRoom : MyObjectBuilder_FunctionalBlock
    {
        [ProtoMember]
        public ulong SteamUserId;

        [ProtoMember]
        public string IdleSound;

        [ProtoMember]
        public string ProgressSound;

        [ProtoMember]
        public bool TakeOwnership;
    }
}
