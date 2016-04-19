using ProtoBuf;
using System.Collections.Generic;
using System.Xml.Serialization;
using VRage.ObjectBuilders;
using VRageMath;

namespace VRage.Game
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_MaterialPropertiesDefinition : MyObjectBuilder_DefinitionBase
    {
        [ProtoContract]
        public struct ContactProperty
        {
            [ProtoMember]
            public string Type;
            [ProtoMember]
            public string Material;
            [ProtoMember]
            public string SoundCue;
            [ProtoMember]
            public string ParticleEffect;
            [ProtoMember]
            public List<AlternativeImpactSounds> AlternativeImpactSounds;
        }

        [ProtoContract]
        public struct GeneralProperty
        {
            [ProtoMember]
            public string Type;
            [ProtoMember]
            public string SoundCue;
        }

        [ProtoMember]
        public List<ContactProperty> ContactProperties;

        [XmlArrayItem("Property")]
        [ProtoMember]
        public List<GeneralProperty> GeneralProperties;

        [ProtoMember]
        public string InheritFrom;
    }

    [ProtoContract, XmlType("AlternativeImpactSound")]
    public sealed class AlternativeImpactSounds
    {
        [ProtoMember, XmlAttribute]
        public float mass = 0f;

        [ProtoMember, XmlAttribute]
        public string soundCue = "";

        [ProtoMember, XmlAttribute]
        public float minVelocity = 0f;

        [ProtoMember, XmlAttribute]
        public float maxVolumeVelocity = 0f;
    };
}