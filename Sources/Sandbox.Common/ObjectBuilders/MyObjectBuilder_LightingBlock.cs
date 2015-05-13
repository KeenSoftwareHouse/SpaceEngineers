using System.ComponentModel;
using ProtoBuf;

namespace Sandbox.Common.ObjectBuilders
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public abstract class MyObjectBuilder_LightingBlock : MyObjectBuilder_FunctionalBlock
    {
        //[ProtoMember(1), DefaultValue(true)]
        //public bool Enabled = true;

        [ProtoMember(2), DefaultValue(-1f)]
        public float Radius = -1f;

        [ProtoMember(3), DefaultValue(1f)]
        public float ColorRed = 1f;

        [ProtoMember(4), DefaultValue(1f)]
        public float ColorGreen = 1f;

        [ProtoMember(5), DefaultValue(1f)]
        public float ColorBlue = 1f;

        [ProtoMember(6), DefaultValue(1f)]
        public float ColorAlpha = 1f;

        [ProtoMember(7), DefaultValue(-1f)]
        public float Falloff = -1f;

        [ProtoMember(8), DefaultValue(-1f)]
        public float Intensity = -1f;

        [ProtoMember(9), DefaultValue(-1f)]
        public float BlinkIntervalSeconds = -1f;

        [ProtoMember(10), DefaultValue(-1f)]
        public float BlinkLenght = -1f;

        [ProtoMember(11), DefaultValue(-1f)]
        public float BlinkOffset = -1f;
    }
}
