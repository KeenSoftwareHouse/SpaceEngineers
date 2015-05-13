using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sandbox.Common.ObjectBuilders.Definitions;
using VRageMath;

namespace Sandbox.Definitions
{
    [MyDefinitionType(typeof(MyObjectBuilder_ConveyorSorterDefinition))]
    public class MyConveyorSorterDefinition : MyCubeBlockDefinition
    {
        public float PowerInput;
        public Vector3 InventorySize;

        protected override void Init(MyObjectBuilder_DefinitionBase builder)
        {
            base.Init(builder);

            var ob = (MyObjectBuilder_ConveyorSorterDefinition)builder;

            PowerInput = ob.PowerInput;
            InventorySize = ob.InventorySize;
        }
    }
}