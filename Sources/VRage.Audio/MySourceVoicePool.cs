using SharpDX.Multimedia;
using SharpDX.XAudio2;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using VRage.Collections;

namespace VRage.Audio
{
    public delegate void AudioEngineChanged();

    class MySourceVoicePool
    {
        public XAudio2 m_audioEngine;
        WaveFormat m_waveFormat;
        MyCueBank m_owner;
        MyConcurrentQueue<MySourceVoice> m_availableVoices;
        List<MySourceVoice> m_fadingOutVoices;
        public event AudioEngineChanged OnAudioEngineChanged;
        int m_currentCount;
        private const int MAX_COUNT = 32;
#if DEBUG
        public List<MySourceVoice> m_debugPlayingList = new List<MySourceVoice>();
#endif

        public WaveFormat WaveFormat { get { return m_waveFormat; } }

        public MySourceVoicePool(XAudio2 audioEngine, WaveFormat waveformat, MyCueBank owner)
        {
            m_audioEngine = audioEngine;
            m_waveFormat = waveformat;
            m_owner = owner;
            m_availableVoices = new MyConcurrentQueue<MySourceVoice>(32);
            m_fadingOutVoices = new List<MySourceVoice>();
            m_currentCount = 0;
        }

        public void SetAudioEngine(XAudio2 audioEngine)
        {
            if (m_audioEngine != audioEngine)
            {
                if (OnAudioEngineChanged != null) OnAudioEngineChanged();
                m_audioEngine = audioEngine;
                m_availableVoices.Clear();
                m_fadingOutVoices.Clear();
                m_currentCount = 0;
            }
        }

        internal MySourceVoice NextAvailable()
        {
            MySourceVoice voice = null;
            if (m_owner.DisablePooling || !m_availableVoices.TryDequeue(out voice))
            {
                if (m_currentCount < MAX_COUNT)
                {
                    voice = new MySourceVoice(this, m_audioEngine, m_waveFormat);
                    m_currentCount++;
                }
            }
#if DEBUG
            if (voice != null)
                m_debugPlayingList.Add(voice);
#endif
            return voice;
        }

        public void OnStopPlaying(MySourceVoice voice)
        {
#if DEBUG
            Debug.Assert(m_debugPlayingList.Contains(voice), string.Format("Debug only. Stopping not playing voice {0}", voice));
            m_debugPlayingList.Remove(voice);
#endif
            m_currentCount--;
            m_availableVoices.Enqueue(voice);
        }

        public void Update()
        {
            if (m_owner.DisablePooling)
            {
                MySourceVoice voice;
                for (int i = 0; i < m_availableVoices.Count; i++)
                    if (m_availableVoices.TryDequeue(out voice))
                    {
                        voice.DestroyVoice();
                    }
            }
            int id = 0;
            while (id < m_fadingOutVoices.Count)
            {
                MySourceVoice voice = m_fadingOutVoices[id];
                if (!voice.IsValid)
                {
                    m_fadingOutVoices.RemoveAt(id);
                    //m_fadingOutVoices.Insert(id, m_owner.GetVoice(voice.CueEnum));
                    //voice = m_fadingOutVoices[id];
                }
                else
                {
                    if (voice.Voice.Volume < 0.01f)
                    {
                        voice.Voice.Stop();
                        voice.Voice.FlushSourceBuffers(); // triggers voice's BufferEnd event
                        m_fadingOutVoices.RemoveAt(id);
                        continue;
                    }
                    else
                        voice.Voice.SetVolume(0.65f * voice.Voice.Volume);
                }

                ++id;
            }
        }

        public void AddToFadeoutList(MySourceVoice voice)
        {
            m_fadingOutVoices.Add(voice);
        }

#if DEBUG
        public override string ToString()
        {
            return string.Format("MySourceVoicePool [{0}-{1}-{2}] - voices count: {3}/{4}", m_waveFormat.Encoding, m_waveFormat.Channels, m_waveFormat.SampleRate, m_currentCount, MAX_COUNT);
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
#if DEBUG
            int id = 0;
            while (id < m_debugPlayingList.Count)
            {
                var item = m_debugPlayingList[id++];
                if (item.IsPlaying && !item.IsPaused)
                    stringBuilder.Append(item.CueEnum.ToString()).AppendDecimal(item.Volume,2).Append(", ");
                if (id % 6 == 0)
                    stringBuilder.AppendLine();
            }
            if(id > 0)
                stringBuilder.AppendLine();
#endif
        }

        public void WritePausedDebugInfo(StringBuilder stringBuilder)
        {
#if DEBUG
            int id = 0;
            while (id < m_debugPlayingList.Count)
            {
                var item = m_debugPlayingList[id++];
                if (item.IsPlaying && item.IsPaused)
                    stringBuilder.Append(item.CueEnum.ToString()).Append(", ");
                if (id % 6 == 0)
                    stringBuilder.AppendLine();
            }
            if (id > 0)
                stringBuilder.AppendLine();
#endif
        }
    }
}
