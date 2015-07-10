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
		public float TickAmount = 0.0f;

		[ProtoMember]
		public float Interval = 1.0f;

		[ProtoMember]
		public float MaxRegenRatio = 1.0f;

		[ProtoMember]
		public float MinRegenRatio = 0.0f;

		[ProtoMember]
		public float AliveTime = 0;

		[ProtoMember]
		public float Duration = -1;
	}
}
