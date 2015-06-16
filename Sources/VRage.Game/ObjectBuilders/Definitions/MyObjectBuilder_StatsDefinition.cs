using ProtoBuf;
using System.Collections.Generic;
using System.Xml.Serialization;
using VRage.ObjectBuilders;

namespace Sandbox.Common.ObjectBuilders.Definitions
{
	[ProtoContract]
	[MyObjectBuilderDefinition]
	public class MyObjectBuilder_StatsDefinition : MyObjectBuilder_DefinitionBase
	{
		[XmlArrayItem("Stat")]
		[ProtoMember]
		public List<SerializableDefinitionId> Stats;

		[XmlArrayItem("Script")]
		[ProtoMember]
		public List<string> Scripts;
	}
}
