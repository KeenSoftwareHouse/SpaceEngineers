using System.Diagnostics;
using Sandbox.Common.ObjectBuilders.Definitions;
using VRage.Game;
using VRage.Game.Definitions;
using VRage.Utils;

namespace Sandbox.Definitions
{
	[MyDefinitionType(typeof(MyObjectBuilder_ProgrammableBlockDefinition))]
	public class MyProgrammableBlockDefinition : MyCubeBlockDefinition
	{
		public MyStringHash ResourceSinkGroup;

		protected override void Init(MyObjectBuilder_DefinitionBase builder)
		{
			base.Init(builder);

			var blockBuilder = (MyObjectBuilder_ProgrammableBlockDefinition)builder;
			Debug.Assert(blockBuilder != null);

			ResourceSinkGroup = MyStringHash.GetOrCompute(blockBuilder.ResourceSinkGroup);
		}
	}
}
