using ProtoBuf;
using System.Collections.Generic;
using System.Xml.Serialization;
using VRage.ObjectBuilders;
using VRageMath;

namespace Sandbox.Common.ObjectBuilders.Definitions
{
    [ProtoContract]
    public class ContactPropertyParticleProperties
    {
        public Vector4 ColorMultiplier = new Vector4(1, 1, 1, 1);
        public float SizeMultiplier = 1f;
        public float Preload = 0f;
    }

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
            public ContactPropertyParticleProperties ParticleProperties;
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
}
