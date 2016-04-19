using ProtoBuf;
using System.ComponentModel;
using System.Xml.Serialization;
using VRage.Game.Gui;
using VRage.ObjectBuilders;

namespace VRage.Game
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_ToolItemDefinition : MyObjectBuilder_PhysicalItemDefinition
    {
        [ProtoContract]
        public class MyVoxelMiningDefinition
        {
            [ProtoMember, DefaultValue(null)]
            public string MinedOre = null;

            // Mine counts needed to spawn prefab
            [ProtoMember, DefaultValue(0)]
            public int HitCount = 0;

            [ProtoMember, DefaultValue(null)]
            public SerializableDefinitionId PhysicalItemId;

            // Removed radius from voxel map.
            [ProtoMember, DefaultValue(0f)]
            public float RemovedRadius = 0f;

            [ProtoMember, DefaultValue(false)]
            public bool OnlyApplyMaterial = false;
        }

        [ProtoContract]
        public class MyToolActionHitCondition
        {
            [ProtoMember, DefaultValue(null)]
            public string[] EntityType = null;

            [ProtoMember]
            public string Animation;

            [ProtoMember]
            public float AnimationTimeScale = 1f;

            [ProtoMember]
            public string StatsAction;

            [ProtoMember]
            public string StatsActionIfHit;

            [ProtoMember]
            public string StatsModifier;

            [ProtoMember]
            public string StatsModifierIfHit;

            [ProtoMember]
            public string Component;
        }

        [ProtoContract]
        public class MyToolActionDefinition
        {
            [ProtoMember]
            public string Name;

            [ProtoMember, DefaultValue(0)]
            public float StartTime = 0;

            [ProtoMember, DefaultValue(1)]
            public float EndTime = 1;
            
            [ProtoMember, DefaultValue(1f)]
            public float Efficiency = 1f;

            [ProtoMember, DefaultValue(null)]
            public string StatsEfficiency = null;

            [ProtoMember, DefaultValue(null)]
            public string SwingSound = null;

            [ProtoMember, DefaultValue(0f)]
            public float SwingSoundStart = 0f;

            [ProtoMember, DefaultValue(0f)]
            public float HitStart = 0f;

            [ProtoMember, DefaultValue(1f)]
            public float HitDuration = 1f;

            [ProtoMember, DefaultValue(null)]
            public string HitSound = null;
            
            [ProtoMember, DefaultValue(0f)]
            public float CustomShapeRadius;

            [ProtoMember]
            public MyHudTexturesEnum Crosshair = MyHudTexturesEnum.HudOre;
             
            [XmlArrayItem("HitCondition")]
            [ProtoMember, DefaultValue(null)]
            public MyToolActionHitCondition[] HitConditions;

        }

        [XmlArrayItem("Mining")]
        [ProtoMember, DefaultValue(null)]
        public MyVoxelMiningDefinition[] VoxelMinings = null;

        [XmlArrayItem("Action")]
        [ProtoMember, DefaultValue(null)]
        public MyToolActionDefinition[] PrimaryActions = null;

        [XmlArrayItem("Action")]
        [ProtoMember, DefaultValue(null)]
        public MyToolActionDefinition[] SecondaryActions = null;

        [ProtoMember, DefaultValue(1)]
        public float HitDistance = 1;
    }
}
