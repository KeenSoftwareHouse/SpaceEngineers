using Sandbox.Common.ObjectBuilders.Definitions;
using VRage.Game;
using VRage.Game.Definitions;
using VRage.Game.ObjectBuilders.Definitions;

namespace Sandbox.Definitions
{
    [MyDefinitionType(typeof(MyObjectBuilder_OxygenContainerDefinition))]
    public class MyOxygenContainerDefinition : MyPhysicalItemDefinition
    {
        public float Capacity;
	    public MyDefinitionId StoredGasId;

        protected override void Init(MyObjectBuilder_DefinitionBase builder)
        {
            base.Init(builder);

            var ob = builder as MyObjectBuilder_OxygenContainerDefinition;

            Capacity = ob.Capacity;

            MyDefinitionId gasId;
            if (ob.StoredGasId.IsNull())    // Backward compatibility
                gasId = new MyDefinitionId(typeof(MyObjectBuilder_GasProperties), "Oxygen");
            else
                gasId = ob.StoredGasId;

	        StoredGasId = gasId;
        }
    }
}
