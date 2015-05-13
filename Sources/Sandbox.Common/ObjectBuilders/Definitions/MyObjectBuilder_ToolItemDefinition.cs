using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using Sandbox.Common.ObjectBuilders.Definitions;
using Sandbox.Common.ObjectBuilders;
using System.ComponentModel;

namespace Medieval.ObjectBuilders.Definitions
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_ToolItemDefinition : MyObjectBuilder_PhysicalItemDefinition
    {
        [ProtoContract]
        public class MyVoxelMiningDefinition
        {
            [ProtoMember(1), DefaultValue(null)]
            public string MinedOre = null;

            // Mine counts needed to spawn prefab
            [ProtoMember(2), DefaultValue(0)]
            public int HitCount = 0;

            [ProtoMember(3), DefaultValue(null)]
            public string Prefab = null;

            // Removed radius from voxel map.
            [ProtoMember(4), DefaultValue(0f)]
            public float RemovedRadius = 0f;
        }

        [XmlArrayItem("Mining")]
        [ProtoMember(1), DefaultValue(null)]
        public MyVoxelMiningDefinition[] VoxelMinings = null;

        [ProtoMember(2), DefaultValue(0)]
        public int AttackStartFrame = 0;
        [ProtoMember(3), DefaultValue(0)]
        public int AttackEndFrame = 0;

        [ProtoMember(4), DefaultValue(0)]
        public int MiningStartFrame = 0;
        [ProtoMember(5), DefaultValue(0)]
        public int MiningEndFrame = 0;

        [ProtoMember(6), DefaultValue(0)]
        public int CuttingStartFrame = 0;
        [ProtoMember(7), DefaultValue(0)]
        public int CuttingEndFrame = 0;

        [ProtoMember(8), DefaultValue(0)]
        public float CuttingEfficiency = 0;

    }
}
