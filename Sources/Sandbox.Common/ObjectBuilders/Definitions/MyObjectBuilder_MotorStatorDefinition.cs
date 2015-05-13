using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ProtoBuf;

namespace Sandbox.Common.ObjectBuilders.Definitions
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_MotorStatorDefinition : MyObjectBuilder_CubeBlockDefinition
    {
        [ProtoMember(1)]
        public float RequiredPowerInput;

        [ProtoMember(2)]
        public float MaxForceMagnitude;

        [ProtoMember(3)]
        public string RotorPart;

        [ProtoMember(4)]
        public float RotorDisplacementMin;

        [ProtoMember(5)]
        public float RotorDisplacementMax;

        [ProtoMember(6)]
        public float RotorDisplacementInModel;
    }
}
