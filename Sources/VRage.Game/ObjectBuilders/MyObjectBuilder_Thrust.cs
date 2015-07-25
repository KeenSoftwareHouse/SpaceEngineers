using System.ComponentModel;
using ProtoBuf;
using VRage.ObjectBuilders;

namespace Sandbox.Common.ObjectBuilders
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_Thrust : MyObjectBuilder_FunctionalBlock
    {
        [ProtoMember, DefaultValue(0.0f)]
        public float ThrustOverride = 0.0f;

        /* The Default color is Color.CornflowerBlue*/ 

        [ProtoMember, DefaultValue(0.274509817f)]
        public float FlameColorRed = 0.274509817f;

        [ProtoMember, DefaultValue(0.409019619f)]
        public float FlameColorGreen = 0.409019619f;

        [ProtoMember, DefaultValue(0.6505882f)]
        public float FlameColorBlue = 0.6505882f;

        [ProtoMember, DefaultValue(0.75f)]
        public float FlameColorAlpha = 0.75f;
    }
}
