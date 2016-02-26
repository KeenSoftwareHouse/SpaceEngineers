using ProtoBuf;
using VRageMath;

namespace VRage.Game
{
    [ProtoContract]
    public class MyBBMemoryTarget : MyBBMemoryValue
    {
        [ProtoMember]
        public MyAiTargetEnum TargetType = MyAiTargetEnum.NO_TARGET;

        [ProtoMember]
        public long? EntityId = null;

        [ProtoMember]
        public Vector3D? Position = null;

		[ProtoMember]
		public int? TreeId = null;

        [ProtoMember]
        public ushort? CompoundId = null;

        public Vector3I BlockPosition { get { return Vector3I.Round(Position.Value); } }
        public Vector3I VoxelPosition { get { return Vector3I.Round(Position.Value); } }

        public MyBBMemoryTarget()
        {
        }

        public static void UnsetTarget(ref MyBBMemoryTarget target)
        {
            if (target == null) target = new MyBBMemoryTarget();
            target.TargetType = MyAiTargetEnum.NO_TARGET;
        }

        public static void SetTargetEntity(ref MyBBMemoryTarget target, MyAiTargetEnum targetType, long entityId, Vector3D? position = null)
        {
            if (target == null) target = new MyBBMemoryTarget();
            target.TargetType = targetType;
            target.EntityId = entityId;
            target.TreeId = null;
            target.Position = position;
        }

        public static void SetTargetPosition(ref MyBBMemoryTarget target, Vector3D position)
        {
            if (target == null) target = new MyBBMemoryTarget();
            target.TargetType = MyAiTargetEnum.POSITION;
            target.EntityId = null;
            target.TreeId = null;
            target.Position = position;
        }

        public static void SetTargetCube(ref MyBBMemoryTarget target, Vector3I blockPosition, long gridEntityId)
        {
            if (target == null) target = new MyBBMemoryTarget();
            target.TargetType = MyAiTargetEnum.CUBE;
            target.EntityId = gridEntityId;
            target.TreeId = null;
            target.Position = new Vector3D(blockPosition);
        }

        public static void SetTargetVoxel(ref MyBBMemoryTarget target, Vector3I voxelPosition, long entityId)
        {
            if (target == null) target = new MyBBMemoryTarget();
            target.TargetType = MyAiTargetEnum.VOXEL;
            target.EntityId = entityId;
            target.TreeId = null;
            target.Position = new Vector3D(voxelPosition);
        }

        public static void SetTargetTree(ref MyBBMemoryTarget target, Vector3D treePosition, long entityId, int treeId)
        {
            if (target == null) target = new MyBBMemoryTarget();
            target.TargetType = MyAiTargetEnum.ENVIRONMENT_ITEM;
            target.EntityId = entityId;
            target.TreeId = treeId;
            target.Position = treePosition;
		}

        public static void SetTargetCompoundBlock(ref MyBBMemoryTarget target, Vector3I blockPosition, long entityId, ushort compoundId)
        {
            if (target == null) target = new MyBBMemoryTarget();
            target.TargetType = MyAiTargetEnum.COMPOUND_BLOCK;
            target.EntityId = entityId;
            target.CompoundId = compoundId;
            target.Position = blockPosition;
        }
    }
}
