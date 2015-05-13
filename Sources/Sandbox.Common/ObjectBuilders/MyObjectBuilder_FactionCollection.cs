using ProtoBuf;
using Sandbox.Common.ObjectBuilders.Definitions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.Serialization;

namespace Sandbox.Common.ObjectBuilders
{
    public enum MyOwnershipShareModeEnum
    {
        None,
        Faction,
        All
    }

    [ProtoContract]
    public struct MyObjectBuilder_FactionRelation
    {
        [ProtoMember(1)]
        public long FactionId1;

        [ProtoMember(2)]
        public long FactionId2;

        [ProtoMember(3)]
        public MyRelationsBetweenFactions Relation;
    }

    [ProtoContract]
    public struct MyObjectBuilder_FactionRequests
    {
        [ProtoMember(1)]
        public long FactionId;

        [ProtoMember(2)]
        public List<long> FactionRequests;
    }

    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_FactionCollection : MyObjectBuilder_Base
    {
        [ProtoMember(1)]
        public List<MyObjectBuilder_Faction> Factions;

        [ProtoMember(2)]
        public SerializableDictionary<long, long> Players;

        [ProtoMember(3)]
        public List<MyObjectBuilder_FactionRelation> Relations;

        [ProtoMember(4)]
        public List<MyObjectBuilder_FactionRequests> Requests;
    }
}
