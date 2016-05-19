using VRage.ObjectBuilders;
using ProtoBuf;

namespace VRage.Game
{
	[ProtoContract]
	[MyObjectBuilderDefinition]
	public class MyObjectBuilder_PoweredCargoContainerDefinition : MyObjectBuilder_CargoContainerDefinition
	{
		[ProtoMember]
		public string ResourceSinkGroup;
    
        [ProtoMember]
        public float RequiredPowerInput;
    }
}
