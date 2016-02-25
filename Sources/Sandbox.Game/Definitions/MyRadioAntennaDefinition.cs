using System.Diagnostics;
using Sandbox.Common.ObjectBuilders.Definitions;
using VRage.Game;
using VRage.Game.Definitions;
using VRage.Utils;

namespace Sandbox.Definitions
{
	[MyDefinitionType(typeof(MyObjectBuilder_RadioAntennaDefinition))]
	public class MyRadioAntennaDefinition : MyCubeBlockDefinition
	{
		public MyStringHash ResourceSinkGroup;

		protected override void Init(MyObjectBuilder_DefinitionBase builder)
		{
			base.Init(builder);

			var antennaBuilder = (MyObjectBuilder_RadioAntennaDefinition)builder;
			Debug.Assert(antennaBuilder != null);

			ResourceSinkGroup = MyStringHash.GetOrCompute(antennaBuilder.ResourceSinkGroup);
		}
	}
}
