using ProtoBuf;
using System.ComponentModel;
using VRage.ObjectBuilders;

namespace Sandbox.Common.ObjectBuilders
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_MotorSuspension : MyObjectBuilder_MotorBase
    {
        [ProtoMember]
        public float SteerAngle;

        [ProtoMember, DefaultValue(true)]
        public bool Steering = true;

        [ProtoMember]
        public float Damping = 0.02f;

        [ProtoMember]
        public float Strength = 0.04f;

        [ProtoMember]
        public bool Propulsion = true;

        [ProtoMember]
        public float Friction = 1.5f / 8;

        [ProtoMember]
        public float Power = 1;

        [ProtoMember]
        public float Height = 0;

        [ProtoMember]
        public float MaxSteerAngle = 0.45f;

        [ProtoMember]
        public float SteerSpeed = 0.02f;

        [ProtoMember]
        public float SteerReturnSpeed = 0.01f;

        [ProtoMember]
        public bool InvertSteer = false;

        [ProtoMember]
        public float SuspensionTravel = 100;
    }
}