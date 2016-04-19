using VRage.Game;
using VRage.Game.Definitions;
using SpaceEngineers.ObjectBuilders.Definitions;
using Sandbox.Definitions;

namespace SpaceEngineers.Definitions
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
