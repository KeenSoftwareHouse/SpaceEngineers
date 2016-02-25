using Sandbox.Common.ObjectBuilders.Definitions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.Game;
using VRage.Game.Definitions;
using VRageMath;

namespace Sandbox.Definitions
{
	[MyDefinitionType(typeof(MyObjectBuilder_EntityStatDefinition))]
	public class MyEntityStatDefinition : MyDefinitionBase
	{
		public struct GuiDefinition
		{
			public float HeightMultiplier;
			public int Priority;
			public Vector3I Color;
            public float CriticalRatio;
            public bool DisplayCriticalDivider;
            public Vector3I CriticalColorFrom;
            public Vector3I CriticalColorTo;
		}

		public float MinValue;

		public float MaxValue;

	    public float DefaultValue;

		public bool EnabledInCreative;

		public string Name;

		public GuiDefinition GuiDef;

		protected override void Init(MyObjectBuilder_DefinitionBase builder)
		{
			base.Init(builder);

			var objectBuilder = builder as MyObjectBuilder_EntityStatDefinition;

			MinValue = objectBuilder.MinValue;
			MaxValue = objectBuilder.MaxValue;
		    DefaultValue = objectBuilder.DefaultValue;
			EnabledInCreative = objectBuilder.EnabledInCreative;
			Name = objectBuilder.Name;

            if (float.IsNaN(DefaultValue)) DefaultValue = MaxValue;

			GuiDef = new GuiDefinition();

			if (objectBuilder.GuiDef != null)
			{
				GuiDef.HeightMultiplier = objectBuilder.GuiDef.HeightMultiplier;
				GuiDef.Priority = objectBuilder.GuiDef.Priority;
				GuiDef.Color = objectBuilder.GuiDef.Color;
                GuiDef.CriticalRatio = objectBuilder.GuiDef.CriticalRatio;
                GuiDef.DisplayCriticalDivider = objectBuilder.GuiDef.DisplayCriticalDivider;
                GuiDef.CriticalColorFrom = objectBuilder.GuiDef.CriticalColorFrom;
                GuiDef.CriticalColorTo = objectBuilder.GuiDef.CriticalColorTo;
			}
		}

		public override MyObjectBuilder_DefinitionBase GetObjectBuilder()
		{
			var builder = base.GetObjectBuilder() as MyObjectBuilder_EntityStatDefinition;

			builder.MinValue = MinValue;
			builder.MaxValue = MaxValue;
            builder.DefaultValue = DefaultValue;
			builder.EnabledInCreative = EnabledInCreative;
			builder.Name = Name;

			builder.GuiDef = new MyObjectBuilder_EntityStatDefinition.GuiDefinition();
			builder.GuiDef.HeightMultiplier = GuiDef.HeightMultiplier;
			builder.GuiDef.Priority = GuiDef.Priority;
			builder.GuiDef.Color = GuiDef.Color;
            builder.GuiDef.CriticalRatio = GuiDef.CriticalRatio;
            builder.GuiDef.DisplayCriticalDivider = GuiDef.DisplayCriticalDivider;
            builder.GuiDef.CriticalColorFrom = GuiDef.CriticalColorFrom;
            builder.GuiDef.CriticalColorTo = GuiDef.CriticalColorTo;

			return builder;
		}
	}
}
