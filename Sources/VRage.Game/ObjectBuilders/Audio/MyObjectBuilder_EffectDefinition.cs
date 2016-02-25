using ProtoBuf;
using System.Collections.Generic;
using System.ComponentModel;
using System.Xml.Serialization;
using VRage.Data.Audio;
using VRage.ObjectBuilders;

namespace VRage.Game
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_AudioEffectDefinition : MyObjectBuilder_DefinitionBase
    {
        [ProtoContract]
        public struct SoundList
        {
            [ProtoMember]
            public List<SoundEffect> SoundEffects;
        }
        [ProtoContract]
        public class SoundEffect
        {
            [ProtoMember]
            public string VolumeCurve;
            [ProtoMember]
            public float Duration;
            [ProtoMember, DefaultValue(MyAudioEffect.FilterType.None)]
            public MyAudioEffect.FilterType Filter = MyAudioEffect.FilterType.None;
            [ProtoMember, DefaultValue(1.0f)]
            public float Frequency = 1;
            [ProtoMember, DefaultValue(false)]
            public bool StopAfter;
            [ProtoMember, DefaultValue(1.0f)]
            public float Q = 1;
        }

        [XmlArrayItem("Sound")]
        [ProtoMember]
        public List<SoundList> Sounds;

        [ProtoMember, DefaultValue(0)] //default is last sound for convenience
        public int OutputSound = 0;
    }
}
