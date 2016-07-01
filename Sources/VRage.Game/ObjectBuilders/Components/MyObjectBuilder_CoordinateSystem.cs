using ProtoBuf;
using System.Collections.Generic;
using VRage.ObjectBuilders;

namespace VRage.Game.ObjectBuilders.Components
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_CoordinateSystem : MyObjectBuilder_SessionComponent
    {
        [ProtoContract]
        public struct CoordSysInfo
        {
            [ProtoMember]
            public long Id;
            [ProtoMember]
            public long EntityCount;
            [ProtoMember]
            public SerializableQuaternion Rotation;
            [ProtoMember]
            public SerializableVector3D Position;
        }

        [ProtoMember]
        public long LastCoordSysId = 1;
        [ProtoMember]
        public List<CoordSysInfo> CoordSystems = new List<CoordSysInfo>();

    }
}
