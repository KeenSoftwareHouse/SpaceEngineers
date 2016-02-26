using Sandbox.Common.ObjectBuilders.Definitions;
using VRage.Game;
using VRage.Game.Definitions;
using VRage.Utils;
using VRageMath;

namespace Sandbox.Definitions
{
    [MyDefinitionType(typeof(MyObjectBuilder_CargoContainerDefinition))]
    public class MyCargoContainerDefinition : MyCubeBlockDefinition
    {
        public Vector3 InventorySize;

        protected override void Init(MyObjectBuilder_DefinitionBase builder)
        {
            base.Init(builder);

            var cargoBuilder = builder as MyObjectBuilder_CargoContainerDefinition;
            MyDebug.AssertDebug(cargoBuilder != null);
            InventorySize = cargoBuilder.InventorySize;
        }
    }
}
