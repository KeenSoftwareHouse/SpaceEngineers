using SharpDX.XAudio2;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using VRage.Data.Audio;

namespace VRage.Audio
{
    class MyEffectInstance : IMyAudioEffect
    {
        bool m_autoUpdate = true;
        public bool AutoUpdate { get { return m_autoUpdate; } set { m_autoUpdate = value; } }
        public event Action<MyEffectInstance> OnEffectEnded;
        private bool m_ended = false;
        public bool Finished { get { return m_ended; } }

        public IMySourceVoice OutputSound { get { return m_effect.ResultEmitterIdx < m_sounds.Count ? m_sounds[m_effect.ResultEmitterIdx].Sound : null; } }
        class SoundData
        {
            public MySourceVoice Sound;
            public float Pivot;
            public int CurrentEffect;
            public float OrigVolume;
            public float OrigFrequency;
            public FilterParameters? CurrentFilter;
        }

        private MyAudioEffect m_effect;
        private List<SoundData> m_sounds = new List<SoundData>();
        private float m_elapsed = 0;
        private float m_scale = 1;
        private float m_duration;
        private XAudio2 m_engine;

        private static  FilterParameters m_defaultFilter = new FilterParameters(){
                Type = FilterType.LowPassFilter,
                 Frequency = 1f,
                 OneOverQ = 1,};
        public MyEffectInstance(MyAudioEffect effect, IMySourceVoice input, MySourceVoice[] cues, float? duration, XAudio2 engine)
        {
            m_engine = engine;
            m_effect = effect;
            var inputSound = input as MySourceVoice;
            if (inputSound != null && inputSound.IsValid && inputSound.Voice != null && inputSound.Voice.IsValid())
            {
                Debug.Assert(!inputSound.Voice.IsDisposed);
                var sd = new SoundData()
                {
                    Sound = inputSound,
                    Pivot = 0,
                    CurrentEffect = 0,
                    OrigVolume = inputSound.Volume,
                    OrigFrequency = inputSound.FrequencyRatio,
                };
                //FilterParameters fp = new FilterParameters();
                m_sounds.Add(sd);
            }

            foreach(var sound in cues)
            {
                Debug.Assert(!sound.Voice.IsDisposed);
                sound.Start(false); //jn:todo effect command to start sound
                m_sounds.Add(new SoundData()
                {
                    Sound = sound,
                    Pivot = 0,
                    CurrentEffect = 0,
                    OrigVolume = sound.Volume,
                    OrigFrequency = sound.FrequencyRatio,
                });
            }
            if(OutputSound != null)
                OutputSound.StoppedPlaying += EffectFinished;

            ComputeDurationAndScale(duration);
            Update(0);
        }

        private void ComputeDurationAndScale(float? duration)
        {
            float maxEffDuration = 0;
            foreach(var effects in m_effect.SoundsEffects)
            {
                float effDur = 0;
                foreach(var effect in effects)
                {
                    effDur += effect.Duration;
                }
                if (effDur > maxEffDuration)
                    maxEffDuration = effDur;
            }

            if (maxEffDuration > 0 && duration.HasValue)
                m_scale = duration.Value / maxEffDuration;

            m_duration = maxEffDuration * m_scale;
        }

        public void Update(int stepMs)
        {
            if (m_ended)
            {
                Debug.Fail("Updating finished effect.");
                return;
            }

            m_elapsed += stepMs;
            bool allFinished = true;
            for(int i = 0; i < m_sounds.Count; i++)
            {
                var sData = m_sounds[i];

                if (sData.Sound.Volume > 0 && sData.Sound.Volume < 1)
                {

                }
                if (!sData.Sound.IsPlaying || sData.CurrentEffect >= m_effect.SoundsEffects[i].Count)
                {
                    Debug.Assert(!sData.Sound.IsPlaying || !sData.Sound.IsLoopable || i == m_effect.ResultEmitterIdx, "Loopable sound not ended by effect!");
                    continue;
                }
                var effect = m_effect.SoundsEffects[i][sData.CurrentEffect];
                float effPosition;
                if (effect.Duration > 0)
                    effPosition = (m_elapsed - sData.Pivot) / (effect.Duration * m_scale);
                else
                {
                    effPosition = 0;
                    Debug.Assert(!sData.Sound.IsLoopable || i == m_effect.ResultEmitterIdx, "Infinite effect on loop without outside reference!");
                }

                if(effPosition > 1) 
                {
                    if(effect.StopAfter)
                    {
                        sData.Sound.Stop();
                        continue;
                    }
                    //update sound again with next effect
                    sData.CurrentEffect++;
                    sData.Pivot += (effect.Duration * m_scale);
                    i--;
                    if (effect.Filter != MyAudioEffect.FilterType.None) //return original filter
                        sData.Sound.Voice.SetFilterParameters(m_defaultFilter);
                    sData.CurrentFilter = null;
                    continue;
                }
                UpdateVolume(sData, effect, effPosition);
                UpdateFilter(sData, effect);

                allFinished = false;
            }

            if (allFinished)
                EffectFinished();
        }

        private static void UpdateVolume(SoundData sData, MyAudioEffect.SoundEffect effect, float effPosition)
        {
            if (effect.VolumeCurve != null)
                sData.Sound.SetVolume(sData.OrigVolume * effect.VolumeCurve.Evaluate(effPosition));
        }

        private static void UpdateFilter(SoundData sData, MyAudioEffect.SoundEffect effect)
        {
            if (effect.Filter != MyAudioEffect.FilterType.None)
            {
                if (!sData.CurrentFilter.HasValue)
                {
                    sData.CurrentFilter = new FilterParameters()
                    {
                        Frequency = effect.Frequency,
                        OneOverQ = effect.OneOverQ,
                        Type = (FilterType)effect.Filter
                    };
                }
                sData.Sound.Voice.SetFilterParameters(sData.CurrentFilter.Value);
            }
        }

        public void SetPosition(float msecs)
        {
            m_elapsed = msecs;
            Update(0);
        }

        public void SetPositionRelative(float position)
        {
            m_elapsed = m_duration * position;
            Update(0);
        }

        private const int FINISHED_OP_SET = 1;
        private void EffectFinished()
        {
            if (m_ended)
                return;
            m_ended = true;
            for (int i = 0; i < m_sounds.Count; i++)
            {
                if (m_sounds[i].Sound == null || m_sounds[i].Sound.IsValid == false || m_sounds[i].Sound.Voice == null || m_sounds[i].Sound.Voice.IsValid() == false)
                    continue;
                m_sounds[i].Sound.Voice.SetFilterParameters(m_defaultFilter, FINISHED_OP_SET);
                if (i == m_effect.ResultEmitterIdx)
                    continue;
                m_sounds[i].Sound.Stop();
            }
            if(m_engine != null && !m_engine.IsDisposed)
                m_engine.CommitChanges(FINISHED_OP_SET);

            if(OutputSound != null)
                OutputSound.StoppedPlaying -= EffectFinished;
            var onEffectEnded = OnEffectEnded;
            if (onEffectEnded != null)
                onEffectEnded(this);
        }
    }
}
