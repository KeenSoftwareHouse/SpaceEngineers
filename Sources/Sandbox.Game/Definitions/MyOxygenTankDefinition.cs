using Sandbox.Common.ObjectBuilders.Definitions;
using VRage.Utils;

namespace Sandbox.Definitions
{
    [MyDefinitionType(typeof(MyObjectBuilder_OxygenTankDefinition))]
    public class MyOxygenTankDefinition : MyProductionBlockDefinition
    {
        public float Capacity;

        protected override void Init(MyObjectBuilder_DefinitionBase builder)
        {
            base.Init(builder);

            var oxygenTank = builder as MyObjectBuilder_OxygenTankDefinition;
            MyDebug.AssertDebug(oxygenTank != null, "Initializing oxygen tank definition using wrong object builder.");

            Capacity = oxygenTank.Capacity;
        }
    }
}
