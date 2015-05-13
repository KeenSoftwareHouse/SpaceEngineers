using Sandbox.Common.ObjectBuilders;
using Sandbox.Common.ObjectBuilders.Definitions;

using VRageMath;

namespace Sandbox.Definitions
{
    [MyDefinitionType(typeof(MyObjectBuilder_BatteryBlockDefinition))]
    public class MyBatteryBlockDefinition : MyPowerProducerDefinition
    {
        public float MaxStoredPower;
        public float RequiredPowerInput;

        protected override void Init(MyObjectBuilder_DefinitionBase builder)
        {
            base.Init(builder);
            var batteryBlockBuilder = builder as MyObjectBuilder_BatteryBlockDefinition;
            MaxStoredPower = batteryBlockBuilder.MaxStoredPower;
            RequiredPowerInput = batteryBlockBuilder.RequiredPowerInput;
        }
    }
}
