using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sandbox.Game.WorldEnvironment.ObjectBuilders;
using VRage.Game;
using VRage.Game.Definitions;
using VRage.Utils;

namespace Sandbox.Game.WorldEnvironment.Definitions
{
    [MyDefinitionType(typeof(MyObjectBuilder_VoxelMapCollectionDefinition))]
    public class MyVoxelMapCollectionDefinition : MyDefinitionBase
    {
        public MyDiscreteSampler<MyDefinitionId> StorageFiles;

        public MyStringHash Modifier;

        protected override void Init(MyObjectBuilder_DefinitionBase builder)
        {
            base.Init(builder);

            var ob = builder as MyObjectBuilder_VoxelMapCollectionDefinition;

            if (ob == null)
                return;

            MyDebug.AssertDebug(ob.StorageDefs != null);

            List<MyDefinitionId> defs = new List<MyDefinitionId>();
            List<float> probabilities = new List<float>();
            for (int i = 0; i < ob.StorageDefs.Length; i++)
            {
                var storage = ob.StorageDefs[i];
                defs.Add(new MyDefinitionId(typeof(MyObjectBuilder_VoxelMapStorageDefinition), storage.Storage));
                probabilities.Add(storage.Probability);
            }

            StorageFiles = new MyDiscreteSampler<MyDefinitionId>(defs, probabilities);

            Modifier = MyStringHash.GetOrCompute(ob.Modifier);
        }
    }
}
