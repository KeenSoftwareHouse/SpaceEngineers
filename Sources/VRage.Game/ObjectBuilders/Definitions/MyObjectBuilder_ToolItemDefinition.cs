using ProtoBuf;
using System.ComponentModel;
using System.Xml.Serialization;
using VRage.ObjectBuilders;

namespace Sandbox.Common.ObjectBuilders.Definitions
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
            public string Prefab = null;

            // Removed radius from voxel map.
            [ProtoMember, DefaultValue(0f)]
            public float RemovedRadius = 0f;
        }

        [ProtoContract]
        public class MyToolActionHitCondition
        {
            [ProtoMember, DefaultValue(null)]
            public string EntityType = null;

            [ProtoMember]
            public string Animation;

            [ProtoMember]
            public string StatsAction;

            [ProtoMember]
            public string Script;
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


        [ProtoMember, DefaultValue(0)]
        public int AttackStartFrame = 0;
        [ProtoMember, DefaultValue(0)]
        public int AttackEndFrame = 0;

        [ProtoMember, DefaultValue(0)]
        public int MiningStartFrame = 0;
        [ProtoMember, DefaultValue(0)]
        public int MiningEndFrame = 0;

        [ProtoMember, DefaultValue(0)]
        public int CuttingStartFrame = 0;
        [ProtoMember, DefaultValue(0)]
        public int CuttingEndFrame = 0;

        [ProtoMember, DefaultValue(0)]
        public float CuttingEfficiency = 0;

		[ProtoMember, DefaultValue(0)]
		public float BuildingEfficiency = 0;

		[ProtoMember, DefaultValue(false)]
		public bool HasDeconstructor = false;

        [ProtoMember, DefaultValue(30)]
        public float ToolDamage = 30;
    }
}
