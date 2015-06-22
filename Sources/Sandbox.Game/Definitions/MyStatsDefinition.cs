using Sandbox.Common.ObjectBuilders.Definitions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.ObjectBuilders;

namespace Sandbox.Definitions
{
	[MyDefinitionType(typeof(MyObjectBuilder_StatsDefinition))]
	public class MyStatsDefinition : MyDefinitionBase
	{
		public List<MyDefinitionId> Stats;
		public List<string> Scripts;

		protected override void Init(MyObjectBuilder_DefinitionBase builder)
		{
			base.Init(builder);

			var objectBuilder = builder as MyObjectBuilder_StatsDefinition;

			Stats = new List<MyDefinitionId>();
			foreach(var stat in objectBuilder.Stats)
			{
				Stats.Add(stat);
			}

			Scripts = objectBuilder.Scripts;
		}

		public override MyObjectBuilder_DefinitionBase GetObjectBuilder()
		{
			var builder = base.GetObjectBuilder() as MyObjectBuilder_StatsDefinition;

			builder.Stats = new List<SerializableDefinitionId>();
			foreach(var stat in Stats)
			{
				builder.Stats.Add(stat);
			}
			builder.Scripts = Scripts;

			return builder;
		}
	}
}
