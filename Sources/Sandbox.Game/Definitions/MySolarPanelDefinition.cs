using Sandbox.Common.ObjectBuilders;
using Sandbox.Common.ObjectBuilders.Definitions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.Game;
using VRage.Game.Definitions;
using VRageMath;

namespace Sandbox.Definitions
{
    [MyDefinitionType(typeof(MyObjectBuilder_SolarPanelDefinition))]
    public class MySolarPanelDefinition : MyPowerProducerDefinition
    {
        public Vector3 PanelOrientation;
        public bool IsTwoSided;
        public float PanelOffset;

        protected override void Init(MyObjectBuilder_DefinitionBase builder)
        {
            base.Init(builder);

            var solarPanelBuilder = builder as MyObjectBuilder_SolarPanelDefinition;
            PanelOrientation = solarPanelBuilder.PanelOrientation;
            IsTwoSided = solarPanelBuilder.TwoSidedPanel;
            PanelOffset = solarPanelBuilder.PanelOffset;
        }


    }
}
