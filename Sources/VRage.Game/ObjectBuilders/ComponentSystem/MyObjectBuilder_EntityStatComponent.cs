using ProtoBuf;
using VRage.Game.ObjectBuilders;
using VRage.ObjectBuilders;
using VRage.Serialization;

namespace Sandbox.Common.ObjectBuilders.ComponentSystem
{
	[ProtoContract]
	[MyObjectBuilderDefinition]
	public class MyObjectBuilder_EntityStatComponent : MyObjectBuilder_ComponentBase
	{
		[ProtoMember]
        [Serialize(MyObjectFlags.Nullable)]
		public MyObjectBuilder_EntityStat[] Stats = null;

		[ProtoMember]
        [Serialize(MyObjectFlags.Nullable)]
		public string[] ScriptNames = null;
	}
}
