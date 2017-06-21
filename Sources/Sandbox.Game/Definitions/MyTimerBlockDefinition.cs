using System.Diagnostics;
using Sandbox.Common.ObjectBuilders.Definitions;
using VRage.Game;
using VRage.Game.Definitions;
using VRage.Utils;

namespace Sandbox.Definitions
{
	[MyDefinitionType(typeof(MyObjectBuilder_TimerBlockDefinition))]
	public class MyTimerBlockDefinition : MyCubeBlockDefinition
	{
		public MyStringHash ResourceSinkGroup;
        public string TimerSoundStart;
        public string TimerSoundMid;
        public string TimerSoundEnd;

		protected override void Init(MyObjectBuilder_DefinitionBase builder)
		{
			base.Init(builder);

			var timerBuilder = (MyObjectBuilder_TimerBlockDefinition)builder;
			Debug.Assert(timerBuilder != null);

			ResourceSinkGroup = MyStringHash.GetOrCompute(timerBuilder.ResourceSinkGroup);

            TimerSoundStart = timerBuilder.TimerSoundStart;
            TimerSoundMid = timerBuilder.TimerSoundMid;
            TimerSoundEnd = timerBuilder.TimerSoundEnd;
		}
	}
}
