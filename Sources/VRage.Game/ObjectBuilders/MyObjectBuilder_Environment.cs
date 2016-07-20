using ProtoBuf;
using VRage.ObjectBuilders;

namespace VRage.Game
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

        [ProtoMember]
        public SerializableDefinitionId EnvironmentDefinition = new SerializableDefinitionId(typeof(MyObjectBuilder_EnvironmentDefinition), "Default");
    }
}
