using System.Diagnostics;
using Sandbox.Common.ObjectBuilders.Definitions;
using VRage.Game;
using VRage.Game.Definitions;
using VRage.Utils;
using VRageMath;

namespace Sandbox.Definitions
{
    [MyDefinitionType(typeof(MyObjectBuilder_ConveyorSorterDefinition))]
    public class MyConveyorSorterDefinition : MyCubeBlockDefinition
    {
	    public MyStringHash ResourceSinkGroup;
        public float PowerInput;
        public Vector3 InventorySize;

        protected override void Init(MyObjectBuilder_DefinitionBase builder)
        {
            base.Init(builder);

            var ob = (MyObjectBuilder_ConveyorSorterDefinition)builder;
			Debug.Assert(ob != null);

	        ResourceSinkGroup = MyStringHash.GetOrCompute(ob.ResourceSinkGroup);
            PowerInput = ob.PowerInput;
            InventorySize = ob.InventorySize;
        }
    }
}