using ProtoBuf;
using VRage.ObjectBuilders;
using VRage.Serialization;

namespace VRage.Game.ObjectBuilders
{
	[ProtoContract]
	[MyObjectBuilderDefinition]
	public class MyObjectBuilder_EntityStat : MyObjectBuilder_Base
	{
		[ProtoMember]
		public float Value = 1.0f;

		[ProtoMember]
		public float MaxValue = 1.0f;

        [ProtoMember]
        public float StatRegenAmountMultiplier = 1.0f;

        [ProtoMember]
        public float StatRegenAmountMultiplierDuration = 0.0f;

		[ProtoMember]
        [Serialize(MyObjectFlags.Nullable)]
		public MyObjectBuilder_EntityStatRegenEffect[] Effects = null;
	}
}
