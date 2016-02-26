using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sandbox.Common.ObjectBuilders.Definitions;
using VRage.Game;
using VRage.Game.Definitions;

namespace Sandbox.Definitions
{
    [MyDefinitionType(typeof(MyObjectBuilder_UpgradeModuleDefinition))]
    class MyUpgradeModuleDefinition : MyCubeBlockDefinition
    {
        public MyUpgradeModuleInfo[] Upgrades;

        protected override void Init(MyObjectBuilder_DefinitionBase builder)
        {
            base.Init(builder);

            var upgradeDef = builder as MyObjectBuilder_UpgradeModuleDefinition;

            Upgrades = upgradeDef.Upgrades;
        }
    }
}
