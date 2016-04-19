using ProtoBuf;
using VRage.Utils;
using VRage.ObjectBuilders;
using VRage.Game;

namespace VRage.Game
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_BlockItem : MyObjectBuilder_PhysicalObject
    {
        [ProtoMember]
        public SerializableDefinitionId BlockDefId;

        public override bool CanStack(MyObjectBuilder_PhysicalObject a)
        {
            MyObjectBuilder_BlockItem other = a as MyObjectBuilder_BlockItem;
            if (other == null) return false;

            return other.BlockDefId.TypeId == BlockDefId.TypeId && other.BlockDefId.SubtypeId == this.BlockDefId.SubtypeId && a.Flags == this.Flags;
        }

        public override bool CanStack(MyObjectBuilderType typeId, MyStringHash subtypeId, MyItemFlags flags)
        {
            MyDefinitionId defId = new MyDefinitionId(typeId, subtypeId);
            return defId == BlockDefId && flags == this.Flags;
        }

        public override MyDefinitionId GetObjectId()
        {
            return BlockDefId;
        }
    }
}
