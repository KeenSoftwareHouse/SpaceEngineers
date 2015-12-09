using ProtoBuf;
using VRage.ObjectBuilders;

namespace Sandbox.Common.ObjectBuilders.Definitions
{
	[ProtoContract]
	[MyObjectBuilderDefinition]
	public class MyObjectBuilder_GasContainerObject : MyObjectBuilder_PhysicalObject
	{
		[ProtoMember]
		public float GasLevel = 0f;

		public override bool CanStack(MyObjectBuilder_PhysicalObject a)
		{
			return false;
		}
	}
}
