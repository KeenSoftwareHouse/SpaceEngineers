using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel.Security;
using System.Text;
using System.Xml.Serialization;
using Sandbox.Common.ObjectBuilders.Definitions;
using Sandbox.Definitions;
using Sandbox.Game.WorldEnvironment.ObjectBuilders;
using VRage.ObjectBuilders;
using VRage;
using VRage.Game;
using VRage.Library.Utils;

namespace Sandbox.Game.WorldEnvironment.Definitions
{
    public abstract class MyEnvironmentModuleBase : IMyEnvironmentModule
    {
        protected MyLogicalEnvironmentSectorBase Sector;

        public virtual void ProcessItems(Dictionary<short, MyLodEnvironmentItemSet> items, List<MySurfaceParams> surfaceParamsPerLod, int[] surfaceParamLodOffsets, int changedLodMin, int changedLodMax)
        {
            using (var batch = new MyEnvironmentModelUpdateBatch(Sector))
                foreach (var group in items)
                {
                    MyRuntimeEnvironmentItemInfo it;
                    Sector.GetItemDefinition((ushort)group.Key, out it);
                    MyDefinitionId modelCollection = new MyDefinitionId(typeof(MyObjectBuilder_PhysicalModelCollectionDefinition), it.Subtype);

                    MyPhysicalModelCollectionDefinition modelCollectionDef = MyDefinitionManager.Static.GetDefinition<MyPhysicalModelCollectionDefinition>(modelCollection);
                    if (modelCollectionDef != null)
                    {
                        foreach (var position in group.Value.Items)
                        {
                            var sample = MyHashRandomUtils.UniformFloatFromSeed(position);
                            var modelDef = modelCollectionDef.Items.Sample(sample);

                            batch.Add(modelDef, position);
                        }
                    }
                }
        }

        public virtual void Init(MyLogicalEnvironmentSectorBase sector, MyObjectBuilder_Base ob)
        {
            Sector = sector;
        }

        public abstract void Close();

        public abstract MyObjectBuilder_EnvironmentModuleBase GetObjectBuilder();

        public abstract void OnItemEnable(int item, bool enable);

        public abstract void HandleSyncEvent(int logicalItem, object data, bool fromClient);

        public virtual void DebugDraw()
        { }
    }
}
