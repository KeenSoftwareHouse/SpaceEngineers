using ProtoBuf;
using VRage.ObjectBuilders;

namespace Sandbox.Common.ObjectBuilders.Definitions
{
	[ProtoContract]
	[MyObjectBuilderDefinition]
	public class MyObjectBuilder_RadioAntennaDefinition : MyObjectBuilder_CubeBlockDefinition
	{
		[ProtoMember]
		public string ResourceSinkGroup;
	}
}
