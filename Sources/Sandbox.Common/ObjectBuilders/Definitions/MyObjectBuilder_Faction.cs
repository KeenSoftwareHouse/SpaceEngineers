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
        [ProtoMember(1)]
        public long PlayerId;

        [ProtoMember(2)]
        public bool IsLeader;

        [ProtoMember(3)]
        public bool IsFounder;
    }

    [ProtoContract]
    public class MyObjectBuilder_Faction
    {
        [ProtoMember(1)]
        public long FactionId;

        [ProtoMember(2)]
        public string Tag;

        [ProtoMember(3)]
        public string Name;

        [ProtoMember(4)]
        public string Description;

        [ProtoMember(5)]
        public string PrivateInfo;

        [ProtoMember(6)]
        public List<MyObjectBuilder_FactionMember> Members;

        [ProtoMember(7)]
        public List<MyObjectBuilder_FactionMember> JoinRequests;

        [ProtoMember(8)]
        public bool AutoAcceptMember;

        [ProtoMember(9)]
        public bool AutoAcceptPeace;
    }
}
