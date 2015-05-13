using Sandbox.Common.ObjectBuilders;
using Sandbox.Common.ObjectBuilders.Definitions;

using Sandbox.Engine.Utils;

namespace Sandbox.Definitions
{
    [MyDefinitionType(typeof(MyObjectBuilder_GyroDefinition))]
    public class MyGyroDefinition : MyCubeBlockDefinition
    {
        public float ForceMagnitude;
        public float RequiredPowerInput;

        protected override void Init(MyObjectBuilder_DefinitionBase builder)
        {
            base.Init(builder);

            var gyroBuilder = (MyObjectBuilder_GyroDefinition)builder;
            ForceMagnitude = gyroBuilder.ForceMagnitude;
            RequiredPowerInput = gyroBuilder.RequiredPowerInput;
        }
    }
}
