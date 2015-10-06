using Sandbox.Common.ObjectBuilders.Definitions;
using VRage.Utils;

namespace Sandbox.Definitions
{
	[MyDefinitionType(typeof(MyObjectBuilder_PoweredCargoContainerDefinition))]
	public class MyPoweredCargoContainerDefinition : MyCargoContainerDefinition
	{
		public string ResourceSinkGroup;

		protected override void Init(MyObjectBuilder_DefinitionBase builder)
		{
			base.Init(builder);

			var cargoBuilder = builder as MyObjectBuilder_PoweredCargoContainerDefinition;
			MyDebug.AssertDebug(cargoBuilder != null);

			ResourceSinkGroup = cargoBuilder.ResourceSinkGroup;
		}
	}
}