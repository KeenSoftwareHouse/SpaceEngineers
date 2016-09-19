using Sandbox.Common.ObjectBuilders;
using Sandbox.Common.ObjectBuilders.Definitions;

using Sandbox.Engine.Utils;
using VRage.Game;
using VRage.Game.Definitions;
using VRage.Utils;

namespace Sandbox.Definitions
{
    [MyDefinitionType(typeof(MyObjectBuilder_GravityGeneratorSphereDefinition))]
    public class MyGravityGeneratorSphereDefinition : MyGravityGeneratorBaseDefinition
    {
        public float MinRadius;
        public float MaxRadius;
        public float BasePowerInput;
        public float ConsumptionPower;

        protected override void Init(MyObjectBuilder_DefinitionBase builder)
        {
            base.Init(builder);

            var obGenerator = builder as MyObjectBuilder_GravityGeneratorSphereDefinition;
            MyDebug.AssertDebug(obGenerator != null, "Initializing thrust definition using wrong object builder.");
            
            MinRadius = obGenerator.MinRadius;
            MaxRadius = obGenerator.MaxRadius;
            BasePowerInput = obGenerator.BasePowerInput;
            ConsumptionPower = obGenerator.ConsumptionPower;
        }
    }
}
