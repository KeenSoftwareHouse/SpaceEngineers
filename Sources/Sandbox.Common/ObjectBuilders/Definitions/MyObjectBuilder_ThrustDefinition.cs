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
        
        [ProtoMember]
        public float ForceMagnitude;

        [ProtoMember]
        public float MaxPowerConsumption;

        [ProtoMember]
        public float MinPowerConsumption;

        [ProtoMember]
        public float FlameDamageLengthScale = 0.6f;

        [ProtoMember]
        public float FlameLengthScale = 1.15f;

        [ProtoMember]
        public Vector4 FlameFullColor = DefaultThrustColor;

        [ProtoMember]
        public Vector4 FlameIdleColor = DefaultThrustColor;

        [ProtoMember]
        public string FlamePointMaterial = "EngineThrustMiddle";

        [ProtoMember]
        public string FlameLengthMaterial = "EngineThrustMiddle";

        [ProtoMember]
        public string FlameGlareMaterial = "GlareSsThrustSmall";

        [ProtoMember]
        public float FlameVisibilityDistance = 200;

        [ProtoMember]
        public float FlameGlareSize = 0.391f;

        [ProtoMember]
        public float FlameGlareQuerySize = 1;

        [ProtoMember]
        public float FlameDamage = 0.5f;
    }
}
