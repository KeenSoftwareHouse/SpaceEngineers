using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRageMath;

namespace Sandbox.Common.ObjectBuilders.Definitions
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_LaserAntennaDefinition : MyObjectBuilder_CubeBlockDefinition
    {
        [ProtoMember]
        public float PowerInputIdle=0.001f;
        [ProtoMember]
        public float PowerInputTurning=0.01f;
        [ProtoMember]
        public float PowerInputLasing=2f;
        [ProtoMember]
        public float RotationRate = MathHelper.Pi / 20000.0f;
        [ProtoMember]
        public float MaxRange = 40000;
        [ProtoMember(6)]
        public bool RequireLineOfSight = true;
    }
}
