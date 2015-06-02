using ProtoBuf;
using VRage.ObjectBuilders;
using VRage.Serialization;

namespace Sandbox.Common.ObjectBuilders
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
    }
}
