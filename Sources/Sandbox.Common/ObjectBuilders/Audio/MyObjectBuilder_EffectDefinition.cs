using ProtoBuf;
using Sandbox.Common.ObjectBuilders.Definitions;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using VRage.Data.Audio;

namespace Sandbox.Common.ObjectBuilders.Audio
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
