using ProtoBuf;
using System.Xml.Serialization;
using VRage.ObjectBuilders;

namespace VRage.Game
{
	[ProtoContract]
	[MyObjectBuilderDefinition]
	public class MyObjectBuilder_EntityStatDefinition : MyObjectBuilder_DefinitionBase
	{
		[ProtoContract]
		public class GuiDefinition
		{
			[ProtoMember]
			public float HeightMultiplier = 1.0f;

			[ProtoMember]
			public int Priority = 1;

			[ProtoMember]
			public SerializableVector3I Color = new SerializableVector3I(255, 255, 255);

            [ProtoMember]
            public float CriticalRatio = 0f;

            [ProtoMember]
            public bool DisplayCriticalDivider = false;

            [ProtoMember]
            public SerializableVector3I CriticalColorFrom = new SerializableVector3I(155, 0, 0);

            [ProtoMember]
            public SerializableVector3I CriticalColorTo = new SerializableVector3I(255, 0, 0);
		}

		[ProtoMember]
		public float MinValue = 0;

		[ProtoMember]
		public float MaxValue = 100;

	    [ProtoMember]
        public float DefaultValue = float.NaN;

		[ProtoMember, XmlAttribute(AttributeName = "EnabledInCreative")]
		public bool EnabledInCreative = true;

		[ProtoMember]
		public string Name = string.Empty;

		[ProtoMember]
		public GuiDefinition GuiDef = new GuiDefinition();
	}
}
