using Sandbox.Common.ObjectBuilders.Definitions;
using VRage.Game;
using VRage.Game.Definitions;
using VRage.Game.ObjectBuilders.Definitions;
using VRage.Utils;

namespace Sandbox.Definitions
{
    [MyDefinitionType(typeof(MyObjectBuilder_GasTankDefinition))]
    public class MyGasTankDefinition : MyProductionBlockDefinition
    {
        public float Capacity;

	    public MyDefinitionId StoredGasId;
	    public MyStringHash ResourceSourceGroup;

        protected override void Init(MyObjectBuilder_DefinitionBase builder)
        {
            base.Init(builder);

            var gasTankDefinition = builder as MyObjectBuilder_GasTankDefinition;
            MyDebug.AssertDebug(gasTankDefinition != null, "Initializing gas tank definition using wrong object builder.");

            Capacity = gasTankDefinition.Capacity;

            MyDefinitionId gasId;
            if (gasTankDefinition.StoredGasId.IsNull())    // Backward compatibility
                gasId = new MyDefinitionId(typeof(MyObjectBuilder_GasProperties), "Oxygen");
            else
                gasId = gasTankDefinition.StoredGasId;

            StoredGasId = gasId;
			ResourceSourceGroup = MyStringHash.GetOrCompute(gasTankDefinition.ResourceSourceGroup);

        }
    }
}
