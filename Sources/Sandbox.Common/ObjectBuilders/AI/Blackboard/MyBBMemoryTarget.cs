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
        [ProtoMember(1)]
        public MyAiTargetEnum TargetType = MyAiTargetEnum.NO_TARGET;

        [ProtoMember(2)]
        public long? EntityId = null;

        [ProtoMember(3)]
        public Vector3D? Position = null;

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

        public void SetTargetEntity(MyAiTargetEnum targetType, long entityId)
        {
            TargetType = targetType;
            EntityId = entityId;
            Position = null;
        }

        public void SetTargetPosition(Vector3D position)
        {
            TargetType = MyAiTargetEnum.POSITION;
            EntityId = null;
            Position = position;
        }

        public void SetTargetCube(Vector3I blockPosition, long entityId)
        {
            TargetType = MyAiTargetEnum.CUBE;
            EntityId = entityId;
            Position = new Vector3D(blockPosition);
        }
    }
}
