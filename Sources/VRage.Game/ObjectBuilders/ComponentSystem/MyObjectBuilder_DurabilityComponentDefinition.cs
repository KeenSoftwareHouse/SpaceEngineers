using ProtoBuf;
using System.ComponentModel;
using System.Xml.Serialization;
using VRage.ObjectBuilders;

namespace VRage.Game.ObjectBuilders.ComponentSystem
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_DurabilityComponentDefinition : MyObjectBuilder_ComponentDefinitionBase
    {
        [ProtoContract]
        public class HitDefinition
        {
            [ProtoMember, DefaultValue(null), XmlAttribute]
            public string Action = null;
            [ProtoMember, DefaultValue(null), XmlAttribute]
            public string Material = null;            
            [ProtoMember, XmlAttribute]
            public float Damage = 0.01f;
        }

        [ProtoMember]
        public float DefaultHitDamage = 0.01f;

        [ProtoMember, XmlArrayItem("Hit")]
        public HitDefinition[] DefinedHits = null;
        
        [ProtoMember]
        public string ParticleEffect = null;
        
        [ProtoMember]
        public string SoundCue = null;

        [ProtoMember]
        public float DamageOverTime = 0.0f;
    }
}
