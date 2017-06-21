using System;
using System.Collections.Generic;
using VRage.Utils;

namespace VRage.Data.Audio
{
    public sealed class MySoundData
    {
        public MyStringId Category = MyStringId.GetOrCompute("Undefined");
        public MyCurveType VolumeCurve = MyCurveType.Custom_1;
        public float MaxDistance;
        public float UpdateDistance = 0f;
        public float Volume = 1.0f;
        public float VolumeVariation;
        public float PitchVariation;
        public float Pitch = 0f;
        public int SoundLimit = 0;
        public bool DisablePitchEffects = false;
        public bool AlwaysUseOneMode = false;
        public bool StreamSound = false;
        public bool Loopable;
        public string Alternative2D;
        public bool UseOcclusion;
        public List<MyAudioWave> Waves;
        public List<DistantSound> DistantSounds;
        public int DynamicMusicAmount = 10;
        public MyStringId DynamicMusicCategory = MyStringId.NullOrEmpty;
        public MyMusicTrack MusicTrack;
        public int PreventSynchronization = -1;
        public bool ModifiableByHelmetFilters = true;
        public bool CanBeSilencedByVoid = true;
        public MyStringHash RealisticFilter = MyStringHash.NullOrEmpty;
        public float RealisticVolumeChange = 1f;

        public bool IsHudCue { get { return StringComparer.InvariantCultureIgnoreCase.Equals(Category.ToString(), "hud"); } }

        public MyStringHash SubtypeId;
    }
}
