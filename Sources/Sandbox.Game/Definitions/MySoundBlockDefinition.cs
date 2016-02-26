using System.Diagnostics;
using Sandbox.Common.ObjectBuilders.Definitions;
using VRage.Game;
using VRage.Game.Definitions;
using VRage.Utils;

namespace Sandbox.Definitions
{
	[MyDefinitionType(typeof(MyObjectBuilder_SoundBlockDefinition))]
	public class MySoundBlockDefinition : MyCubeBlockDefinition
	{
		public MyStringHash ResourceSinkGroup;

		protected override void Init(MyObjectBuilder_DefinitionBase builder)
		{
			base.Init(builder);

			var soundBlockBuilder = (MyObjectBuilder_SoundBlockDefinition)builder;
			Debug.Assert(soundBlockBuilder != null);

			ResourceSinkGroup = MyStringHash.GetOrCompute(soundBlockBuilder.ResourceSinkGroup);
		}
	}
}
