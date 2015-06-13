using ProtoBuf;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using VRage.ObjectBuilders;

namespace Sandbox.Common.ObjectBuilders.Definitions
{
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
            public int SubModelId = -1;

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
        [XmlArrayItem("Item")]
        public EnvItem[] EnvironmentItems = null;

        [ProtoMember, DefaultValue(false)]
        public bool Regrowable = false;

        [ProtoMember, DefaultValue(-1)]
        public float RegrowTime = -1;

        [ProtoMember, DefaultValue(null)]
        [XmlArrayItem("Step")]
        public GrowthStep[] GrowthSteps = null;

        [ProtoMember]
        public int PostGatherStep = 0;

        [ProtoMember]
        public int GatherableStep = 0;

        [ProtoMember, DefaultValue(null)]
        public GatheredItemDef GatheredItem = null;
    }
}
