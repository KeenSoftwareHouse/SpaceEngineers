using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ProtoBuf;
using Sandbox.Common.ObjectBuilders.VRageData;

namespace Sandbox.Common.ObjectBuilders
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_BattleSystemComponent : MyObjectBuilder_SessionComponent
    {
        [ProtoMember(1)]
        public bool IsBattleMap;

        [ProtoMember(2)]
        public ulong Points;

        [ProtoMember(3)]
        public ulong BaseMapVoxelHandVolumeChanged;

        [ProtoMember(4)]
        public SerializableBoundingBoxD[] AttackerSlots;

        [ProtoMember(5)]
        public SerializableBoundingBoxD DefenderSlot;

        [ProtoMember(6)]
        public long DefenderEntityId;
    }
}
