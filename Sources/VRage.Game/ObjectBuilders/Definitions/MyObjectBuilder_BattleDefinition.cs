﻿using System.ComponentModel;
using System.Xml.Serialization;
using ProtoBuf;
using VRage.ObjectBuilders;

namespace VRage.Game
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_BattleDefinition : MyObjectBuilder_DefinitionBase
    {
        [ProtoMember]
        public MyObjectBuilder_Toolbar DefaultToolbar;

        [XmlArrayItem("Block")]
        [ProtoMember, DefaultValue(null)]
        public SerializableDefinitionId[] SpawnBlocks = null;

        // Defender king statue entity damage 0 - 1 (removes integrity = DefenderEntityDamage * MaxIntegrity).
        [ProtoMember, DefaultValue(0.067f)]
        public float DefenderEntityDamage = 0.067f;

        [XmlArrayItem("Blueprint")]
        [ProtoMember, DefaultValue(null)]
        public string[] DefaultBlueprints = null;
    }
}
