using SharpDX.Multimedia;
using SharpDX.XAudio2;
using System.Collections.Generic;
using System.Text;
using VRage.Collections;
using VRage.Data.Audio;

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

        public bool UseSameSoundLimiter = false;

        int m_currentCount;
        private const int MAX_COUNT = 128;
#if DEBUG
        public MyConcurrentHashSet<MySourceVoice> m_debugPlayingList = new MyConcurrentHashSet<MySourceVoice>();
#endif
        MyConcurrentHashSet<MySourceVoice> m_allVoices = new MyConcurrentHashSet<MySourceVoice>();

        public WaveFormat WaveFormat { get { return m_waveFormat; } }
        private List<MySourceVoice> m_voiceBuffer = new List<MySourceVoice>();
        private List<MySourceVoice> m_voiceBuffer2 = new List<MySourceVoice>();
        private List<MySourceVoice> m_voicesToRemove = new List<MySourceVoice>();
        private List<MySourceVoice> m_distancedVoices = new List<MySourceVoice>();

        public MySourceVoicePool(XAudio2 audioEngine, WaveFormat waveformat, MyCueBank owner)
        {
            m_audioEngine = audioEngine;
            m_waveFormat = waveformat;
            m_owner = owner;
            m_availableVoices = new MyConcurrentQueue<MySourceVoice>(MAX_COUNT);
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
                    m_allVoices.Add(voice);
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
            //Debug.Assert(m_debugPlayingList.Contains(voice), string.Format("Debug only. Stopping not playing voice {0}", voice));
            m_debugPlayingList.Remove(voice);
#endif
            m_currentCount--;
            m_availableVoices.Enqueue(voice);
        }

        public void Update()
        {
            if (m_owner == null || m_audioEngine == null)
                return;
            int i;
            if (m_owner.DisablePooling)
            {
                MySourceVoice voice;
                for (i = 0; i < m_voiceBuffer2.Count; i++)
                {
                    m_voiceBuffer2[i].Dispose();
                }
                m_voiceBuffer2 = m_voiceBuffer;
                m_voiceBuffer.Clear();
                for (i = 0; i < m_availableVoices.Count; i++)
                    if (m_availableVoices.TryDequeue(out voice))
                    {
                        voice.DestroyVoice();
                        m_voiceBuffer.Add(voice);
                    }
            }
            int id = 0;

            //fading out
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

            //check for invalid voices
            m_voicesToRemove.Clear();
            foreach (MySourceVoice voice in m_allVoices)
            {
                if (voice.IsValid == false)
                    m_voicesToRemove.Add(voice);
            }

            //remove invalid voices
            while (m_voicesToRemove.Count > 0)
            {
                m_allVoices.Remove(m_voicesToRemove[0]);
                m_voicesToRemove.RemoveAt(0);
            }

            //silent sounds playing in large number (sameSoundLimiterCount)
            if (UseSameSoundLimiter)
            {
                //add remaining voices to distance and sort them
                m_distancedVoices.Clear();
                foreach (MySourceVoice voice in m_allVoices)
                {
                    m_distancedVoices.Add(voice);
                }
                m_distancedVoices.Sort(delegate(MySourceVoice x, MySourceVoice y)
                {
                    return x.distanceToListener.CompareTo(y.distanceToListener);
                });

                //silent or un-silent voices
                MyCueId currentCueId;
                int j,limit;
                MySoundData cueDefinition;
                while (m_distancedVoices.Count > 0)
                {
                    currentCueId = m_distancedVoices[0].CueEnum;
                    i = 0;
                    cueDefinition = MyAudio.Static.GetCue(currentCueId);
                    limit = cueDefinition != null ? cueDefinition.SoundLimit : 0;
                    for (j = 0; j < m_distancedVoices.Count; j++)
                    {
                        if (m_distancedVoices[j].CueEnum.Equals(currentCueId))
                        {
                            i++;
                            if (limit > 0 && i > limit)
                                m_distancedVoices[j].Silent = true;
                            else
                                m_distancedVoices[j].Silent = false;
                            m_distancedVoices.RemoveAt(j);
                            j--;
                        }
                    }
                }
            }
        }

        public void AddToFadeoutList(MySourceVoice voice)
        {
            m_fadingOutVoices.Add(voice);
        }

        public void StopAll()
        {
            foreach (var v in m_allVoices)
            {
                v.Stop(true);
            }
        }

        public void Dispose()
        {
            m_availableVoices.Clear();
            m_fadingOutVoices.Clear();
            m_currentCount = 0;
            m_audioEngine = null;
            m_owner = null;
            foreach(var v in m_allVoices)
            {
                v.Cleanup();
                m_voiceBuffer.Add(v);
            }
            m_allVoices.Clear();
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
            foreach(var item in m_debugPlayingList)
            {
                if (item.IsPlaying && !item.IsPaused && !item.Silent && item.VolumeMultiplier != 0f)
                {
                    if(id > 0)
                        stringBuilder.Append(", ");
                    if (id % 5 == 0 && id > 0)
                        stringBuilder.AppendLine();
                    stringBuilder.Append(item.CueEnum.ToString() + " ").AppendDecimal(item.Volume*item.VolumeMultiplier, 2);
                    id++;
                }
            }

            if(id > 0)
                stringBuilder.AppendLine();
#endif
        }

        public void WritePausedDebugInfo(StringBuilder stringBuilder)
        {
#if DEBUG
            int id = 0;
            foreach(var item in m_debugPlayingList)
            {
                if (item.IsPlaying && (item.IsPaused || item.Silent || item.VolumeMultiplier == 0f))
                {
                    if (id > 0)
                        stringBuilder.Append(", ");
                    if (id % 4 == 0 && id > 0)
                        stringBuilder.AppendLine();
                    stringBuilder.Append(item.CueEnum.ToString() + " ");
                    if (item.Silent)
                        stringBuilder.Append("LIM");
                    else if (item.VolumeMultiplier == 0f)
                        stringBuilder.Append("SIL");
                    else
                        stringBuilder.Append("PAU");
                    id++;
                }
            }
            if (id > 0)
                stringBuilder.AppendLine();
#endif
        }
    }
}
