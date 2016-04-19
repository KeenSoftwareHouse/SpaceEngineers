using ProtoBuf;
using System.Collections.Generic;
using VRage.Serialization;

namespace VRage.Game
{
    [ProtoContract]
    public struct MyObjectBuilder_FactionMember
    {
        [ProtoMember]
        public long PlayerId;

        [ProtoMember]
        public bool IsLeader;

        [ProtoMember]
        public bool IsFounder;
    }

    [ProtoContract]
    public class MyObjectBuilder_Faction
    {
        [ProtoMember]
        public long FactionId;

        [ProtoMember]
        [Serialize(MyObjectFlags.Nullable)] 
        public string Tag;

        [ProtoMember]
        [Serialize(MyObjectFlags.Nullable)] 
        public string Name;

        [ProtoMember]
        [Serialize(MyObjectFlags.Nullable)] 
        public string Description;

        [ProtoMember]
        [Serialize(MyObjectFlags.Nullable)] 
        public string PrivateInfo;

        [ProtoMember]
        public List<MyObjectBuilder_FactionMember> Members;

        [ProtoMember]
        public List<MyObjectBuilder_FactionMember> JoinRequests;

        [ProtoMember]
        public bool AutoAcceptMember;

        [ProtoMember]
        public bool AutoAcceptPeace;

        [ProtoMember]
        public bool AcceptHumans = true;

        [ProtoMember]
        public bool EnableFriendlyFire = true;
    }
}