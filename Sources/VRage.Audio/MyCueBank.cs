﻿using SharpDX.Multimedia;
using SharpDX.XAudio2;
using SharpDX.XAudio2.Fx;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using VRage.Collections;
using VRage.Data.Audio;
using VRage.Library.Utils;
using VRage.Utils;

namespace VRage.Audio
{
    public enum MyGuiSounds
    {
        HudClick,
        HudUse,
        HudRotateBlock,
        HudPlaceBlock,
        HudDeleteBlock,
        HudColorBlock,
        HudMouseClick,
        HudMouseOver,
        HudUnable,
        PlayDropItem,
        HudVocInventoryFull,
        HudVocMeteorInbound,
        HudVocHealthLow,
        HudVocHealthCritical,
        None,
        HudVocEnergyLow,
        HudVocStationFuelLow,
        HudVocShipFuelLow,
        HudVocEnergyCrit,
        HudVocStationFuelCrit,
        HudVocShipFuelCrit,
        HudVocEnergyNo,
        HudVocStationFuelNo,
        HudVocShipFuelNo,
        HudCraftBarProgressLoop,
        HudErrorMessage,
        HudOpenCraftWin,
        HudOpenInventory,
        HudItem,
        PlayTakeItem,
        HudPlaceItem
    }

    public struct MyWaveFormat : IEquatable<MyWaveFormat>
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
                    //byte ch = (byte)obj.Channels;
                    //int result = (int)((((uint)obj.Encoding) << 20) ^ (ch << 12) ^ ((uint)obj.SampleRate));

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

        public bool Equals(MyWaveFormat y)
        {
            if (Encoding != y.Encoding)
                return false;

            if (Channels != y.Channels)
                return false;

            if ((Encoding != WaveFormatEncoding.Adpcm) && (SampleRate != y.SampleRate))
                return false;

            return true;
        }
    }

    public class MyCueBank : IDisposable
    {
        public enum CuePart
        {
            Start = 0,
            Loop = 1,
            End = 2
        }

        XAudio2 m_audioEngine;

        Dictionary<MyCueId, MySoundData> m_cues;

        MyWaveBank m_waveBank;
        Dictionary<MyWaveFormat, MySourceVoicePool> m_voicePools;
        Dictionary<MyStringId, Dictionary<MyStringId, MyCueId>> m_musicTransitionCues;
        Dictionary<MyStringId, List<MyCueId>> m_musicTracks;
        List<MyStringId> m_categories;

        public bool UseSameSoundLimiter = false;

#if DEBUG
        public static List<StringBuilder> lastSounds = new List<StringBuilder>();
        public static int lastSoundIndex = 0;
        private const int LAST_SOUND_COUNT = 8;
#endif
        bool m_applyReverb = false;
        EffectDescriptor m_effectDescriptor;
        Reverb m_reverb;

        public bool DisablePooling { get; set; }

        public MyCueBank(XAudio2 audioEngine, ListReader<MySoundData> cues)
        {
            m_audioEngine = audioEngine;
            if (cues.Count > 0)
            {
                m_cues = new Dictionary<MyCueId, MySoundData>(cues.Count, MyCueId.Comparer);
                InitTransitionCues();
                InitCues(cues);
                InitCategories();

                InitWaveBank();
                InitVoicePools();

                m_reverb = new Reverb(audioEngine);
                m_effectDescriptor = new EffectDescriptor(m_reverb);
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

        private static MyStringId MUSIC_CATEGORY = MyStringId.GetOrCompute("Music");
        private void InitCues(ListReader<MySoundData> cues)
        {
            foreach (var cue in cues)
            {
                Debug.Assert(m_cues.Where((v) => v.Value.SubtypeId == cue.SubtypeId).Count() == 0, "Cue with this name was already added.");
                var id = new MyCueId(cue.SubtypeId);
                m_cues[id] = cue;
                if (cue.Category == MUSIC_CATEGORY)
                    AddMusicCue(cue.MusicTrack.TransitionCategory, cue.MusicTrack.MusicCategory, id);
            }
        }

        private void InitCategories()
        {
            m_categories = new List<MyStringId>();
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
                m_voicePools = new Dictionary<MyWaveFormat, MySourceVoicePool>(waveFormats.Count);
                foreach (MyWaveFormat waveFormat in waveFormats)
                {
                    m_voicePools[waveFormat] = new MySourceVoicePool(m_audioEngine, waveFormat.WaveFormat, this);
                    m_voicePools[waveFormat].UseSameSoundLimiter = UseSameSoundLimiter;
                }
            }
        }

        public void SetSameSoundLimiter()
        {
            if (m_voicePools != null)
            {
                foreach (MySourceVoicePool voicePool in m_voicePools.Values)
                {
                    voicePool.UseSameSoundLimiter = UseSameSoundLimiter;
                }
            }
        }

        private void InitTransitionCues()
        {
            m_musicTransitionCues = new Dictionary<MyStringId, Dictionary<MyStringId, MyCueId>>(MyStringId.Comparer);
            m_musicTracks = new Dictionary<MyStringId, List<MyCueId>>(MyStringId.Comparer);
        }

        private int GetNumberOfSounds()
        {
            return m_cues.Count;
        }

        private void AddMusicCue(MyStringId musicTransition, MyStringId category, MyCueId cueId)
        {
            if (!m_musicTransitionCues.ContainsKey(musicTransition))
                m_musicTransitionCues[musicTransition] = new Dictionary<MyStringId, MyCueId>(MyStringId.Comparer);
            if (m_musicTransitionCues[musicTransition].ContainsKey(category) == false)
                m_musicTransitionCues[musicTransition].Add(category, cueId);

            if (m_musicTracks.ContainsKey(category) == false)
                m_musicTracks.Add(category, new List<MyCueId>());
            m_musicTracks[category].Add(cueId);
        }

        public Dictionary<MyStringId, List<MyCueId>> GetMusicCues()
        {
            return m_musicTracks;
        }

        public void Update()
        {
            foreach (var voicePool in m_voicePools)
            {
                voicePool.Value.Update();
            }
        }

        public void ClearSounds()
        {
            foreach (var vp in m_voicePools)
            {
                // eventual stopping of playing
                vp.Value.StopAll();
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
            if (m_reverb != null)
                m_reverb.Dispose();
            m_reverb = null;
            foreach (var vp in m_voicePools)
            {
                // eventual stopping of playing
                vp.Value.StopAll();
            }
            foreach(var vp in m_voicePools)
            {
                vp.Value.Dispose();
            }
            m_voicePools.Clear();
            m_cues.Clear();
        }

        public bool ApplyReverb
        {
            get { return m_applyReverb; }
            set { m_applyReverb = value; }
        }

        public MyStringId GetRandomTransitionEnum()
        {
            return m_musicTransitionCues.Keys.ElementAt(MyUtils.GetRandomInt(m_musicTransitionCues.Count));
        }

        public MyStringId GetRandomTransitionCategory(ref MyStringId transitionEnum, ref MyStringId noRandom)
        {
            if (m_musicTransitionCues.ContainsKey(transitionEnum) == false)
            {
                do {
                    transitionEnum = GetRandomTransitionEnum();
                } while (transitionEnum == noRandom && m_musicTransitionCues.Count > 1);
            }
            int randomIndex = MyUtils.GetRandomInt(m_musicTransitionCues[transitionEnum].Count);
            int currentIndex = 0;
            foreach (var categoryCueKVP in m_musicTransitionCues[transitionEnum])
            {
                if (currentIndex == randomIndex)
                {
                    return categoryCueKVP.Key;
                }
                currentIndex++;
            }
            throw new InvalidBranchException();
        }

        public bool IsValidTransitionCategory(MyStringId transitionEnum, MyStringId category)
        {
            return m_musicTransitionCues.ContainsKey(transitionEnum) && (category == MyStringId.NullOrEmpty || m_musicTransitionCues[transitionEnum].ContainsKey(category));
        }

        public MyCueId GetTransitionCue(MyStringId transitionEnum, MyStringId category)
        {
            return m_musicTransitionCues[transitionEnum][category];
        }

        public MySoundData GetCue(MyCueId cueId)
        {
            //Debug.Assert(m_cues.ContainsKey(cue));
            if (!m_cues.ContainsKey(cueId) && cueId.Hash != MyStringHash.NullOrEmpty)
                MyLog.Default.WriteLine("Cue was not found: " + cueId, LoggingOptions.AUDIO);
            MySoundData result = null;
            m_cues.TryGetValue(cueId, out result);
            return result;
        }

        public Dictionary<MyCueId, MySoundData>.ValueCollection CueDefinitions { get { return m_cues.Values; } }

        public List<MyStringId> GetCategories()
        {
            return m_categories;
        }

        internal MyInMemoryWave GetRandomWave(MySoundData cue, MySoundDimensions type, out int waveNumber, out CuePart part, int tryIgnoreWaveNumber = -1)
        {
            int counter = 0;
            foreach (var w in cue.Waves)
                if (w.Type == type)
                    counter++;
            waveNumber = MyUtils.GetRandomInt(counter);
			if (counter > 2 && waveNumber == tryIgnoreWaveNumber)
				waveNumber = (waveNumber+1) % (counter);	// TODO: Do this better
            var wave = GetWave(cue, type, waveNumber, CuePart.Start);
            if (wave != null)
                part = CuePart.Start;
            else
            {
                wave = GetWave(cue, type, waveNumber, CuePart.Loop);
                part = CuePart.Loop;
            }
            return wave;
        }

        internal MyInMemoryWave GetWave(MySoundData cue, MySoundDimensions dim, int waveNumber, CuePart cuePart)
        {
            if (m_waveBank == null)
                return null;
            foreach (var wave in cue.Waves)
                if (wave.Type == dim)
                {
                    if (waveNumber == 0)
                        switch (cuePart)
                        {
                            case CuePart.Start:
                                return cue.StreamSound ? m_waveBank.GetStreamedWave(wave.Start, cue, dim) : m_waveBank.GetWave(wave.Start);
                            case CuePart.Loop:
                                return cue.StreamSound ? m_waveBank.GetStreamedWave(wave.Loop, cue, dim) : m_waveBank.GetWave(wave.Loop);
                            case CuePart.End:
                                return cue.StreamSound ? m_waveBank.GetStreamedWave(wave.End, cue, dim) : m_waveBank.GetWave(wave.End);
                        }
                    waveNumber--;
                }
            return null;
        }

        internal MySourceVoice GetVoice(MyCueId cueId, MyInMemoryWave wave, CuePart part = CuePart.Start)
        {
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
            voice.Flush();
            voice.SubmitSourceBuffer(cueId, wave, part);
            voice.Voice.SetEffectChain(null);
            return voice;
        }

#if DEBUG
        private static void AddVoiceForDebug(MySourceVoice voice)
        {
            StringBuilder v = new StringBuilder(voice.CueEnum.ToString());
            if (lastSounds.Count < LAST_SOUND_COUNT)
            {
                lastSounds.Add(v);
            }
            else
            {
                lastSounds[lastSoundIndex] = v;
            }
            lastSoundIndex++;
            if (lastSoundIndex >= LAST_SOUND_COUNT)
                lastSoundIndex = 0;
        }
#endif

        internal MySourceVoice GetVoice(MyCueId cueId, out int waveNumber, MySoundDimensions type = MySoundDimensions.D2, int tryIgnoreWaveNumber = -1)
        {
			waveNumber = -1;
            MySoundData cue = GetCue(cueId);
            if ((cue == null) || (cue.Waves == null) || (cue.Waves.Count == 0))
                return null;

            CuePart part;
            MyInMemoryWave wave = GetRandomWave(cue, type, out waveNumber, out part, tryIgnoreWaveNumber);
            if (wave == null && type == MySoundDimensions.D2)
            {
                type = MySoundDimensions.D3;
                wave = GetRandomWave(cue, type, out waveNumber, out part, tryIgnoreWaveNumber);
            }
            if (wave == null)
                return null;

            MySourceVoice voice = GetVoice(cueId, wave, part);
            if (voice == null)
                return null;

            if (cue.Loopable)
            {
                wave = GetWave(cue, type, waveNumber, CuePart.Loop);
                if (wave != null)
                {
                    Debug.Assert(voice.Owner.WaveFormat.Encoding == wave.WaveFormat.Encoding);
                    if (voice.Owner.WaveFormat.Encoding == wave.WaveFormat.Encoding)
                        voice.SubmitSourceBuffer(cueId, wave, CuePart.Loop);
                    else
                        MyLog.Default.WriteLine(string.Format("Inconsistent encodings: '{0}', got '{1}', expected '{2}', part = '{3}'", cueId, wave.WaveFormat.Encoding, voice.Owner.WaveFormat.Encoding, CuePart.Loop));
                }
                wave = GetWave(cue, type, waveNumber, CuePart.End);
                if (wave != null)
                {
                    Debug.Assert(voice.Owner.WaveFormat.Encoding == wave.WaveFormat.Encoding);
                    if (voice.Owner.WaveFormat.Encoding == wave.WaveFormat.Encoding)
                        voice.SubmitSourceBuffer(cueId, wave, CuePart.End);
                    else
                        MyLog.Default.WriteLine(string.Format("Inconsistent encodings: '{0}', got '{1}', expected '{2}', part = '{3}'", cueId, wave.WaveFormat.Encoding, voice.Owner.WaveFormat.Encoding, CuePart.End));
                }
            }

#if DEBUG
            if (voice.CueEnum.IsNull == false)
                AddVoiceForDebug(voice);
#endif

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
            stringBuilder.AppendLine("");
            stringBuilder.Append("Not playing: ");
            foreach (var voicePool in m_voicePools)
            {
                voicePool.Value.WritePausedDebugInfo(stringBuilder);
            }
        }
    }
}
