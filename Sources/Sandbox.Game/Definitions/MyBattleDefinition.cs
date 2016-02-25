using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Common.ObjectBuilders.Definitions;
using VRage.Game;
using VRage.Game.Definitions;

namespace Sandbox.Definitions
{
    [MyDefinitionType(typeof(MyObjectBuilder_BattleDefinition))]
    public class MyBattleDefinition : MyDefinitionBase
    {
        public MyObjectBuilder_Toolbar DefaultToolbar;
        public MyDefinitionId[] SpawnBlocks;
        public float DefenderEntityDamage;
        public string[] DefaultBlueprints;

        protected override void Init(MyObjectBuilder_DefinitionBase builder)
        {
            base.Init(builder);
            var ob = builder as MyObjectBuilder_BattleDefinition;

            DefaultToolbar = ob.DefaultToolbar;
            DefenderEntityDamage = ob.DefenderEntityDamage;
            DefaultBlueprints = ob.DefaultBlueprints;

            if (ob.SpawnBlocks != null && ob.SpawnBlocks.Length > 0)
            {
                SpawnBlocks = new MyDefinitionId[ob.SpawnBlocks.Length];

                for (int i = 0; i < ob.SpawnBlocks.Length; ++i)
                    SpawnBlocks[i] = ob.SpawnBlocks[i];
            }
        }

        public void Merge(MyBattleDefinition src)
        {
            DefaultToolbar = src.DefaultToolbar;
            DefenderEntityDamage = src.DefenderEntityDamage;
            DefaultBlueprints = src.DefaultBlueprints;

            if (src.SpawnBlocks != null && src.SpawnBlocks.Length > 0)
            {
                SpawnBlocks = new MyDefinitionId[src.SpawnBlocks.Length];

                for (int i = 0; i < src.SpawnBlocks.Length; ++i)
                    SpawnBlocks[i] = src.SpawnBlocks[i];
            }
        }

    }
}
