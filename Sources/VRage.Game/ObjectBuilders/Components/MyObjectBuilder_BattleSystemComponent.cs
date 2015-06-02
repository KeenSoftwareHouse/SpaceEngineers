﻿using VRage.ObjectBuilders;
using ProtoBuf;
using VRage;

namespace Sandbox.Common.ObjectBuilders
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_BattleSystemComponent : MyObjectBuilder_SessionComponent
    {
        [ProtoMember]
        public bool IsBattleMap;

        [ProtoMember]
        public ulong Points;

        [ProtoMember]
        public ulong BaseMapVoxelHandVolumeChanged;

        [ProtoMember]
        public ulong BaseMapSmallGridsPoints;

        [ProtoMember]
        public ulong BaseMapLargeGridsPoints;

        [ProtoMember]
        public SerializableBoundingBoxD[] AttackerSlots;

        [ProtoMember]
        public SerializableBoundingBoxD DefenderSlot;

        [ProtoMember]
        public long DefenderEntityId;
    }
}
