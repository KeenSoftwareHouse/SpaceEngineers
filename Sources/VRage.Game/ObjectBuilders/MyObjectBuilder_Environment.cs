using ProtoBuf;
using VRage;
using VRage.ObjectBuilders;

namespace Sandbox.Common.ObjectBuilders
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_EnvironmentSettings : MyObjectBuilder_Base
    {
        [ProtoMember]
        public float SunAzimuth;

        [ProtoMember]
        public float SunElevation;

        [ProtoMember]
        public float SunIntensity;

        [ProtoMember]
        public float FogMultiplier;

        [ProtoMember]
        public float FogDensity;

        [ProtoMember]
        public SerializableVector3 FogColor;
    }
}
