using ProtoBuf;
using System.Collections.Generic;
using VRage.ObjectBuilders;
using VRage.Serialization;
using VRageMath;

namespace VRage.Game
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_World : MyObjectBuilder_Base
    {
        [ProtoMember]
        public MyObjectBuilder_Checkpoint Checkpoint;

        [ProtoMember]
        public MyObjectBuilder_Sector Sector;

        [ProtoMember]
        public SerializableDictionary<string, byte[]> VoxelMaps;

        public List<BoundingBoxD> Clusters;
    }
}
