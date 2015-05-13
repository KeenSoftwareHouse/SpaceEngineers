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
    }

    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_AiTarget : MyObjectBuilder_Base
    {
        [ProtoContract]
        public class UnreachableEntitiesData
        {
            [ProtoMember(1)]
            public long UnreachableEntityId;

            [ProtoMember(2)]
            public int Timeout;
        }

        [ProtoMember(1)]
        public MyAiTargetEnum CurrentTarget = MyAiTargetEnum.NO_TARGET;

        [ProtoMember(2)]
        public long? EntityId = null;

        [ProtoMember(3)]
        public Vector3I TargetCube = Vector3I.Zero;

        [ProtoMember(4)]
        public Vector3D TargetPosition = Vector3D.Zero;

        [ProtoMember(5)]
        public List<UnreachableEntitiesData> UnreachableEntities = null;
    }
}
