using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRageMath;

namespace Sandbox.Common.ObjectBuilders.AI
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
		public long? TreeId = null;

        public Vector3I BlockPosition { get { return Vector3I.Round(Position.Value); } }

        public MyBBMemoryTarget()
        {
        }

        public MyBBMemoryTarget(MyAiTargetEnum targetType, long entityId)
        {
            SetTargetEntity(targetType, entityId);
        }

        public MyBBMemoryTarget(Vector3D position)
        {
            SetTargetPosition(position);
        }

        public MyBBMemoryTarget(Vector3I blockPosition, long entityId)
        {
            SetTargetCube(blockPosition, entityId);
        }

		public MyBBMemoryTarget(Vector3D position, long environmentId, long itemId)
		{
			TargetType = MyAiTargetEnum.ENVIRONMENT_ITEM;
			EntityId = environmentId;
			TreeId = itemId;
			Position = position;
		}

        public void SetTargetEntity(MyAiTargetEnum targetType, long entityId)
        {
            TargetType = targetType;
            EntityId = entityId;
			TreeId = null;
            Position = null;
        }

        public void SetTargetPosition(Vector3D position)
        {
            TargetType = MyAiTargetEnum.POSITION;
            EntityId = null;
			TreeId = null;
            Position = position;
        }

        public void SetTargetCube(Vector3I blockPosition, long entityId)
        {
            TargetType = MyAiTargetEnum.CUBE;
            EntityId = entityId;
			TreeId = null;
            Position = new Vector3D(blockPosition);
        }

		public void SetTargetTree(Vector3D treePosition, long entityId, long treeId)
		{
			TargetType = MyAiTargetEnum.ENVIRONMENT_ITEM;
			EntityId = entityId;
			TreeId = treeId;
			Position = treePosition;
		}
    }
}
