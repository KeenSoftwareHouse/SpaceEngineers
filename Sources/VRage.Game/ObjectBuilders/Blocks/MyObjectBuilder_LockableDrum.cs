﻿using ProtoBuf;
using Sandbox.Common.ObjectBuilders;
using VRage.ObjectBuilders;

namespace Medieval.ObjectBuilders.Blocks
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_LockableDrum : MyObjectBuilder_CubeBlock
    {
        [ProtoMember(1)]
        public float MaxRopeLength;

        [ProtoMember(2)]
        public float MinRopeLength;

        [ProtoMember(3)]
        public bool IsUnlocked;
    }
}
