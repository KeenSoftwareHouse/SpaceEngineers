using ProtoBuf;
using VRage.Game.ObjectBuilders;
using VRage.ObjectBuilders;

namespace Sandbox.Common.ObjectBuilders.ComponentSystem
{
	[ProtoContract]
	[MyObjectBuilderDefinition]
	public class MyObjectBuilder_EntityStatComponent : MyObjectBuilder_ComponentBase
	{
		[ProtoMember]
		public MyObjectBuilder_EntityStat[] Stats = null;

		[ProtoMember]
		public string[] ScriptNames = null;
	}
}
