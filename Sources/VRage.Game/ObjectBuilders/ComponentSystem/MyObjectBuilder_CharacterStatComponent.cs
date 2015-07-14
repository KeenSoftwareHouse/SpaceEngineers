using ProtoBuf;
using VRage.Game.ObjectBuilders;
using VRage.ObjectBuilders;

namespace Sandbox.Common.ObjectBuilders.ComponentSystem
{
	[ProtoContract]
	[MyObjectBuilderDefinition]
	public class MyObjectBuilder_CharacterStatComponent : MyObjectBuilder_EntityStatComponent
	{
	}
}
