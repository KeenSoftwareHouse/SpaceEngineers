using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SysUtils.Utils;
using SharpDX.XAudio2;
using SharpDX.Multimedia;
using VRage.CommonLib.Generics;
using Sandbox.Engine.Utils;
using System.Threading;
using System.Collections.Concurrent;
using System.Diagnostics;
using Sandbox.CommonLib.ObjectBuilders.Audio;

namespace Sandbox.Engine.Audio
{
    class MySourceVoice
    {
        MySourceVoicePool m_owner;
        SourceVoice m_voice;
        MySoundCuesEnum m_cueEnum;
        float m_frequencyRatio = 1f;

        bool m_isPlaying;
        bool m_isPaused;
        bool m_isLoopable;

        public SourceVoice Voice { get { return m_voice; } }
        public MySoundCuesEnum CueEnum { get { return m_cueEnum; } }
        public bool IsPlaying { get { return m_isPlaying; } }
        public bool IsPaused { get { return m_isPaused; } }
        public MySourceVoicePool Owner { get { return m_owner; } }
        public float FrequencyRatio
        {
            get { return m_frequencyRatio; }
            set { m_frequencyRatio = value; }
        }

        public MySourceVoice(MySourceVoicePool owner, SourceVoice voice)
        {
            m_owner = owner;
            m_voice = voice;
            m_isPlaying = false;
            m_isPaused = false;
            m_isLoopable = false;
        }

        public void SubmitSourceBuffer(MySoundCuesEnum cueEnum, AudioBuffer buffer, uint[] decodedXMWAPacketInfo, int sampleRate)
        {
            m_cueEnum = cueEnum;
            m_isLoopable = (buffer.LoopCount > 0);
            m_voice.SourceSampleRate = sampleRate;
            m_voice.SubmitSourceBuffer(buffer, decodedXMWAPacketInfo);
        }

        public void Start()
        {
            m_voice.Start();
            m_isPlaying = true;
        }

        public void OnStopPlaying(IntPtr context)
        {
            if (m_voice.State.BuffersQueued == 0)
            {
                m_isPlaying = false;
                m_owner.OnStopPlaying(this);
            }
        }

        public void Stop(bool force = false)
        {
            if (force || m_isLoopable)
                m_owner.AddToFadeoutList(this);
        }

        public void Pause()
        {
            m_voice.Stop();
            m_isPaused = true;
        }

        public void Resume()
        {
            m_voice.Start();
            m_isPaused = false;
        }

        public void SetVolume(float volume)
        {
            m_voice.SetVolume(volume);
        }
    }   
}
