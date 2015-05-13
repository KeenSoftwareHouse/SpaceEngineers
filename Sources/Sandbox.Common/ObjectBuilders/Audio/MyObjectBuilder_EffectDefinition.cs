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
            [ProtoMember(1)]
            public List<SoundEffect> SoundEffects;
        }
        [ProtoContract]
        public class SoundEffect
        {
            [ProtoMember(1)]
            public string VolumeCurve;
            [ProtoMember(2)]
            public float Duration;
            [ProtoMember(3), DefaultValue(MyAudioEffect.FilterType.None)]
            public MyAudioEffect.FilterType Filter = MyAudioEffect.FilterType.None;
            [ProtoMember(4), DefaultValue(1.0f)]
            public float Frequency = 1;
            [ProtoMember(5), DefaultValue(false)]
            public bool StopAfter;
            [ProtoMember(6), DefaultValue(1.0f)]
            public float Q = 1;
        }

        [XmlArrayItem("Sound")]
        [ProtoMember(1)]
        public List<SoundList> Sounds;

        [ProtoMember(2), DefaultValue(0)] //default is last sound for convenience
        public int OutputSound = 0;
    }
}
