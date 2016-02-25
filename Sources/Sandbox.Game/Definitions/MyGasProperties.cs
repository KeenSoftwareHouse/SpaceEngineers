using Sandbox.Common.ObjectBuilders.Definitions;
using VRage.Game;
using VRage.Game.Definitions;
using VRage.Game.ObjectBuilders.Definitions;

namespace Sandbox.Definitions
{
	[MyDefinitionType(typeof(MyObjectBuilder_GasProperties))]
	public class MyGasProperties : MyDefinitionBase
	{
		public float EnergyDensity;

		protected override void Init(MyObjectBuilder_DefinitionBase builder)
		{
			base.Init(builder);

			var objectBuilder = builder as MyObjectBuilder_GasProperties;

			EnergyDensity = objectBuilder.EnergyDensity;
		}

		public override MyObjectBuilder_DefinitionBase GetObjectBuilder()
		{
			var builder = base.GetObjectBuilder() as MyObjectBuilder_GasProperties;

			builder.EnergyDensity = EnergyDensity;

			return builder;
		}
	}
}
