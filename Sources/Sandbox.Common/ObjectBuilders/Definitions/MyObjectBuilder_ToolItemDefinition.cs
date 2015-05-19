﻿using ProtoBuf;
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

        [XmlArrayItem("Mining")]
        [ProtoMember, DefaultValue(null)]
        public MyVoxelMiningDefinition[] VoxelMinings = null;

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

    }
}
