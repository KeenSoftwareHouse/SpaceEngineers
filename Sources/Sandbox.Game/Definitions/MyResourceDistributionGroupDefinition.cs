using Sandbox.Common.ObjectBuilders.Definitions;
using VRage.Game;
using VRage.Game.Definitions;
using VRage.Game.ObjectBuilders.Definitions;

namespace Sandbox.Definitions
{
	[MyDefinitionType(typeof(MyObjectBuilder_ResourceDistributionGroup))]
	public class MyResourceDistributionGroupDefinition : MyDefinitionBase
	{
		public int Priority;
		public bool IsSource;
		public bool IsAdaptible;

		protected override void Init(MyObjectBuilder_DefinitionBase builder)
		{
			base.Init(builder);

			var objectBuilder = builder as MyObjectBuilder_ResourceDistributionGroup;

			IsSource = objectBuilder.IsSource;
			Priority = objectBuilder.Priority;
			IsAdaptible = objectBuilder.IsAdaptible;
		}
	}
}
