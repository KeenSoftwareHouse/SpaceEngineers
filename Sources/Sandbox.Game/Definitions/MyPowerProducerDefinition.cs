using Sandbox.Common.ObjectBuilders.Definitions;
using System.Diagnostics;
using VRage.Game;
using VRage.Game.Definitions;
using VRage.Utils;

namespace Sandbox.Definitions
{
    [MyDefinitionType(typeof(MyObjectBuilder_PowerProducerDefinition))]
    public class MyPowerProducerDefinition : MyCubeBlockDefinition
    {
	    public MyStringHash ResourceSourceGroup;
        public float MaxPowerOutput;

        protected override void Init(MyObjectBuilder_DefinitionBase builder)
        {
            base.Init(builder);
            var powerProducerBuilder = builder as MyObjectBuilder_PowerProducerDefinition;
			Debug.Assert(powerProducerBuilder != null);
	        if (powerProducerBuilder == null)
		        return;

	        ResourceSourceGroup = MyStringHash.GetOrCompute(powerProducerBuilder.ResourceSourceGroup);
            MaxPowerOutput = powerProducerBuilder.MaxPowerOutput;
        }

    }
}
