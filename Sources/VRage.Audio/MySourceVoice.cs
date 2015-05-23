using SharpDX.Multimedia;
using SharpDX.XAudio2;
using System;
using System.Diagnostics;
using VRage.Utils;

namespace VRage.Audio
{
    class MySourceVoice : IMySourceVoice
    {
        public Action StoppedPlaying { get; set; }
        MySourceVoicePool m_owner;
        SourceVoice m_voice;
        MyStringId m_cue;
        MyInMemoryWave[] m_loopBuffers = new MyInMemoryWave[3];
        float m_frequencyRatio = 1f;
        VoiceSendDescriptor[] m_currentDescriptor;

        bool m_isPlaying;
        bool m_isPaused;
        bool m_isLoopable;
        private bool m_valid;

        public SourceVoice Voice { get { return m_voice; }}
        public MyStringId CueEnum { get { return m_cue; } }
        public bool IsPlaying { get { return m_isPlaying; } }
        public bool IsPaused { get { return m_isPaused; } }
        public bool IsLoopable { get { return m_isLoopable; } }
        public bool IsValid { get { return m_valid && m_voice != null && !m_voice.IsDisposed; } }
        public MySourceVoicePool Owner { get { return m_owner; } }
        public float FrequencyRatio
        {
            get { return m_frequencyRatio; }
            set { m_frequencyRatio = value; }
        }
        public float Volume { get { return Voice != null ? Voice.Volume : 0; } }

        public MySourceVoice(MySourceVoicePool owner, XAudio2 device, WaveFormat sourceFormat)
        {
            // This value influences how many native memory is allocated in XAudio
            // When shifting sound to higher frequency it needs more data, because it's compressed in time
            // Ratio 2 equals to 11 or 12 semitones (=1 octave)
            // Values around 32 should be pretty safe
            // Values around 128 needs large amount of memory
            // Values > 128 are memory killer
            const float MaxFrequencyRatio = 2;

            m_voice = new SourceVoice(device, sourceFormat, VoiceFlags.UseFilter, MaxFrequencyRatio, true);
            m_voice.BufferEnd += OnStopPlaying;
            m_valid = true;

            m_owner = owner;
            m_owner.OnAudioEngineChanged += m_owner_OnAudioEngineChanged;
            Flush();
        }

        void m_owner_OnAudioEngineChanged()
        {
            m_valid = false;
        }

        public void Flush()
        {
            m_cue = MyStringId.NullOrEmpty;
            m_voice.Stop();
            m_voice.FlushSourceBuffers();
            for (int i = 0; i < m_loopBuffers.Length; i++ )
                m_loopBuffers[i] = null;
            m_isPlaying = false;
            m_isPaused = false;
            m_isLoopable = false;
            m_currentDescriptor = null;
        }

        internal void SubmitSourceBuffer(MyStringId cue, MyInMemoryWave wave, MyCueBank.CuePart part)
        {
            m_loopBuffers[(int)part] = wave;
            m_cue = cue;
            m_isLoopable |= (wave.Buffer.LoopCount > 0);
        }

        private void SubmitSourceBuffer(MyInMemoryWave wave)
        {
            if (wave == null)
                return;
            m_isLoopable |= (wave.Buffer.LoopCount > 0);
            m_voice.SourceSampleRate = wave.WaveFormat.SampleRate;
            m_voice.SubmitSourceBuffer(wave.Buffer, wave.Stream.DecodedPacketsInfo);
        }

        public void Start(bool skipIntro, bool skipToEnd = false)
        {
            if (!skipIntro)
                SubmitSourceBuffer(m_loopBuffers[(int)MyCueBank.CuePart.Start]);
            else
                Debug.Assert(m_isLoopable, "Only loops should skip intro");

            if (m_isLoopable)
            {
                if(!skipToEnd)
                    SubmitSourceBuffer(m_loopBuffers[(int)MyCueBank.CuePart.Loop]);
                SubmitSourceBuffer(m_loopBuffers[(int)MyCueBank.CuePart.End]);
            }
            if (m_voice.State.BuffersQueued > 0)
            {
                m_voice.SetFrequencyRatio(FrequencyRatio);
                m_voice.Start();
                m_isPlaying = true;
            }
            else
                OnStopPlaying(m_voice.NativePointer);
        }

        private void OnStopPlaying(IntPtr context)
        {
            if (m_voice.State.BuffersQueued == 0)
            {
                m_isPlaying = false;
                m_owner.OnStopPlaying(this);
                if (StoppedPlaying != null)
                    StoppedPlaying();
            }
        }

        public void Stop(bool force = false)
        {
            if (!IsValid || !m_isPlaying)
                return;

            if (force || m_isLoopable)
                m_owner.AddToFadeoutList(this);
            else
            {
                m_voice.Stop();
                m_voice.FlushSourceBuffers();
            }
        }

        public void Pause()
        {
            m_voice.Stop();
            m_isPaused = true;
        }

        public void Resume()
        {
            m_voice.FlushSourceBuffers();
            if (m_isLoopable)
            {
                SubmitSourceBuffer(m_loopBuffers[(int)MyCueBank.CuePart.Loop]);
                SubmitSourceBuffer(m_loopBuffers[(int)MyCueBank.CuePart.End]);
            }
            else
                SubmitSourceBuffer(m_loopBuffers[(int)MyCueBank.CuePart.Start]);
            m_voice.Start();
            m_isPaused = false;
        }

        public void SetVolume(float volume)
        {
            if (m_voice != null)
                m_voice.SetVolume(volume);
        }

        public void SetOutputVoices(VoiceSendDescriptor[] descriptors)
        {
            if (m_currentDescriptor != descriptors)
            {
                m_voice.SetOutputVoices(descriptors);
                m_currentDescriptor = descriptors;
            }
        }

        public override string ToString()
        {
            return string.Format(m_cue.ToString());
        }

        internal void DestroyVoice()
        {
            m_voice.DestroyVoice();
            m_voice = null;
        }
    }
}
