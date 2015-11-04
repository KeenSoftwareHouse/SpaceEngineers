using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
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
        [Serialize(MyObjectFlags.Nullable)]
		public MyObjectBuilder_EntityStatRegenEffect[] Effects = null;
	}
}
