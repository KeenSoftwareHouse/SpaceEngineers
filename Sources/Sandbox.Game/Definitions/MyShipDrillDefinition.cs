using Sandbox.Common.ObjectBuilders;
using Sandbox.Common.ObjectBuilders.Definitions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sandbox.Engine.Utils;
using VRageMath;

namespace Sandbox.Definitions
{
    [MyDefinitionType(typeof(MyObjectBuilder_ShipDrillDefinition))]
    class MyShipDrillDefinition : MyCubeBlockDefinition
    {
        public float SensorRadius;
        public float SensorOffset;
        public Vector3 InventorySize;

        protected override void Init(MyObjectBuilder_DefinitionBase builder)
        {
            base.Init(builder);
            var cbuilder = builder as MyObjectBuilder_ShipDrillDefinition;
            SensorRadius = cbuilder.SensorRadius;
            SensorOffset = cbuilder.SensorOffset;
            InventorySize = cbuilder.InventorySize;
            DeformationRatio = 0.5f;
        }
    }
}
