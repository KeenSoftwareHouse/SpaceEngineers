using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRageMath;

namespace Sandbox.Common.ObjectBuilders.AI
{
    public enum MyAiTargetEnum
    {
        NO_TARGET,
        GRID,
        CUBE,
        CHARACTER,
        POSITION,
        ENTITY,
		ENVIRONMENT_ITEM,
		VOXEL,
    }

    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_AiTarget : MyObjectBuilder_Base
    {
        [ProtoContract]
        public class UnreachableEntitiesData
        {
            [ProtoMember]
            public long UnreachableEntityId;

            [ProtoMember]
            public int Timeout;
        }

        [ProtoMember]
        public MyAiTargetEnum CurrentTarget = MyAiTargetEnum.NO_TARGET;

        [ProtoMember]
        public long? EntityId = null;

        [ProtoMember]
        public Vector3I TargetCube = Vector3I.Zero;

        [ProtoMember]
        public Vector3D TargetPosition = Vector3D.Zero;

        [ProtoMember]
        public List<UnreachableEntitiesData> UnreachableEntities = null;
    }
}
