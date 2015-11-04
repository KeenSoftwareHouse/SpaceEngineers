using Sandbox.Common.ObjectBuilders.Definitions;
using System.Diagnostics;
using VRage.Utils;

namespace Sandbox.Definitions
{
    [MyDefinitionType(typeof(MyObjectBuilder_JumpDriveDefinition))]
    public class MyJumpDriveDefinition : MyCubeBlockDefinition
    {
	    public MyStringHash ResourceSinkGroup;
        public float RequiredPowerInput;
        public float PowerNeededForJump;
        public double MaxJumpDistance;
        public double MaxJumpMass;

        protected override void Init(MyObjectBuilder_DefinitionBase builder)
        {
            base.Init(builder);

            var jumpDriveBuilder = builder as MyObjectBuilder_JumpDriveDefinition;
			Debug.Assert(jumpDriveBuilder != null);

	        ResourceSinkGroup = MyStringHash.GetOrCompute(jumpDriveBuilder.ResourceSinkGroup);
            RequiredPowerInput = jumpDriveBuilder.RequiredPowerInput;
            PowerNeededForJump = jumpDriveBuilder.PowerNeededForJump;
            MaxJumpDistance = jumpDriveBuilder.MaxJumpDistance;
            MaxJumpMass = jumpDriveBuilder.MaxJumpMass;
        }
    }
}
