using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.ObjectBuilders;

namespace VRage.Game.ObjectBuilders
{
	[ProtoContract]
	[MyObjectBuilderDefinition]
	public class MyObjectBuilder_EntityStatRegenEffect : MyObjectBuilder_Base
	{
		[ProtoMember]
		public float TickAmount;

		[ProtoMember]
		public float Interval;

		[ProtoMember]
		public float MaxRegenRatio;

		[ProtoMember]
		public float MinRegenRatio;

		[ProtoMember]
		public float AliveTime = 0;

		[ProtoMember]
		public float Duration = -1;
	}
}
