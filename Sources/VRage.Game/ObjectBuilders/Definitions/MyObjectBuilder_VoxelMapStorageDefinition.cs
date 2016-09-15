using ProtoBuf;
using VRage.Data;
using VRage.ObjectBuilders;
using VRage.Voxels;

namespace VRage.Game
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_VoxelMapStorageDefinition : MyObjectBuilder_DefinitionBase
    {
        [ProtoMember, ModdableContentFile(MyVoxelConstants.FILE_EXTENSION)]
        public string StorageFile;
    }
}
