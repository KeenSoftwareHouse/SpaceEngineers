using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sandbox.CommonLib.ObjectBuilders.Audio;
using Sandbox.Engine.Utils;
using SysUtils.Utils;
using VRage.CommonLib.Utils;
using SharpDX.XAudio2;
using SharpDX.XAudio2.Fx;
using SharpDX.Multimedia;

namespace Sandbox.Engine.Audio
{
    struct MyWaveFormat
    {
        public class Comparer : IEqualityComparer<MyWaveFormat>
        {
            public bool Equals(MyWaveFormat x, MyWaveFormat y)
            {
                if (x.Encoding != y.Encoding)
                    return false;

                if (x.Channels != y.Channels)
                    return false;

                if ((x.Encoding != WaveFormatEncoding.Adpcm) && (x.SampleRate != y.SampleRate))
                    return false;

                return true;
            }

            public int GetHashCode(MyWaveFormat obj)
            {
                unchecked
                {
                    int result = (int)obj.Encoding;
                    result = (result * 397) ^ obj.Channels;
                    if (obj.Encoding != WaveFormatEncoding.Adpcm)
                        result = (result * 397) ^ obj.SampleRate;

                    return result;
                }
            }
        }

        public WaveFormatEncoding Encoding;
        public int Channels;
        public int SampleRate;
        public WaveFormat WaveFormat;
    }

    class MyCueBank : IDisposable
    {
        XAudio2 m_audioEngine;

        Dictionary<int, MyObjectBuilder_CueDefinition> m_cues;
        HashSet<WeakReference>[] m_nonLoopableCuesLimit;    //  Here we will remember every non-loopable cue we are playing. It will serve for limiting max count of same cues played too.

        MyWaveBank m_waveBank;
        Dictionary<MyWaveFormat, MySourceVoicePool> m_voicePools;
        Dictionary<string, MySoundCuesEnum>[] m_musicTransitionCues;
        List<string> m_categories;

        bool m_applyReverb;
        EffectDescriptor m_effectDescriptor;

        public MyCueBank(XAudio2 audioEngine, MyObjectBuilder_CueDefinitions cues)
        {
            m_audioEngine = audioEngine;
            if (cues.Cues.Length > 0)
            {
                InitTransitionCues();
                InitMusicCategories();

                m_cues = new Dictionary<int, MyObjectBuilder_CueDefinition>(cues.Cues.Length);
                InitCues(cues);
                InitCategories();

                InitWaveBank();
                InitVoicePools();

#if DEBUG
                ValidateCues();
#endif //DEBUG

                InitNonLoopableCuesLimitRemoveHelper();

                Reverb reverb = new Reverb();
                m_effectDescriptor = new EffectDescriptor(reverb);
            }
        }

        public void SetAudioEngine(XAudio2 audioEngine)
        {
            if (m_audioEngine != audioEngine)
            {
                m_audioEngine = audioEngine;
                foreach (MySourceVoicePool voicePool in m_voicePools.Values)
                {
                    voicePool.SetAudioEngine(audioEngine);
                }
            }
        }

        public int Count { get { return m_cues.Count; } }

        private void InitCues(MyObjectBuilder_CueDefinitions cues)
        {
            foreach (var cue in cues.Cues)
            {
                bool found = false;
                foreach (MySoundCuesEnum soundCue in Enum.GetValues(typeof(MySoundCuesEnum)))
                {
                    if (cue.Name == soundCue.ToString())
                    {
                        m_cues[(int)soundCue] = cue;
                        found = true;
                        break;
                    }
                }
                MyDebug.AssertRelease(found, string.Format("Cue {0} not in enum", cue.Name));
            }
        }

        private void InitCategories()
        {
            m_categories = new List<string>();
            foreach (var cue in m_cues)
            {
                if (!m_categories.Contains(cue.Value.Category))
                    m_categories.Add(cue.Value.Category);
            }
        }

        private void InitWaveBank()
        {
            m_waveBank = new MyWaveBank();
            foreach (var cue in m_cues)
            {
                if (cue.Value.Waves == null)
                    continue;

                foreach (var wave in cue.Value.Waves)
                {
                    m_waveBank.Add(cue.Value, wave);
                }
            }
        }

        private void InitVoicePools()
        {
            List<MyWaveFormat> waveFormats = m_waveBank.GetWaveFormats();
            if (waveFormats.Count > 0)
            {
                m_voicePools = new Dictionary<MyWaveFormat, MySourceVoicePool>(waveFormats.Count, new MyWaveFormat.Comparer());
                foreach (MyWaveFormat waveFormat in waveFormats)
                {
                    m_voicePools[waveFormat] = new MySourceVoicePool(m_audioEngine, waveFormat.WaveFormat, this);
                }
            }
        }

        private void InitTransitionCues()
        {
            m_musicTransitionCues = new Dictionary<string, MySoundCuesEnum>[MyVRageUtils.GetMaxValueFromEnum<MyMusicTransitionEnum>() + 1];
        }

        private int GetNumberOfSounds()
        {
            return MyVRageUtils.GetMaxValueFromEnum<MySoundCuesEnum>() + 1;
        }

        private void InitMusicCategories()
        {
            AddMusicCategory(MyMusicTransitionEnum.CalmAtmosphere, "KA01", MySoundCuesEnum.MusCalmAtmosphere_KA01);
            AddMusicCategory(MyMusicTransitionEnum.CalmAtmosphere, "KA02", MySoundCuesEnum.MusCalmAtmosphere_KA02);
            AddMusicCategory(MyMusicTransitionEnum.CalmAtmosphere, "KA03", MySoundCuesEnum.MusCalmAtmosphere_KA03);
            AddMusicCategory(MyMusicTransitionEnum.CalmAtmosphere, "KA05", MySoundCuesEnum.MusCalmAtmosphere_KA05);
            AddMusicCategory(MyMusicTransitionEnum.CalmAtmosphere, "MM_b", MySoundCuesEnum.MusCalmAtmosphere_MM_b);
            AddMusicCategory(MyMusicTransitionEnum.CalmAtmosphere, "MM01", MySoundCuesEnum.MusCalmAtmosphere_MM01);
            AddMusicCategory(MyMusicTransitionEnum.CalmAtmosphere, "MM02", MySoundCuesEnum.MusCalmAtmosphere_MM02);
            AddMusicCategory(MyMusicTransitionEnum.DesperateWithStress, "KA01", MySoundCuesEnum.MusDesperateWithStress_KA01);
            AddMusicCategory(MyMusicTransitionEnum.DesperateWithStress, "KA02", MySoundCuesEnum.MusDesperateWithStress_KA02);
            AddMusicCategory(MyMusicTransitionEnum.DesperateWithStress, "KA03", MySoundCuesEnum.MusDesperateWithStress_KA03);
            AddMusicCategory(MyMusicTransitionEnum.DesperateWithStress, "KA04", MySoundCuesEnum.MusDesperateWithStress_KA04);
            AddMusicCategory(MyMusicTransitionEnum.HeavyFight, "KA01", MySoundCuesEnum.MusHeavyFight_KA01);
            AddMusicCategory(MyMusicTransitionEnum.HeavyFight, "KA02", MySoundCuesEnum.MusHeavyFight_KA02);
            AddMusicCategory(MyMusicTransitionEnum.HeavyFight, "KA03", MySoundCuesEnum.MusHeavyFight_KA03);
            AddMusicCategory(MyMusicTransitionEnum.HeavyFight, "KA04", MySoundCuesEnum.MusHeavyFight_KA04);
            AddMusicCategory(MyMusicTransitionEnum.HeavyFight, "KA05", MySoundCuesEnum.MusHeavyFight_KA05);
            AddMusicCategory(MyMusicTransitionEnum.HeavyFight, "KA07", MySoundCuesEnum.MusHeavyFight_KA07);
            AddMusicCategory(MyMusicTransitionEnum.HeavyFight, "KA15", MySoundCuesEnum.MusHeavyFight_KA15);
            AddMusicCategory(MyMusicTransitionEnum.Mystery, "KA01", MySoundCuesEnum.MusMystery_KA01);
            AddMusicCategory(MyMusicTransitionEnum.Mystery, "KA02", MySoundCuesEnum.MusMystery_KA02);
            AddMusicCategory(MyMusicTransitionEnum.SadnessOrDesperation, "KA01", MySoundCuesEnum.MusSadnessOrDesperation_KA01);
            AddMusicCategory(MyMusicTransitionEnum.SadnessOrDesperation, "KA02", MySoundCuesEnum.MusSadnessOrDesperation_KA02);
            AddMusicCategory(MyMusicTransitionEnum.SadnessOrDesperation, "MM01", MySoundCuesEnum.MusSadnessOrDesperation_MM01);
        }

        private void AddMusicCategory(MyMusicTransitionEnum musicTransition, string category, MySoundCuesEnum cueEnum)
        {
            if (m_musicTransitionCues[(int)musicTransition] == null)
            {
                m_musicTransitionCues[(int)musicTransition] = new Dictionary<string, MySoundCuesEnum>();
            }
            m_musicTransitionCues[(int)musicTransition].Add(category, cueEnum);
        }

        private void InitNonLoopableCuesLimitRemoveHelper()
        {
            //  Here we will remember every non-loopable cue we are playing. It will serve for limiting max count of same cues played too.
            if (MyAudioConstants.LIMIT_MAX_SAME_CUES == true)
            {
                m_nonLoopableCuesLimit = new HashSet<WeakReference>[GetNumberOfSounds()];
                foreach (short i in Enum.GetValues(typeof(MySoundCuesEnum)))
                {
                    if (m_cues[i].Loopable == false)
                        m_nonLoopableCuesLimit[i] = new HashSet<WeakReference>();
                }
            }
        }

        private void ValidateCues()
        {
            MyObjectBuilder_CueDefinition cue;
            foreach (MySoundCuesEnum soundCue in Enum.GetValues(typeof(MySoundCuesEnum)))
            {
                if (soundCue == MySoundCuesEnum.None)
                    continue;

                System.Diagnostics.Debug.Assert(m_cues.TryGetValue((int)soundCue, out cue), "Cue \"" + soundCue.ToString() + "\" does not exist in cues bank!");
            }
        }

        public void Update()
        {
            foreach (var voicePool in m_voicePools)
            {
                voicePool.Value.Update();
            }
        }

        public void Dispose()
        {
//#if DEBUG
//            if (m_voicePools != null)
//            {
//                foreach (MySourceVoicePool voicePool in m_voicePools.Values)
//                {
//                    MySandboxGame.Log.WriteLine(voicePool.ToString());
//                }
//            }
//#endif //DEBUG

            if (m_waveBank != null)
                m_waveBank.Dispose();
        }

        public bool ApplyReverb
        {
            get { return m_applyReverb; }
            set { m_applyReverb = value; }
        }

        public MyMusicTransitionEnum GetRandomTransitionEnum()
        {
            return (MyMusicTransitionEnum)MyVRageUtils.GetRandomInt(m_musicTransitionCues.Length);
        }

        public string GetRandomTransitionCategory(MyMusicTransitionEnum transitionEnum)
        {
            int randomIndex = MyVRageUtils.GetRandomInt(m_musicTransitionCues[(int)transitionEnum].Count);
            int currentIndex = 0;
            foreach (var categoryCueKVP in m_musicTransitionCues[(int)transitionEnum])
            {
                if (currentIndex == randomIndex)
                {
                    return categoryCueKVP.Key;
                }
                currentIndex++;
            }
            throw new InvalidBranchException();
        }

        public bool IsValidTransitionCategory(MyMusicTransitionEnum transitionEnum, string category)
        {
            return m_musicTransitionCues[(int)transitionEnum].ContainsKey(category);
        }

        public MySoundCuesEnum GetTransitionCue(MyMusicTransitionEnum transitionEnum, string category)
        {
            return m_musicTransitionCues[(int)transitionEnum][category];
        }

        public MyObjectBuilder_CueDefinition GetCue(MySoundCuesEnum cueEnum)
        {
            return m_cues[(int)cueEnum];
        }

        public List<string> GetCategories()
        {
            return m_categories;
        }

        public MyInMemoryWave GetRandomWave(MyObjectBuilder_CueDefinition cue)
        {
            int randomIndex = MyVRageUtils.GetRandomInt(cue.Waves.Length);
            string waveToPlay = cue.Waves[randomIndex];
            MyInMemoryWave wave = m_waveBank.GetWave(waveToPlay);
            if (wave == null)
                return null;

            return wave;
        }

        public bool IsPlayingHudSounds()
        {
            foreach (var voicePool in m_voicePools.ToArray())
            {
                if (voicePool.Value.IsPlayingHudSounds())
                    return true;
            }

            return false;
        }

        public MySourceVoice GetVoice(MySoundCuesEnum cueEnum)
        {
            MyObjectBuilder_CueDefinition cue = GetCue(cueEnum);
            if ((cue == null) || (cue.Waves == null) || (cue.Waves.Length == 0))
                return null;

            MyInMemoryWave wave = GetRandomWave(cue);
            if (wave == null)
                return null;

            MyWaveFormat myWaveFormat = new MyWaveFormat()
            {
                Encoding = wave.WaveFormat.Encoding,
                Channels = wave.WaveFormat.Channels,
                SampleRate = wave.WaveFormat.SampleRate,
                WaveFormat = wave.WaveFormat
            };

            MySourceVoice voice = m_voicePools[myWaveFormat].NextAvailable();
            if (voice == null)
                return null;

            voice.SubmitSourceBuffer(cueEnum, wave.Buffer, wave.Stream.DecodedPacketsInfo, wave.WaveFormat.SampleRate);
            if (m_applyReverb)
            {
                voice.Voice.SetEffectChain(m_effectDescriptor);
                voice.Voice.EnableEffect(0);
            }
            else
            {
                voice.Voice.SetEffectChain(null);
            }

            return voice;
        }

        public void WriteDebugInfo(StringBuilder stringBuilder)
        {
            if (m_voicePools == null)
                return;

            stringBuilder.Append("Playing: ");
            foreach (var voicePool in m_voicePools)
            {
                voicePool.Value.WritePlayingDebugInfo(stringBuilder);
            }

            stringBuilder.Append("Paused: ");
            foreach (var voicePool in m_voicePools)
            {
                voicePool.Value.WritePausedDebugInfo(stringBuilder);
            }
        }

        public MyObjectBuilder_CueDefinitions GetObjectBuilder()
        {
            MyObjectBuilder_CueDefinitions cues = new MyObjectBuilder_CueDefinitions();

            cues.Cues = m_cues.Values.ToArray();

            return cues;
        }
    }
}
