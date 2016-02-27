using ProtoBuf;
using System.ComponentModel;
using VRage.ObjectBuilders;

namespace Sandbox.Common.ObjectBuilders
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    [System.Xml.Serialization.XmlSerializerAssembly("SpaceEngineers.ObjectBuilders.XmlSerializers")]
    public class MyObjectBuilder_MotorSuspension : MyObjectBuilder_MotorBase
    {
        [ProtoMember]
        public float SteerAngle;

        [ProtoMember, DefaultValue(true)]
        public bool Steering = true;

        [ProtoMember]
        public float Damping = 0.012f;

        [ProtoMember]
        public float Strength = 0.002f;

        [ProtoMember]
        public bool Propulsion = true;

        [ProtoMember]
        public float Friction = 0.4f * 4;

        [ProtoMember]
        public float Power = 0.1f;

        [ProtoMember]
        public float Height = -0.458f;

        [ProtoMember]
        public float MaxSteerAngle = 0.32f;

        [ProtoMember]
        public float SteerSpeed = 0.01f;

        [ProtoMember]
        public float SteerReturnSpeed = 0.01f;

        [ProtoMember]
        public bool InvertSteer = false;

        [ProtoMember]
        public bool InvertPropulsion = false;

        [ProtoMember]
        public float SuspensionTravel = 100;

        [ProtoMember]
        public float SpeedLimit = 360;
    }
}