using System.Diagnostics;
using Sandbox.Common.ObjectBuilders.Definitions;
using VRage.Game;
using VRage.Game.Definitions;

namespace Sandbox.Definitions
{
	[MyDefinitionType(typeof(MyObjectBuilder_BeaconDefinition))]
	public class MyBeaconDefinition : MyCubeBlockDefinition
	{
		public string ResourceSinkGroup;

		protected override void Init(MyObjectBuilder_DefinitionBase builder)
		{
			base.Init(builder);

			var beaconBuilder = (MyObjectBuilder_BeaconDefinition)builder;
			Debug.Assert(beaconBuilder != null);

			ResourceSinkGroup = beaconBuilder.ResourceSinkGroup;
		}
	}
}
