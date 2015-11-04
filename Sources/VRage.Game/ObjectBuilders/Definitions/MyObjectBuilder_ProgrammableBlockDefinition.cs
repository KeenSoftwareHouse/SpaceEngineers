using ProtoBuf;
using VRage.ObjectBuilders;

namespace Sandbox.Common.ObjectBuilders.Definitions
{
	[ProtoContract]
	[MyObjectBuilderDefinition]
	public class MyObjectBuilder_ProgrammableBlockDefinition : MyObjectBuilder_CubeBlockDefinition
	{
		[ProtoMember]
		public string ResourceSinkGroup;
	}
}
