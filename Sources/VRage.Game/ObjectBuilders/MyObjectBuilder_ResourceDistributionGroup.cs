using ProtoBuf;
using VRage.ObjectBuilders;

namespace VRage.Game.ObjectBuilders.Definitions
{
	[ProtoContract]
	[MyObjectBuilderDefinition]
	public class MyObjectBuilder_ResourceDistributionGroup : MyObjectBuilder_DefinitionBase
	{
		[ProtoMember]
		public int Priority = 0;

		[ProtoMember]
		public bool IsSource;

		[ProtoMember]
		public bool IsAdaptible;
	}
}
