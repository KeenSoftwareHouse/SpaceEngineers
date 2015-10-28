using VRage.ObjectBuilders;
using ProtoBuf;

namespace Sandbox.Common.ObjectBuilders.Definitions
{
	[ProtoContract]
	[MyObjectBuilderDefinition]
	public class MyObjectBuilder_PoweredCargoContainerDefinition : MyObjectBuilder_CargoContainerDefinition
	{
		[ProtoMember]
		public string ResourceSinkGroup;
	}
}
