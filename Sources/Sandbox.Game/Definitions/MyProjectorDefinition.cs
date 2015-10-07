using Sandbox.Common.ObjectBuilders.Definitions;
using VRage.Utils;

namespace Sandbox.Definitions
{
    [MyDefinitionType(typeof(MyObjectBuilder_ProjectorDefinition))]
    public class MyProjectorDefinition : MyCubeBlockDefinition
    {
	    public MyStringHash ResourceSinkGroup;
        public float RequiredPowerInput;

        protected override void Init(MyObjectBuilder_DefinitionBase builder)
        {
            base.Init(builder);

            var obProjector = builder as MyObjectBuilder_ProjectorDefinition;
            MyDebug.AssertDebug(obProjector != null, "Initializing camera definition using wrong object builder.!");
	        ResourceSinkGroup = MyStringHash.GetOrCompute(obProjector.ResourceSinkGroup);
            RequiredPowerInput = obProjector.RequiredPowerInput;
        }

    }
}
