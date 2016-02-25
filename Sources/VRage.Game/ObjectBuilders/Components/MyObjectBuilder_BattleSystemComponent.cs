using VRage.ObjectBuilders;
using ProtoBuf;
using VRage.Serialization;

namespace VRage.Game
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_BattleSystemComponent : MyObjectBuilder_SessionComponent
    {
        [ProtoMember]
        public bool IsCastleSiegeMap;

        [ProtoMember]
        public ulong Points;

        [ProtoMember]
        public ulong BaseMapVoxelHandVolumeChanged;

        [ProtoMember]
        public ulong BaseMapSmallGridsPoints;

        [ProtoMember]
        public ulong BaseMapLargeGridsPoints;

        [ProtoMember]
        public SerializableBoundingSphereD[] AttackerSlots;

        [ProtoMember]
        public SerializableBoundingSphereD DefenderSlot;

        [ProtoMember]
        public long Faction1EntityId;
    }
}
