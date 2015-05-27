using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sandbox.Common.ObjectBuilders.Definitions
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_MotorSuspensionDefinition : MyObjectBuilder_MotorStatorDefinition
    {
        [ProtoMember]
        public float MaxSteer = 0.45f;

        [ProtoMember]
        public float SteeringSpeed = 0.02f;

        [ProtoMember]
        public float PropulsionForce = 10000;

        [ProtoMember]
        public float SuspensionLimit = 0.1f;

        [ProtoMember]
        public float MinHeight = -0.32f;

        [ProtoMember]
        public float MaxHeight = 0.26f;
    }
}
