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

        [ProtoMember, DefaultValue(0.274509817f)]
        public float ColorRed = 0.274509817f;

        [ProtoMember, DefaultValue(0.409019619f)]
        public float ColorGreen = 0.409019619f;

        [ProtoMember, DefaultValue(0.6505882f)]
        public float ColorBlue = 0.6505882f;

        [ProtoMember, DefaultValue(0.75f)]
        public float ColorAlpha = 0.75f;
    }
}
