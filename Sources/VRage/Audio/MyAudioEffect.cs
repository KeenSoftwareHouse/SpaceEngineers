using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.Utils;
using VRageMath;

namespace VRage.Data.Audio
{
    public class MyAudioEffect
    {
        public enum FilterType
        {
            LowPass,
            BandPass,
            HighPass,
            Notch,
            None
        }
        public struct SoundEffect
        {
            public Curve VolumeCurve;
            public float Duration;
            public FilterType Filter;
            public float Frequency;
            public bool StopAfter;
            public float OneOverQ;
        }

        public int ResultEmitterIdx;
        public List<List<SoundEffect>> SoundsEffects = new List<List<SoundEffect>>();
        public MyStringHash EffectId;
    }
}
