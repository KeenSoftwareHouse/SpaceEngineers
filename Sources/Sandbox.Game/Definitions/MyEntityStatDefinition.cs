using Sandbox.Common.ObjectBuilders.Definitions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sandbox.Definitions
{
	[MyDefinitionType(typeof(MyObjectBuilder_EntityStatDefinition))]
	public class MyEntityStatDefinition : MyDefinitionBase
	{
		public float MinValue;

		public float MaxValue;

		public bool EnabledInCreative;

		protected override void Init(MyObjectBuilder_DefinitionBase builder)
		{
			base.Init(builder);

			var objectBuilder = builder as MyObjectBuilder_EntityStatDefinition;

			MinValue = objectBuilder.MinValue;
			MaxValue = objectBuilder.MaxValue;
			EnabledInCreative = objectBuilder.EnabledInCreative;
		}

		public override MyObjectBuilder_DefinitionBase GetObjectBuilder()
		{
			var builder = base.GetObjectBuilder() as MyObjectBuilder_EntityStatDefinition;

			builder.MinValue = MinValue;
			builder.MaxValue = MaxValue;
			builder.EnabledInCreative = EnabledInCreative;

			return builder;
		}
	}
}
