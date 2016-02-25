using Sandbox.Common.ObjectBuilders.Definitions;
using VRage.Game;
using VRage.Game.Definitions;
using VRage.Game.ObjectBuilders.Definitions;
using VRage.Utils;
using VRageMath;

namespace Sandbox.Definitions
{
    [MyDefinitionType(typeof(MyObjectBuilder_OxygenFarmDefinition))]
    public class MyOxygenFarmDefinition : MyCubeBlockDefinition
    {
	    public MyStringHash ResourceSinkGroup;
        public MyStringHash ResourceSourceGroup;
        public Vector3 PanelOrientation;
        public bool IsTwoSided;
        public float PanelOffset;
        public MyDefinitionId ProducedGas;
        public float MaxGasOutput;
        public float OperationalPowerConsumption;

        protected override void Init(MyObjectBuilder_DefinitionBase builder)
        {
            base.Init(builder);

            var oxygenFarmBuilder = builder as MyObjectBuilder_OxygenFarmDefinition;
	        ResourceSinkGroup = MyStringHash.GetOrCompute(oxygenFarmBuilder.ResourceSinkGroup);
            ResourceSourceGroup = MyStringHash.GetOrCompute(oxygenFarmBuilder.ResourceSourceGroup);
            PanelOrientation = oxygenFarmBuilder.PanelOrientation;
            IsTwoSided = oxygenFarmBuilder.TwoSidedPanel;
            PanelOffset = oxygenFarmBuilder.PanelOffset;

            MyDefinitionId gasId;
            if (oxygenFarmBuilder.ProducedGas.Id.IsNull())    // Backward compatibility
                gasId = new MyDefinitionId(typeof(MyObjectBuilder_GasProperties), "Oxygen");
            else
                gasId = oxygenFarmBuilder.ProducedGas.Id;

            ProducedGas = gasId;
            MaxGasOutput = oxygenFarmBuilder.ProducedGas.MaxOutputPerSecond;
            OperationalPowerConsumption = oxygenFarmBuilder.OperationalPowerConsumption;
        }
    }
}
