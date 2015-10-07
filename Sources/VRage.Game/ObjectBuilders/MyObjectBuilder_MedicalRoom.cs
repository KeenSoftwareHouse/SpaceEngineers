using VRage.ObjectBuilders;
using ProtoBuf;
using VRage.Serialization;

namespace Sandbox.Common.ObjectBuilders
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_MedicalRoom : MyObjectBuilder_FunctionalBlock
    {
        [ProtoMember]
        public ulong SteamUserId;

        [ProtoMember]
        [Serialize(MyObjectFlags.Nullable)]
        public string IdleSound;

        [ProtoMember]
        [Serialize(MyObjectFlags.Nullable)]
        public string ProgressSound;

        [ProtoMember]
        public bool TakeOwnership;

        [ProtoMember]
        public bool SetFaction;
    }
}
