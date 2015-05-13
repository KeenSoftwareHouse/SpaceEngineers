using Sandbox.Common.ObjectBuilders;
using Sandbox.Common.ObjectBuilders.Definitions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRageMath;

namespace Sandbox.Definitions
{
    [MyDefinitionType(typeof(MyObjectBuilder_OxygenFarmDefinition))]
    public class MyOxygenFarmDefinition : MyCubeBlockDefinition
    {
        public Vector3 PanelOrientation;
        public bool IsTwoSided;
        public float PanelOffset;
        public float MaxOxygenOutput;
        public float OperationalPowerConsumption;

        protected override void Init(MyObjectBuilder_DefinitionBase builder)
        {
            base.Init(builder);

            var oxygenFarmBuilder = builder as MyObjectBuilder_OxygenFarmDefinition;
            PanelOrientation = oxygenFarmBuilder.PanelOrientation;
            IsTwoSided = oxygenFarmBuilder.TwoSidedPanel;
            PanelOffset = oxygenFarmBuilder.PanelOffset;
            MaxOxygenOutput = oxygenFarmBuilder.MaxOxygenOutput;
            OperationalPowerConsumption = oxygenFarmBuilder.OperationalPowerConsumption;
        }
    }
}
