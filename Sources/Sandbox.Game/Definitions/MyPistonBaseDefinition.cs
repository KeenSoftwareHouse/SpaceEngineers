using Sandbox.Common.ObjectBuilders.Definitions;
using VRage.Game;
using VRage.Game.Definitions;
using VRage.Utils;

namespace Sandbox.Definitions
{
    [MyDefinitionType(typeof(MyObjectBuilder_PistonBaseDefinition))]
    public class MyPistonBaseDefinition : MyMechanicalConnectionBlockBaseDefinition
    {
        public float Minimum;
        public float Maximum;
        public float MaxVelocity;
	    public MyStringHash ResourceSinkGroup;
        public float RequiredPowerInput;

        protected override void Init(MyObjectBuilder_DefinitionBase builder)
        {
            base.Init(builder);

            var ob = (MyObjectBuilder_PistonBaseDefinition)builder;
            Minimum = ob.Minimum;
            Maximum = ob.Maximum;
            MaxVelocity = ob.MaxVelocity;
	        ResourceSinkGroup = MyStringHash.GetOrCompute(ob.ResourceSinkGroup);
            RequiredPowerInput = ob.RequiredPowerInput;
        }
    }
}
