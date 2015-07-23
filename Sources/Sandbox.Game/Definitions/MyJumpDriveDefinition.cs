using Sandbox.Common.ObjectBuilders.Definitions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sandbox.Definitions
{
    [MyDefinitionType(typeof(MyObjectBuilder_JumpDriveDefinition))]
    public class MyJumpDriveDefinition : MyCubeBlockDefinition
    {
        public float RequiredPowerInput;
        public float PowerNeededForJump;
        public double MaxJumpDistance;
        public double MaxJumpMass;

        protected override void Init(MyObjectBuilder_DefinitionBase builder)
        {
            base.Init(builder);

            var jumpDriveBuilder = builder as MyObjectBuilder_JumpDriveDefinition;

            RequiredPowerInput = jumpDriveBuilder.RequiredPowerInput;
            PowerNeededForJump = jumpDriveBuilder.PowerNeededForJump;
            MaxJumpDistance = jumpDriveBuilder.MaxJumpDistance;
            MaxJumpMass = jumpDriveBuilder.MaxJumpMass;
        }
    }
}
