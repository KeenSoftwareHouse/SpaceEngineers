using System;
using System.Collections.Generic;
using Sandbox.Game.WorldEnvironment.ObjectBuilders;
using VRage.Game;
using VRage.Game.Definitions;

namespace Sandbox.Game.WorldEnvironment.Definitions
{
    [MyDefinitionType(typeof(MyObjectBuilder_WorldEnvironmentBase))]
    public abstract class MyWorldEnvironmentDefinition : MyDefinitionBase
    {
        public abstract Type SectorType { get; }
        public int SyncLod;

        public MyEnvironmentSector CreateSector()
        {
            return (MyEnvironmentSector)Activator.CreateInstance(SectorType);
        }

        public MyRuntimeEnvironmentItemInfo[] Items;

        public double SectorSize;

        public double ItemDensity;

        protected override void Init(MyObjectBuilder_DefinitionBase builder)
        {
            base.Init(builder);

            var ob = (MyObjectBuilder_WorldEnvironmentBase)builder;

            SectorSize = ob.SectorSize;

            ItemDensity = ob.ItemsPerSqMeter;

            SyncLod = ob.MaxSyncLod;
        }
    }
}
