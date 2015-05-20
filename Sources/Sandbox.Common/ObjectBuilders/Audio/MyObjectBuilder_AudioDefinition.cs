using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using ProtoBuf;
using Sandbox.Common.ObjectBuilders.Definitions;
using System.ComponentModel;
using VRage.Data;
using VRage.Utils;
using VRage.Library.Utils;
using VRage.Data.Audio;
using VRage;

namespace Sandbox.Common.ObjectBuilders.Audio
{
    [ProtoContract]
    [XmlType("Sound")]
    [MyObjectBuilderDefinition]
    public sealed class MyObjectBuilder_AudioDefinition : MyObjectBuilder_DefinitionBase
    {
        //replaced by Id.SubTypeId
        //[ProtoMember]
        //public string Name;

        [XmlIgnore]
        public MySoundData SoundData = new MySoundData();

        [ProtoMember]
        public string Category
        {
            get { return SoundData.Category.ToString(); }
            set { SoundData.Category = MyStringId.GetOrCompute(value); }
        }

        [ProtoMember, DefaultValue(MyCurveType.Custom_1)]
        public MyCurveType VolumeCurve
        {
            get { return SoundData.VolumeCurve; }
            set { SoundData.VolumeCurve = value; }
        }

        [ProtoMember]
        public float MaxDistance
        {
            get { return SoundData.MaxDistance; }
            set { SoundData.MaxDistance = value; }
        }

        [ProtoMember, DefaultValue(1.0f)]
        public float Volume
        {
            get { return SoundData.Volume; }
            set { SoundData.Volume = value; }
        }

        [ProtoMember, DefaultValue(0.0f)]
        public float VolumeVariation
        {
            get { return SoundData.VolumeVariation; }
            set { SoundData.VolumeVariation = value; }
        }

        [ProtoMember, DefaultValue(0.0f)]
        public float PitchVariation
        {
            get { return SoundData.PitchVariation; }
            set { SoundData.PitchVariation = value; }
        }

        [ProtoMember, DefaultValue(false)]
        public bool Loopable
        {
            get { return SoundData.Loopable; }
            set { SoundData.Loopable = value; }
        }

        [ProtoMember]
        public string Alternative2D
        {
            get { return SoundData.Alternative2D; }
            set { SoundData.Alternative2D = value; }
        }

        [ProtoMember, DefaultValue(false)]
        public bool UseOcclusion
        {
            get { return SoundData.UseOcclusion; }
            set { SoundData.UseOcclusion = value; }
        }

        [ProtoMember]
        public List<MyAudioWave> Waves
        {
            get { return SoundData.Waves; }
            set { SoundData.Waves = value; }
        }

        [ProtoMember, DefaultValue("")]
        public string TransitionCategory
        {
            get { return SoundData.MusicTrack.TransitionCategory.ToString(); }
            set { SoundData.MusicTrack.TransitionCategory = MyStringId.GetOrCompute(value); }
        }

        [ProtoMember, DefaultValue("")]
        public string MusicCategory
        {
            get { return SoundData.MusicTrack.MusicCategory.ToString(); }
            set { SoundData.MusicTrack.MusicCategory = MyStringId.GetOrCompute(value); }
        }
    }
}
