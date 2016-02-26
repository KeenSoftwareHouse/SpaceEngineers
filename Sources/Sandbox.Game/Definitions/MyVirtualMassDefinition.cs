using Sandbox.Common.ObjectBuilders.Definitions;
using VRage.Game;
using VRage.Game.Definitions;
using VRage.Utils;


namespace Sandbox.Definitions
{
    [MyDefinitionType(typeof(MyObjectBuilder_VirtualMassDefinition))]
    public class MyVirtualMassDefinition : MyCubeBlockDefinition
    {
	    public MyStringHash ResourceSinkGroup;
        public float RequiredPowerInput;
        public float VirtualMass;

        protected override void Init(MyObjectBuilder_DefinitionBase builder)
        {
            base.Init(builder);

            var obMass = builder as MyObjectBuilder_VirtualMassDefinition;
            MyDebug.AssertDebug(obMass != null, "Initializing virtual mass definition using wrong object builder.");
	        ResourceSinkGroup = MyStringHash.GetOrCompute(obMass.ResourceSinkGroup);
            RequiredPowerInput = obMass.RequiredPowerInput;
            VirtualMass = obMass.VirtualMass;
        }
    }
}
