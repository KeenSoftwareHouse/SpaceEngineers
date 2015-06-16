using ProtoBuf;
using VRage.ObjectBuilders;

namespace Sandbox.Common.ObjectBuilders.Definitions
{
	[ProtoContract]
	[MyObjectBuilderDefinition]
	public class MyObjectBuilder_EntityStatDefinition : MyObjectBuilder_DefinitionBase
	{
		[ProtoMember]
		public float MinValue = 0;

		[ProtoMember]
		public float MaxValue = 100;

		[ProtoMember]
		public bool EnabledInCreative = true;
	}
}
