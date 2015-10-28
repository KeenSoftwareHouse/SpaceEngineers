﻿using ProtoBuf;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using VRage.Game.ObjectBuilders;
using VRage.ObjectBuilders;

namespace Sandbox.Common.ObjectBuilders.Definitions
{
    public enum MyAreaTransformType : byte
    {
        NONE = 0,
        ENRICHING,
        EXPANDING,
    }

    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_FloraElementDefinition : MyObjectBuilder_DefinitionBase
    {
        [ProtoContract]
        public class EnvItem
        {
            [ProtoMember]
            [XmlAttribute]
            public string Group;

            [ProtoMember]
            [XmlAttribute]
            public string Subtype;
        }

        [ProtoContract]
        public class GrowthStep
        {
            [ProtoMember]
            [XmlAttribute]
            public int GroupInsId = -1; // 0 is the first

            [ProtoMember]
            [XmlAttribute]
            public float Percent = 1f;
        }

        [ProtoContract]
        public class GatheredItemDef
        {
            [ProtoMember]
            public SerializableDefinitionId Id;

            [ProtoMember]
            public float Amount = 0;
        }

        [ProtoMember, DefaultValue(null)]
        [XmlArrayItem("Group")]
        public string[] AppliedGroups;

        [ProtoMember]
        public float SpawnProbability = 1;

        [ProtoMember]
        public MyAreaTransformType AreaTransformType = MyAreaTransformType.ENRICHING;

        [ProtoMember, DefaultValue(false)]
        public bool Regrowable = false;

        [ProtoMember, DefaultValue(0)]
        public float GrowTime = 0;

        [ProtoMember, DefaultValue(null)]
        [XmlArrayItem("Step")]
        public GrowthStep[] GrowthSteps = null;

        [ProtoMember]
        public int PostGatherStep = 0;

        [ProtoMember]
        public int GatherableStep = -1;

        [ProtoMember, DefaultValue(null)]
        public GatheredItemDef GatheredItem = null;

        [ProtoMember, DefaultValue(0)]
        public float DecayTime = 0;
    }
}
