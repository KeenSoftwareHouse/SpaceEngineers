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
    class MySourceVoicePool
    {
        XAudio2 m_audioEngine;
        WaveFormat m_waveFormat;
        MyCueBank m_owner;
        Queue<MySourceVoice> m_availableVoices;
        List<MySourceVoice> m_playingVoices;
        List<MySourceVoice> m_fadingOutVoices;
        int m_maxCount;

        public WaveFormat WaveFormat { get { return m_waveFormat; } }

        public MySourceVoicePool(XAudio2 audioEngine, WaveFormat waveformat, MyCueBank owner)
        {
            m_audioEngine = audioEngine;
            m_waveFormat = waveformat;
            m_owner = owner;
            m_availableVoices = new Queue<MySourceVoice>();
            m_playingVoices = new List<MySourceVoice>();
            m_fadingOutVoices = new List<MySourceVoice>();
            m_maxCount = 0;
        }

        public void SetAudioEngine(XAudio2 audioEngine)
        {
            if (m_audioEngine != audioEngine)
            {
                m_audioEngine = audioEngine;
                m_playingVoices.Clear();
                m_availableVoices.Clear();
                m_fadingOutVoices.Clear();
                m_maxCount = 0;
            }
        }

        public MySourceVoice NextAvailable()
        {
            if (m_availableVoices.Count == 0)
            {
                SourceVoice sourceVoice = new SourceVoice(m_audioEngine, m_waveFormat, VoiceFlags.None, XAudio2.MaximumFrequencyRatio, true);
                MySourceVoice voice = new MySourceVoice(this, sourceVoice);
                sourceVoice.BufferEnd += voice.OnStopPlaying;
                m_availableVoices.Enqueue(voice);
                ++m_maxCount;
            }
            MySourceVoice next = m_availableVoices.Dequeue();
            m_playingVoices.Add(next);

            return next;
        }

        public void OnStopPlaying(MySourceVoice voice)
        {
            if (m_playingVoices.Contains(voice))
            {
                m_availableVoices.Enqueue(voice);
                m_playingVoices.Remove(voice);
            }
        }

        public void Update()
        {
            int id = 0;
            while (id < m_fadingOutVoices.Count)
            {
                MySourceVoice voice = m_fadingOutVoices[id];
                if (voice.Voice.Volume < 0.01f)
                {
                    voice.Voice.Stop();
                    voice.Voice.FlushSourceBuffers(); // triggers voice's BufferEnd event
                    m_fadingOutVoices.RemoveAt(id);
                    --id;
                }
                else
                    voice.Voice.SetVolume(0.75f * voice.Voice.Volume);

                ++id;
            }
        }

        public void AddToFadeoutList(MySourceVoice voice)
        {
            m_fadingOutVoices.Add(voice);
        }

        public bool IsPlayingHudSounds()
        {
            var list = m_playingVoices.ToArray();
            foreach (var voice in list)
            {
                if (voice == null)
                    continue;

                MyObjectBuilder_CueDefinition cue = m_owner.GetCue(voice.CueEnum);
                if (cue.IsHudCue)
                    return true;
            }

            return false;
        }

#if DEBUG
        public override string ToString()
        {
            return string.Format("MySourceVoicePool [{0}-{1}-{2}] - max voices count: {3}", m_waveFormat.Encoding, m_waveFormat.Channels, m_waveFormat.SampleRate, m_maxCount);
        }
#endif //DEBUG

//        public void Dispose()
//        {
//            foreach (MySourceVoice voice in m_playingVoices)
//            {
//                m_availableVoices.Enqueue(voice);
//            }
//            m_playingVoices.Clear();
//
//#if DEBUG
//            MyLog.Default.WriteLine(this.ToString());
//#endif //DEBUG
//
//            while (m_availableVoices.Count > 0)
//            {
//                MySourceVoice voice = m_availableVoices.Dequeue();
//                if (voice.Voice != null)
//                {
//                    voice.Voice.DestroyVoice();
//                    voice.Voice.Dispose();
//                }
//            }
//        }

        public void WritePlayingDebugInfo(StringBuilder stringBuilder)
        {
            int id = 0;
            while (id < m_playingVoices.Count)
            {
                var item = m_playingVoices[id++];
                if (item.IsPlaying && !item.IsPaused)
                    stringBuilder.Append(MyEnumsToStrings.Sounds[(int)item.CueEnum]).Append(", ");
            }
        }

        public void WritePausedDebugInfo(StringBuilder stringBuilder)
        {
            int id = 0;
            while (id < m_playingVoices.Count)
            {
                var item = m_playingVoices[id++];
                if (item.IsPlaying && item.IsPaused)
                    stringBuilder.Append(MyEnumsToStrings.Sounds[(int)item.CueEnum]).Append(", ");
            }
        }
    }
}
