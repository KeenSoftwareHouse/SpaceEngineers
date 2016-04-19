using System.ComponentModel;
using ProtoBuf;
using VRage.Game;
using VRage.ObjectBuilders;

namespace Sandbox.Common.ObjectBuilders
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    [System.Xml.Serialization.XmlSerializerAssembly("SpaceEngineers.ObjectBuilders.XmlSerializers")]
    public abstract class MyObjectBuilder_LightingBlock : MyObjectBuilder_FunctionalBlock
    {
        //[ProtoMember, DefaultValue(true)]
        //public bool Enabled = true;

        [ProtoMember, DefaultValue(-1f)]
        public float Radius = -1f;

        [ProtoMember, DefaultValue(-1f)]
        public float ReflectorRadius = -1f;

        [ProtoMember, DefaultValue(1f)]
        public float ColorRed = 1f;

        [ProtoMember, DefaultValue(1f)]
        public float ColorGreen = 1f;

        [ProtoMember, DefaultValue(1f)]
        public float ColorBlue = 1f;

        [ProtoMember, DefaultValue(1f)]
        public float ColorAlpha = 1f;

        [ProtoMember, DefaultValue(-1f)]
        public float Falloff = -1f;

        [ProtoMember, DefaultValue(-1f)]
        public float Intensity = -1f;

        [ProtoMember, DefaultValue(-1f)]
        public float BlinkIntervalSeconds = -1f;

        [ProtoMember, DefaultValue(-1f)]
        public float BlinkLenght = -1f;

        [ProtoMember, DefaultValue(-1f)]
        public float BlinkOffset = -1f;
    }
}
