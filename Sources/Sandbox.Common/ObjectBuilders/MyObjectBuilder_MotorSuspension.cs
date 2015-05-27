using ProtoBuf;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

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
    }
}