using System.Collections.Generic;
using System.Xml.Serialization;
using ProtoBuf;
using System.ComponentModel;
using VRage.Utils;
using VRage.Data.Audio;
using VRage.ObjectBuilders;

namespace VRage.Game
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

        [ProtoMember]
        public float UpdateDistance
        {
            get { return SoundData.UpdateDistance; }
            set { SoundData.UpdateDistance = value; }
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

        [ProtoMember, DefaultValue(0.0f)]
        public float Pitch
        {
            get { return SoundData.Pitch; }
            set { SoundData.Pitch = value; }
        }

        [ProtoMember, DefaultValue(-1)]
        public int PreventSynchronization
        {
            get { return SoundData.PreventSynchronization; }
            set { SoundData.PreventSynchronization = value; }
        }

        [ProtoMember]
        public string DynamicMusicCategory
        {
            get { return SoundData.DynamicMusicCategory.ToString(); }
            set { SoundData.DynamicMusicCategory = MyStringId.GetOrCompute(value); }
        }

        [ProtoMember]
        public int DynamicMusicAmount
        {
            get { return SoundData.DynamicMusicAmount; }
            set { SoundData.DynamicMusicAmount = value; }
        }

        [ProtoMember, DefaultValue(true)]
        public bool ModifiableByHelmetFilters
        {
            get { return SoundData.ModifiableByHelmetFilters; }
            set { SoundData.ModifiableByHelmetFilters = value; }
        }

        [ProtoMember, DefaultValue(false)]
        public bool AlwaysUseOneMode
        {
            get { return SoundData.AlwaysUseOneMode; }
            set { SoundData.AlwaysUseOneMode = value; }
        }

        [ProtoMember, DefaultValue(true)]
        public bool CanBeSilencedByVoid
        {
            get { return SoundData.CanBeSilencedByVoid; }
            set { SoundData.CanBeSilencedByVoid = value; }
        }

        [ProtoMember, DefaultValue(false)]
        public bool StreamSound
        {
            get { return SoundData.StreamSound; }
            set { SoundData.StreamSound = value; }
        }

        [ProtoMember, DefaultValue(false)]
        public bool DisablePitchEffects
        {
            get { return SoundData.DisablePitchEffects; }
            set { SoundData.DisablePitchEffects = value; }
        }

        [ProtoMember, DefaultValue(0)]
        public int SoundLimit
        {
            get { return SoundData.SoundLimit; }
            set { SoundData.SoundLimit = value; }
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

        [ProtoMember]
        public List<DistantSound> DistantSounds
        {
            get { return SoundData.DistantSounds; }
            set { SoundData.DistantSounds = value; }
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

        [ProtoMember, DefaultValue("")]
        public string RealisticFilter
        {
            get { return SoundData.RealisticFilter.String; }
            set { SoundData.RealisticFilter = MyStringHash.GetOrCompute(value); }
        }

        [ProtoMember, DefaultValue(1f)]
        public float RealisticVolumeChange
        {
            get { return SoundData.RealisticVolumeChange; }
            set { SoundData.RealisticVolumeChange = value; }
        }
    }
}
