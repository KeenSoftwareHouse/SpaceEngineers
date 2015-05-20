using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.Voxels;
using VRage.Data;

namespace Sandbox.Common.ObjectBuilders.Definitions
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_VoxelMapStorageDefinition : MyObjectBuilder_DefinitionBase
    {
        [ProtoMember, ModdableContentFile(MyVoxelConstants.FILE_EXTENSION)]
        public string StorageFile;
    }
}
