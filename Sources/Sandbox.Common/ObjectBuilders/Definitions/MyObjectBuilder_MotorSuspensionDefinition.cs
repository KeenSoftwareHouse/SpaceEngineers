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
        [ProtoMember(1)]
        public float MaxSteer = 0.45f;

        [ProtoMember(2)]
        public float SteeringSpeed = 0.02f;

        [ProtoMember(3)]
        public float PropulsionForce = 10000;

        [ProtoMember(4)]
        public float SuspensionLimit = 0.1f;
    }
}
