using System.Diagnostics;
using Sandbox.Common.ObjectBuilders.Definitions;
using VRage.Game;
using VRage.Game.Definitions;

namespace Sandbox.Definitions
{
    [MyDefinitionType(typeof(MyObjectBuilder_GyroDefinition))]
    public class MyGyroDefinition : MyCubeBlockDefinition
    {
	    public string ResourceSinkGroup;
        public float ForceMagnitude;
        public float RequiredPowerInput;

        protected override void Init(MyObjectBuilder_DefinitionBase builder)
        {
            base.Init(builder);

            var gyroBuilder = (MyObjectBuilder_GyroDefinition)builder;
			Debug.Assert(gyroBuilder != null);

	        ResourceSinkGroup = gyroBuilder.ResourceSinkGroup;
            ForceMagnitude = gyroBuilder.ForceMagnitude;
            RequiredPowerInput = gyroBuilder.RequiredPowerInput;
        }
    }
}
