using Sandbox.Common.ObjectBuilders.Definitions;
using System.Diagnostics;
using VRage.Game;
using VRage.Game.Definitions;
using VRage.Utils;

namespace Sandbox.Definitions
{
    [MyDefinitionType(typeof(MyObjectBuilder_ShipDrillDefinition))]
    class MyShipDrillDefinition : MyCubeBlockDefinition
    {
	    public MyStringHash ResourceSinkGroup;
        public float SensorRadius;
        public float SensorOffset;
        public float CutOutOffset;
        public float CutOutRadius;

        protected override void Init(MyObjectBuilder_DefinitionBase builder)
        {
            base.Init(builder);
            var cbuilder = builder as MyObjectBuilder_ShipDrillDefinition;
			Debug.Assert(cbuilder != null);

	        ResourceSinkGroup = MyStringHash.GetOrCompute(cbuilder.ResourceSinkGroup);
            SensorRadius = cbuilder.SensorRadius;
            SensorOffset = cbuilder.SensorOffset;
            CutOutOffset = cbuilder.CutOutOffset;
            CutOutRadius = cbuilder.CutOutRadius;
        }
    }
}
