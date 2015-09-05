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
        public float Volume = 1.0f;
        public float VolumeVariation;
        public float PitchVariation;
        public bool Loopable;
        public string Alternative2D;
        public bool UseOcclusion;
        public List<MyAudioWave> Waves;
        public MyMusicTrack MusicTrack;

        public bool IsHudCue { get { return StringComparer.InvariantCultureIgnoreCase.Equals(Category.ToString(), "hud"); } }

        public MyStringHash SubtypeId;
    }
}
