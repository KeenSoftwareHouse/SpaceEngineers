using Sandbox.Common.ObjectBuilders.Definitions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.Game;
using VRage.Game.Definitions;
using VRage.Utils;


namespace Sandbox.Definitions
{
    [MyDefinitionType(typeof(MyObjectBuilder_VoxelMapStorageDefinition))]
    public class MyVoxelMapStorageDefinition : MyDefinitionBase
    {
        public string StorageFile;

        protected override void Init(MyObjectBuilder_DefinitionBase builder)
        {
            base.Init(builder);

            var ob = builder as MyObjectBuilder_VoxelMapStorageDefinition;
            MyDebug.AssertDebug(ob != null, "Initializing voxelmap storage definition using wrong object builder.");
            StorageFile = ob.StorageFile;
        }
    }
}
