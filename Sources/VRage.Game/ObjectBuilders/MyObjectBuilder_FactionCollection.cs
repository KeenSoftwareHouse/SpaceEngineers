﻿using ProtoBuf;
using Sandbox.Common.ObjectBuilders.Definitions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.ObjectBuilders;
using VRage.Serialization;

namespace Sandbox.Common
{    public enum MyRelationsBetweenFactions
    {
        Neutral,
        Enemies
    }
}


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
        [ProtoMember]
        public long FactionId1;

        [ProtoMember]
        public long FactionId2;

        [ProtoMember]
        public MyRelationsBetweenFactions Relation;
    }

    [ProtoContract]
    public struct MyObjectBuilder_FactionRequests
    {
        [ProtoMember]
        public long FactionId;

        [ProtoMember]
        public List<long> FactionRequests;
    }

    [ProtoContract]
    [MyObjectBuilderDefinition(LegacyName: "Factions")]
    public class MyObjectBuilder_FactionCollection : MyObjectBuilder_Base
    {
        [ProtoMember]
        public List<MyObjectBuilder_Faction> Factions;

        [ProtoMember]
        public SerializableDictionary<long, long> Players;

        [ProtoMember]
        public List<MyObjectBuilder_FactionRelation> Relations;

        [ProtoMember]
        public List<MyObjectBuilder_FactionRequests> Requests;
    }
}
