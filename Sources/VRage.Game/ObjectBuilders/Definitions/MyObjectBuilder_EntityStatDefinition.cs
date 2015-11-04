using ProtoBuf;
using System.Xml.Serialization;
using VRage;
using VRage.ObjectBuilders;

namespace Sandbox.Common.ObjectBuilders.Definitions
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
		}

		[ProtoMember]
		public float MinValue = 0;

		[ProtoMember]
		public float MaxValue = 100;

	    [ProtoMember]
        public float DefaultValue = 100f;

		[ProtoMember, XmlAttribute(AttributeName = "EnabledInCreative")]
		public bool EnabledInCreative = true;

		[ProtoMember]
		public string Name = string.Empty;

		[ProtoMember]
		public GuiDefinition GuiDef = new GuiDefinition();
	}
}
