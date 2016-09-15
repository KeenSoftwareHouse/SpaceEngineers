using System.Xml.Serialization;
using ProtoBuf;
using VRage.ObjectBuilders;
using System.Collections.Generic;
using VRageMath;
using VRageRender.Animations;

namespace VRage.Game
{
    [ProtoContract]
    [XmlType("ParticleEffect")]
    [MyObjectBuilderDefinition]
    public sealed class MyObjectBuilder_ParticleEffect : MyObjectBuilder_DefinitionBase
    {
        [ProtoMember]
        public int ParticleId = 0;
        
        [ProtoMember]
        public float Length = 10f;
        
        [ProtoMember]
        public float Preload = 0f;
        
        [ProtoMember]
        public bool LowRes = false;

        [ProtoMember]
        public bool Loop = false;

        [ProtoMember]
        public float DurationMin = 0f;

        [ProtoMember]
        public float DurationMax = 0f;

        [ProtoMember]
        public int Version = 0;

        [ProtoMember]
        public List<ParticleGeneration> ParticleGenerations;

        [ProtoMember]
        public List<ParticleLight> ParticleLights;

        [ProtoMember]
        public List<ParticleSound> ParticleSounds;
    }

    [ProtoContract, XmlType("ParticleGeneration")]
    public class ParticleGeneration
    {
        [ProtoMember, XmlAttribute("Name")]
        public string Name = "";

        [ProtoMember, XmlAttribute("Version")]
        public int Version = 0;

        [ProtoMember]
        public string GenerationType = "CPU";

        [ProtoMember]
        public List<GenerationProperty> Properties;

        [ProtoMember]
        public ParticleEmitter Emitter = null;
    }

    [ProtoContract]
    public class ParticleEmitter
    {
        [ProtoMember, XmlAttribute("Version")]
        public int Version = 0;

        [ProtoMember]
        public List<GenerationProperty> Properties;
    }
    
    [ProtoContract, XmlType("ParticleLight")]
    public class ParticleLight
    {
        [ProtoMember, XmlAttribute("Name")]
        public string Name = "";

        [ProtoMember, XmlAttribute("Version")]
        public int Version = 0;

        [ProtoMember]
        public List<GenerationProperty> Properties;
    }

    [ProtoContract, XmlType("ParticleSound")]
    public class ParticleSound
    {
        [ProtoMember, XmlAttribute("Name")]
        public string Name = "";

        [ProtoMember, XmlAttribute("Version")]
        public int Version = 0;

        [ProtoMember]
        public List<GenerationProperty> Properties;
    }
}
