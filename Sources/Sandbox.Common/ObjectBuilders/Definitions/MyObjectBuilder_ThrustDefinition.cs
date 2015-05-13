using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ProtoBuf;
using VRageMath;

namespace Sandbox.Common.ObjectBuilders.Definitions
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_ThrustDefinition : MyObjectBuilder_CubeBlockDefinition
    {
        static readonly Vector4 DefaultThrustColor = new Vector4(Color.CornflowerBlue.ToVector3() * 0.7f, 0.75f);
        
        [ProtoMember(1)]
        public float ForceMagnitude;

        [ProtoMember(2)]
        public float MaxPowerConsumption;

        [ProtoMember(3)]
        public float MinPowerConsumption;

        [ProtoMember(4)]
        public float FlameDamageLengthScale = 0.6f;

        [ProtoMember(5)]
        public float FlameLengthScale = 1.15f;

        [ProtoMember(6)]
        public Vector4 FlameFullColor = DefaultThrustColor;

        [ProtoMember(7)]
        public Vector4 FlameIdleColor = DefaultThrustColor;

        [ProtoMember(8)]
        public string FlamePointMaterial = "EngineThrustMiddle";

        [ProtoMember(9)]
        public string FlameLengthMaterial = "EngineThrustMiddle";

        [ProtoMember(10)]
        public string FlameGlareMaterial = "GlareSsThrustSmall";

        [ProtoMember(11)]
        public float FlameVisibilityDistance = 200;

        [ProtoMember(12)]
        public float FlameGlareSize = 0.391f;

        [ProtoMember(13)]
        public float FlameGlareQuerySize = 1;

        [ProtoMember(14)]
        public float FlameDamage = 0.5f;
    }
}
