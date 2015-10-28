using Sandbox.Common.ObjectBuilders.Definitions;
using System.Diagnostics;
using VRage.Utils;

namespace Sandbox.Definitions
{
    [MyDefinitionType(typeof(MyObjectBuilder_ShipDrillDefinition))]
    class MyShipDrillDefinition : MyCubeBlockDefinition
    {
	    public MyStringHash ResourceSinkGroup;
        public float SensorRadius;
        public float SensorOffset;

        protected override void Init(MyObjectBuilder_DefinitionBase builder)
        {
            base.Init(builder);
            var cbuilder = builder as MyObjectBuilder_ShipDrillDefinition;
			Debug.Assert(cbuilder != null);

	        ResourceSinkGroup = MyStringHash.GetOrCompute(cbuilder.ResourceSinkGroup);
            SensorRadius = cbuilder.SensorRadius;
            SensorOffset = cbuilder.SensorOffset;
            DeformationRatio = 0.5f;
        }
    }
}
