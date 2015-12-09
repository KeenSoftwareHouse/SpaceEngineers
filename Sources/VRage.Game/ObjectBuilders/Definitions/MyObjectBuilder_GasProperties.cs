using ProtoBuf;
using Sandbox.Common.ObjectBuilders.Definitions;
using VRage.ObjectBuilders;

namespace VRage.Game.ObjectBuilders.Definitions
{
	[ProtoContract]
	[MyObjectBuilderDefinition]
	public class MyObjectBuilder_GasProperties : MyObjectBuilder_DefinitionBase
	{
		[ProtoMember]
		public float EnergyDensity = 0f;
	}
}
