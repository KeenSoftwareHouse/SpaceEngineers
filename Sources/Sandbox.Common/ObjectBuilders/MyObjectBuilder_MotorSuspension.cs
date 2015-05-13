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
        [ProtoMember(1)]
        public float SteerAngle;

        [ProtoMember(2), DefaultValue(true)]
        public bool Steering = true;

        [ProtoMember(3)]
        public float Damping = 0.02f;

        [ProtoMember(4)]
        public float Strength = 0.04f;

        [ProtoMember(5)]
        public bool Propulsion = true;

        [ProtoMember(6)]
        public float Friction = 1.5f / 8;

        [ProtoMember(7)]
        public float Power = 1;
    }
}