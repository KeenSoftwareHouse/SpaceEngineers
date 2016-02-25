using Sandbox.Common.ObjectBuilders.Definitions;
using VRage.Game;
using VRage.Game.Definitions;
using VRage.Utils;

namespace Sandbox.Definitions
{
    [MyDefinitionType(typeof(MyObjectBuilder_OreDetectorDefinition))]
    public class MyOreDetectorDefinition : MyCubeBlockDefinition
    {
	    public MyStringHash ResourceSinkGroup;
        public float MaximumRange;

        protected override void Init(MyObjectBuilder_DefinitionBase builder)
        {
            base.Init(builder);
            var ob = builder as MyObjectBuilder_OreDetectorDefinition;
	        ResourceSinkGroup = MyStringHash.GetOrCompute(ob.ResourceSinkGroup);
            MaximumRange = ob.MaximumRange;
        }
    }
}
