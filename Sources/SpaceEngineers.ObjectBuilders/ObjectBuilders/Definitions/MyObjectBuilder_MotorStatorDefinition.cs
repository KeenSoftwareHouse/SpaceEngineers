using ObjectBuilders.Definitions;
using VRage.ObjectBuilders;
using ProtoBuf;
using VRage.Game;

namespace Sandbox.Common.ObjectBuilders.Definitions
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    [System.Xml.Serialization.XmlSerializerAssembly("SpaceEngineers.ObjectBuilders.XmlSerializers")]
    public class MyObjectBuilder_MotorStatorDefinition : MyObjectBuilder_MechanicalConnectionBlockBaseDefinition
    {
	    [ProtoMember]
	    public string ResourceSinkGroup;

        [ProtoMember]
        public float RequiredPowerInput;

        [ProtoMember]
        public float MaxForceMagnitude;

        [ProtoMember]
        public float RotorDisplacementMin;

        [ProtoMember]
        public float RotorDisplacementMax;

        [ProtoMember]
        public float RotorDisplacementInModel;
    }
}
