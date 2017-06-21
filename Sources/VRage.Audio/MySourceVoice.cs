using SharpDX;
using SharpDX.Multimedia;
using SharpDX.XAudio2;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using VRage.Data.Audio;
using VRage.Utils;

namespace VRage.Audio
{
    public static class VoiceExtensions
    {
        public static bool IsValid(this SourceVoice self)
        {
            return !self.IsDisposed && self.NativePointer != IntPtr.Zero;
        }
    }

    class MySourceVoice : IMySourceVoice
    {
        public Action StoppedPlaying { get; set; }
        MySourceVoicePool m_owner;
        SourceVoice m_voice;
        MyCueId m_cueId;
        MyInMemoryWave[] m_loopBuffers = new MyInMemoryWave[3];
        float m_frequencyRatio = 1f;
        VoiceSendDescriptor[] m_currentDescriptor;
        Queue<DataStream> m_dataStreams;// = new Queue<DataStream>();

        bool m_isPlaying;
        bool m_isPaused;
        bool m_isLoopable;
        static private readonly object theLock = new Object();
        private bool m_valid;
        private bool m_buffered;
        public float distanceToListener = float.MaxValue;

        public SourceVoice Voice { get { return m_voice; } }
        public MyCueId CueEnum { get { return m_cueId; } }
        public bool IsPlaying { get { return m_isPlaying; } }
        public bool IsPaused { get { return m_isPaused; } }
        public bool IsLoopable { get { return m_isLoopable; } }
        public bool IsValid { get { return m_valid && m_voice != null && m_voice.IsValid(); } }
        public MySourceVoicePool Owner { get { return m_owner; } }
        public float FrequencyRatio
        {
            get 
            {
                return m_frequencyRatio;
            }
            set
            { 
                m_frequencyRatio = value;
                MySoundData soundData = MyAudio.Static.GetCue(m_cueId);
                if (soundData != null && soundData.DisablePitchEffects)
                    return;
                if (m_voice != null && m_voice.IsValid()){
                    try
                    {
                        VoiceState state = m_voice.State;//this sometimes fails - not sure why since we already check for null and IsValid
                        if (state.BuffersQueued > 0)
                            m_voice.SetFrequencyRatio(FrequencyRatio);
                    }
                    catch (NullReferenceException)
                    {
                    }
                }
            }
        }
        private float m_volumeBase = 1f;
        public float Volume { get { return IsValid ? m_volumeBase : 0; } }
        private float m_volumeMultiplier = 1f;
        public float VolumeMultiplier
        { 
            get { return IsValid ? m_volumeMultiplier : 1f; }
            set 
            {
                m_volumeMultiplier = value;
                SetVolume(m_volumeBase);
            } 
        }
        public bool Silent = false;
        public bool IsBuffered { get { return m_buffered; } }

        public MySourceVoice(XAudio2 device, WaveFormat sourceFormat)
        {
            m_voice = new SourceVoice(device, sourceFormat, true);
            m_voice.BufferEnd += OnStopPlayingBuffered;
            m_valid = true;
            m_dataStreams = new Queue<DataStream>();

            Flush();
        }

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
            m_cueId = new MyCueId(MyStringHash.NullOrEmpty);
            m_voice.Stop();
            m_voice.FlushSourceBuffers();
            DisposeWaves();

            m_isPlaying = false;
            m_isPaused = false;
            m_isLoopable = false;
            m_currentDescriptor = null;
        }

        internal void SubmitSourceBuffer(MyCueId cueId, MyInMemoryWave wave, MyCueBank.CuePart part)
        {
            m_loopBuffers[(int)part] = wave;
            m_cueId = cueId;
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

            if (m_isLoopable || skipToEnd)
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

        public void StartBuffered()
        {
            if (m_voice.State.BuffersQueued > 0)
            {
                m_voice.SetFrequencyRatio(FrequencyRatio);
                m_voice.Start();
                m_isPlaying = true;
                m_buffered = true;
            }
            else
                OnStopPlaying(m_voice.NativePointer);
        }

        public void SubmitBuffer(byte[] buffer, int size)
        {
            Debug.Assert(m_dataStreams != null, "SourceVoice wasnt created with buffer support");
            var dataStream = DataStream.Create(buffer, true, false);
            AudioBuffer buff = new AudioBuffer(dataStream);
            buff.Flags = BufferFlags.None;

            m_dataStreams.Enqueue(dataStream);
            m_voice.SubmitSourceBuffer(buff, null);
        }

        private void OnStopPlayingBuffered(IntPtr context)
        {
            Debug.Assert(m_dataStreams != null, "SourceVoice wasnt created with buffer support");
            if (m_dataStreams.Count > 0)
            {
                var dataStream = m_dataStreams.Dequeue();
                dataStream.Dispose();
            }
            OnStopPlaying(context);
        }

        private void OnStopPlaying(IntPtr context)
        {
            if (m_voice.State.BuffersQueued == 0)
            {
                m_buffered = false;
                m_isPlaying = false;
                if (m_owner != null)
                    m_owner.OnStopPlaying(this);
                if (StoppedPlaying != null)
                    StoppedPlaying();
            }
        }

        public void Stop(bool force = false)
        {
            if (!IsValid || !m_isPlaying)
                return;

            if ((force || m_isLoopable) && m_owner != null)
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
            m_volumeBase = volume;
            if (IsValid)
            {
                try
                {
                    m_voice.SetVolume(m_volumeBase * m_volumeMultiplier);
                }
                catch (NullReferenceException)
                {
                }
            }
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
            return string.Format(m_cueId.ToString());
        }

        public void Dispose()
        {
            if (m_voice == null)
                return;

            DisposeWaves();
            m_voice.DestroyVoice();
            m_voice.Dispose();
            m_voice = null;
        }

        private void DisposeWaves()
        {
            if (m_loopBuffers != null)
                for (int i = 0; i < m_loopBuffers.Length; i++)
                {
                    if (m_loopBuffers[i] != null && m_loopBuffers[i].Streamed)
                        m_loopBuffers[i].Dereference();
                    m_loopBuffers[i] = null;
                }
        }

        internal void DestroyVoice()
        {
            m_valid = false;
            m_currentDescriptor = null;
            StoppedPlaying = null;
            m_owner = null;
            m_loopBuffers = null;

            DisposeWaves();

            if (m_voice == null && m_dataStreams == null)
                return;

            lock (theLock)
            {
                if (m_voice != null && !m_voice.IsDisposed)
                {
                    if (m_voice.NativePointer != IntPtr.Zero)
                    {
                        if (IsValid)
                            m_voice.Stop();
                    }
                    if (m_dataStreams != null)
                        m_voice.BufferEnd -= OnStopPlayingBuffered;
                    else
                        m_voice.BufferEnd -= OnStopPlaying;
                }

                if (m_dataStreams != null)
                {
                    foreach (var dataStream in m_dataStreams)
                        dataStream.Dispose();
                    m_dataStreams.Clear();
                }
                m_dataStreams = null;
            }
        }

        public void Cleanup()
        {
            DestroyVoice();
        }
    }
}
