using ProtoBuf;
using VRage.Game;
using VRage.ObjectBuilders;

namespace Sandbox.Common.ObjectBuilders.Definitions
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    [System.Xml.Serialization.XmlSerializerAssembly("SpaceEngineers.ObjectBuilders.XmlSerializers")]
    public class MyObjectBuilder_AirtightDoorGenericDefinition : MyObjectBuilder_CubeBlockDefinition
    {
		[ProtoMember]
		public string ResourceSinkGroup;
        [ProtoMember]
        public float PowerConsumptionIdle;
        [ProtoMember]
        public float PowerConsumptionMoving;
        [ProtoMember]
        public float OpeningSpeed;
        [ProtoMember]
        public string Sound;
        [ProtoMember]
        public float SubpartMovementDistance=2.5f;
    }
}
