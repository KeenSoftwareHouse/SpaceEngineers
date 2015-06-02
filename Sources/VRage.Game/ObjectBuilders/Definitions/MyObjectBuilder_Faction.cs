using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.Serialization;

namespace Sandbox.Common.ObjectBuilders.Definitions
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
        public string Tag;

        [ProtoMember]
        public string Name;

        [ProtoMember]
        public string Description;

        [ProtoMember]
        public string PrivateInfo;

        [ProtoMember]
        public List<MyObjectBuilder_FactionMember> Members;

        [ProtoMember]
        public List<MyObjectBuilder_FactionMember> JoinRequests;

        [ProtoMember]
        public bool AutoAcceptMember;

        [ProtoMember]
        public bool AutoAcceptPeace;
    }
}
