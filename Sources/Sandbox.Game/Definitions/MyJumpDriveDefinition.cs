using Sandbox.Common.ObjectBuilders.Definitions;
using System.Diagnostics;
using VRage.Game;
using VRage.Game.Definitions;
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
        public float JumpDelay;

        protected override void Init(MyObjectBuilder_DefinitionBase builder)
        {
            base.Init(builder);

            var ob = builder as MyObjectBuilder_JumpDriveDefinition;
			Debug.Assert(ob != null);

	        ResourceSinkGroup = MyStringHash.GetOrCompute(ob.ResourceSinkGroup);
            RequiredPowerInput = ob.RequiredPowerInput;
            PowerNeededForJump = ob.PowerNeededForJump;
            MaxJumpDistance = ob.MaxJumpDistance;
            MaxJumpMass = ob.MaxJumpMass;
            JumpDelay = ob.JumpDelay;
        }
    }
}
