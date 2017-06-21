//#define DEBUG_AUDIO

using SharpDX.Multimedia;
using SharpDX.XAudio2;
using SharpDX.XAudio2.Fx;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using VRage.Audio.X3DAudio;
using VRage.Collections;
using VRage.Data.Audio;
using VRage.FileSystem;
using VRage.Trace;
using VRage.Utils;
using VRageMath;

namespace VRage.Audio
{
    public class MyXAudio2 : IMyAudio
    {
        VoiceSendDescriptor[] m_gameAudioVoiceDesc;
        VoiceSendDescriptor[] m_musicAudioVoiceDesc;
        VoiceSendDescriptor[] m_hudAudioVoiceDesc;

        MyAudioInitParams m_initParams;

        XAudio2 m_audioEngine;
        DeviceDetails m_deviceDetails;
        MasteringVoice m_masterVoice;
        SubmixVoice m_gameAudioVoice;
        SubmixVoice m_musicAudioVoice;
        SubmixVoice m_hudAudioVoice;

        MyCueBank m_cueBank;
        MyEffectBank m_effectBank;

        MyX3DAudio m_x3dAudio;

        bool m_canPlay;

        float m_volumeHud;
        float m_volumeDefault;
        float m_volumeMusic;
        float m_volumeVoiceChat;

        bool m_mute;
        bool m_musicAllowed;

        bool m_musicOn;
        bool m_gameSoundsOn;

        bool m_voiceChatEnabled;

        MyMusicState m_musicState;
        bool m_loopMusic;

        MySourceVoice m_musicCue;

        CalculateFlags m_calculateFlags;

        //  Music cues
        struct MyMusicTransition
        {
            public int Priority;
            public MyStringId TransitionEnum;
            public MyStringId Category;

            public MyMusicTransition(int priority, MyStringId transitionEnum, MyStringId category)
            {
                Priority = priority;
                TransitionEnum = transitionEnum;
                Category = category;
            }
        }

        SortedList<int, MyMusicTransition> m_nextTransitions = new SortedList<int, MyMusicTransition>();
        MyMusicTransition? m_currentTransition;
        bool m_transitionForward;
        StringBuilder m_currentTransitionDescription = new StringBuilder();

        float m_volumeAtTransitionStart;
        int m_timeFromTransitionStart; // in ms
        const int TRANSITION_TIME = 1000;     // in ms

        Listener m_listener;
        Emitter m_helperEmitter; // The emitter describes an entity which is making a 3D sound.
        List<IMy3DSoundEmitter> m_3Dsounds; // List of currently playing 3D sounds to update
        bool m_canUpdate3dSounds = true;

        //  Number of sound instances (cue) created/added from the application start
        int m_soundInstancesTotal2D;
        int m_soundInstancesTotal3D;

        //  Events
        delegate void VolumeChangeHandler(float newVolume);
        event VolumeChangeHandler OnSetVolumeHud, OnSetVolumeGame, OnSetVolumeMusic;

        //reverb
        private Reverb m_reverb = null;
        private bool m_applyReverb = false;

        //global volume change
        private float m_globalVolumeLevel = 1f;
        private float m_globalVolumeTarget = 1f;
        private float m_globalVolumeIncrement = 1f;
        private bool m_globalVolumeRaising = true;
        private bool m_globalVolumeChanging = false;


        Dictionary<MyCueId, MySoundData>.ValueCollection IMyAudio.CueDefinitions { get { return m_canPlay ? m_cueBank.CueDefinitions : null; } }
        List<MyStringId> IMyAudio.GetCategories() { return m_canPlay ? m_cueBank.GetCategories() : null; }
        MySoundData IMyAudio.GetCue(MyCueId cueId) { return m_canPlay ? m_cueBank.GetCue(cueId) : null; }
        Dictionary<MyStringId, List<MyCueId>> IMyAudio.GetAllMusicCues() { return m_cueBank != null ? m_cueBank.GetMusicCues() : null; }

        int IMyAudio.SampleRate { get { return m_masterVoice != null ? m_masterVoice.VoiceDetails.InputSampleRate : 0; } }

        public MySoundData SoloCue { get; set; }
        public bool GameSoundIsPaused { get; private set; }
        private bool m_useVolumeLimiter = false;
        private bool m_useSameSoundLimiter = false;
        private bool m_enableReverb = false;
        private bool m_reverbSet = false;
        private bool m_soundLimiterReady = false;
        private bool m_soundLimiterSet = false;
        bool IMyAudio.UseVolumeLimiter
        { 
            get
            {
                return m_useVolumeLimiter;
            }
            set
            {
                m_useVolumeLimiter = value;
            }
        }
        bool IMyAudio.UseSameSoundLimiter
        {
            get
            {
                return m_useSameSoundLimiter;
            }
            set
            {
                m_useSameSoundLimiter = value;
            }
        }
        bool IMyAudio.EnableReverb
        {
            get
            {
                return m_enableReverb;
            }
            set
            {
                m_enableReverb = value;
                if (!m_reverbSet && value && m_masterVoice != null && m_masterVoice.VoiceDetails.InputSampleRate <= MyAudio.MAX_SAMPLE_RATE)
                {
                    m_reverb = new Reverb(m_audioEngine);
                    EffectDescriptor descriptor = new EffectDescriptor(m_reverb, m_masterVoice.VoiceDetails.InputChannelCount);
                    m_gameAudioVoice.SetEffectChain(descriptor);
                    m_gameAudioVoice.DisableEffect(0);
                    m_reverbSet = true;
                }
            }
        }

        volatile bool m_deviceLost = false;
        int m_lastDeviceCount = 0;
        private ListReader<MySoundData> m_sounds;
        private ListReader<MyAudioEffect> m_effects;
        private int m_deviceNumber;

        private void Init()
        {
            StartEngine();
            CreateX3DAudio();
        }

        private void StartEngine()
        {
            if (m_audioEngine != null)
            {
                DisposeVoices();
                m_audioEngine.CriticalError -= m_audioEngine_CriticalError;
                m_audioEngine.Dispose();
            }

            // Init/reinit engine
            m_audioEngine = new XAudio2(XAudio2Version.Version27);

            // A way to disable SharpDX callbacks
            //var meth = m_audioEngine.GetType().GetMethod("UnregisterForCallbacks_", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            //var callbacks = m_audioEngine.GetType().GetField("engineShadowPtr", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            //meth.Invoke((object)m_audioEngine, new object[] { callbacks.GetValue(m_audioEngine) });

            m_audioEngine.CriticalError += m_audioEngine_CriticalError;
            m_lastDeviceCount = m_audioEngine.DeviceCount;


            m_deviceNumber = 0;
            while (true) //find first non com device
            {
                try
                {
                    m_deviceDetails = m_audioEngine.GetDeviceDetails(m_deviceNumber);
                    if (m_deviceDetails.Role == DeviceRole.DefaultCommunicationsDevice)
                    {
                        m_deviceNumber++;
                        if (m_deviceNumber == m_audioEngine.DeviceCount)
                        {
                            m_deviceNumber--;
                            break;
                        }
                    }
                    else
                        break;
                }
                catch(Exception e)
                {
                    MyLog.Default.WriteLine(string.Format("Failed to get device details.\n\tdevice no.: {0}\n\tdevice count: {1}",m_deviceNumber, m_audioEngine.DeviceCount),LoggingOptions.AUDIO);
                    MyLog.Default.WriteLine(e.ToString());
                    m_deviceNumber = 0;
                    m_deviceDetails = m_audioEngine.GetDeviceDetails(m_deviceNumber);
                    break;
                }
            }

            m_masterVoice = new MasteringVoice(m_audioEngine, XAudio2.DefaultChannels, XAudio2.DefaultSampleRate, m_deviceNumber);
            
            if (m_useVolumeLimiter)
            {
                var limiter = new SharpDX.XAPO.Fx.MasteringLimiter(m_audioEngine);
                var param = limiter.Parameter;
                param.Loudness = 0;
                limiter.Parameter = param;
                //TODO: this throws exception in 3.0.1 version
                var effectDescriptor = new EffectDescriptor(limiter);
                m_masterVoice.SetEffectChain(effectDescriptor);
                m_soundLimiterReady = true;
                m_masterVoice.DisableEffect(0);
                //m_masterVoice.EnableEffect(0);
                //limiter.Dispose();
            }

            m_calculateFlags = CalculateFlags.Matrix | CalculateFlags.Doppler;
            if ((m_deviceDetails.OutputFormat.ChannelMask & Speakers.LowFrequency) != 0)
            {
                m_calculateFlags |= CalculateFlags.RedirectToLfe;
            }

			var masterDetails = m_masterVoice.VoiceDetails;
            m_gameAudioVoice = new SubmixVoice(m_audioEngine, masterDetails.InputChannelCount, masterDetails.InputSampleRate);
            m_musicAudioVoice = new SubmixVoice(m_audioEngine, masterDetails.InputChannelCount, masterDetails.InputSampleRate);
            m_hudAudioVoice = new SubmixVoice(m_audioEngine, masterDetails.InputChannelCount, masterDetails.InputSampleRate);
            m_gameAudioVoiceDesc = new VoiceSendDescriptor[] { new VoiceSendDescriptor(m_gameAudioVoice) };
            m_musicAudioVoiceDesc = new VoiceSendDescriptor[] { new VoiceSendDescriptor(m_musicAudioVoice) };
            m_hudAudioVoiceDesc = new VoiceSendDescriptor[] { new VoiceSendDescriptor(m_hudAudioVoice) };

            if (m_mute)
            { // keep sounds muted 
                m_gameAudioVoice.SetVolume(0);
                m_musicAudioVoice.SetVolume(0);
            }
        }

        public void SetReverbParameters(float diffusion, float roomSize)
        {
            /*ReverbParameters newParameters = new ReverbParameters();
            newParameters.Diffusion = MyMath.Clamp(diffusion, Reverb.MinimumDiffusion, Reverb.MaximumDiffusion);
            newParameters.RoomSize = MyMath.Clamp(roomSize, Reverb.MinimumRoomsize, Reverb.MaximumRoomsize);
            m_reverb.Parameter = newParameters;*/
        }

        public void ChangeGlobalVolume(float level, float time)
        {
            level = MyMath.Clamp(level, 0, 1f);
            m_globalVolumeChanging = false;
            if (level != m_globalVolumeLevel)
            {
                if (time <= 0f)
                {
                    m_globalVolumeLevel = level;
                    if (m_musicAudioVoice != null && !m_musicAudioVoice.IsDisposed)
                        m_musicAudioVoice.SetVolume(m_volumeMusic * level);
                    if (m_hudAudioVoice != null && !m_hudAudioVoice.IsDisposed)
                        m_hudAudioVoice.SetVolume(m_volumeHud * level);
                    if (m_gameAudioVoice != null && !m_gameAudioVoice.IsDisposed)
                        m_gameAudioVoice.SetVolume(m_volumeDefault * level);
                }
                else
                {
                    m_globalVolumeChanging = true;
                    m_globalVolumeIncrement = ((level - m_globalVolumeLevel) / 60f) / time;
                    m_globalVolumeTarget = level;
                    m_globalVolumeRaising = level > m_globalVolumeLevel;
                }
            }
        }

        private void GlobalVolumeUpdate()
        {
            m_globalVolumeLevel += m_globalVolumeIncrement;
            if((m_globalVolumeRaising && m_globalVolumeLevel >= m_globalVolumeTarget) || (!m_globalVolumeRaising && m_globalVolumeLevel <= m_globalVolumeTarget))
            {
                m_globalVolumeLevel = m_globalVolumeTarget;
                m_globalVolumeChanging = false;
            }
            if (m_musicAudioVoice != null)
                m_musicAudioVoice.SetVolume(m_volumeMusic * m_globalVolumeLevel);
            if (m_hudAudioVoice != null)
                m_hudAudioVoice.SetVolume(m_volumeHud * m_globalVolumeLevel);
            if (m_gameAudioVoice != null)
                m_gameAudioVoice.SetVolume(m_volumeDefault * m_globalVolumeLevel);
        }

        public void EnableMasterLimiter(bool enable)
        {
            if (m_useVolumeLimiter && m_soundLimiterReady && enable != m_soundLimiterSet)
            {
                if (enable)
                    m_masterVoice.EnableEffect(0);
                else
                    m_masterVoice.DisableEffect(0);
                m_soundLimiterSet = enable;
            }
        }

        void m_audioEngine_CriticalError(object sender, SharpDX.XAudio2.ErrorEventArgs e)
        {
            const uint LEAP_E_INVALID_CALL = 0x88880001;

            if (((uint)e.ErrorCode.Code) == LEAP_E_INVALID_CALL)
            {
                MyLog.Default.WriteLine("Audio device removed");
            }
            else
            {
                MyLog.Default.WriteLine("Audio error: " + e.ErrorCode);
            }
            m_deviceLost = true;
        }

        private void CreateX3DAudio()
        {
            if (m_audioEngine == null)
                return;

            m_x3dAudio = new MyX3DAudio(m_deviceDetails.OutputFormat.ChannelMask);

            MyLog.Default.WriteLine(string.Format("MyAudio.CreateX3DAudio - Device: {0} - Channel #: {1}", m_deviceDetails.DisplayName, m_deviceDetails.OutputFormat.Channels));
        }

        private void DisposeVoices()
        {
            if (m_hudAudioVoice != null)
                m_hudAudioVoice.Dispose();

            if (m_musicAudioVoice != null)
                m_musicAudioVoice.Dispose();

            if (m_gameAudioVoice != null)
                m_gameAudioVoice.Dispose();

            if (m_masterVoice != null)
                m_masterVoice.Dispose();
        }

        private void CheckIfDeviceChanged()
        {
            // DeviceCount cannot be called on XAudio >= 2.8 (Windows 8)
            // Maybe call DeviceCount with XAudio 2.7 and use different API for windows 8
            // http://blogs.msdn.com/b/chuckw/archive/2012/04/02/xaudio2-and-windows-8-consumer-preview.aspx
            // DeviceCount is very slow even on Windows 7 sometimes, don't know why
            if (m_deviceLost)// || m_lastDeviceCount != m_audioEngine.DeviceCount)
            {
                m_deviceLost = false;

                try
                {
                    Init();
                }
                catch (Exception ex)
                {
                    MyLog.Default.WriteLine("Exception during loading audio engine. Game continues, but without sound. Details: " + ex.ToString(), LoggingOptions.AUDIO);
                    MyLog.Default.WriteLine("Device ID: " + m_deviceDetails.DeviceID, LoggingOptions.AUDIO);
                    MyLog.Default.WriteLine("Device name: " + m_deviceDetails.DisplayName, LoggingOptions.AUDIO);
                    MyLog.Default.WriteLine("Device role: " + m_deviceDetails.Role, LoggingOptions.AUDIO);
                    MyLog.Default.WriteLine("Output format: " + m_deviceDetails.OutputFormat, LoggingOptions.AUDIO);

                    //  This exception is the only way I can know if we can play sound (e.g. if computer doesn't have sound card).
                    //  I didn't find other ways of checking it.
                    m_canPlay = false;
                }

                if (m_initParams.SimulateNoSoundCard)
                    m_canPlay = false;

                if (m_canPlay)
                {
                    if (m_cueBank != null)
                        m_cueBank.SetAudioEngine(m_audioEngine);
                    m_gameAudioVoice.SetVolume(m_volumeDefault * m_globalVolumeLevel);
                    m_hudAudioVoice.SetVolume(m_volumeHud * m_globalVolumeLevel);
                    m_musicAudioVoice.SetVolume(m_volumeMusic * m_globalVolumeLevel);
                    //TODO: JN reinit sounds so they play
                    m_3Dsounds.Clear();

                    if ((m_musicCue != null) && m_musicCue.IsPlaying)
                    {
                        // restarts music cue
                        m_musicCue = PlaySound(m_musicCue.CueEnum);
                        if (m_musicCue != null)
                            m_musicCue.SetOutputVoices(m_musicAudioVoiceDesc);

                        UpdateMusic(0);
                    }
                }
            }
        }


        public void LoadData(MyAudioInitParams initParams, ListReader<MySoundData> sounds, ListReader<MyAudioEffect> effects)
        {
            MyLog.Default.WriteLine("MyAudio.LoadData - START");
            MyLog.Default.IncreaseIndent();
            m_initParams = initParams;
            m_sounds = sounds;
            m_effects = effects;
            m_canPlay = true;
            try
            {
                // has to be called, as others can access the initialized members
                Init();
            }
            catch (Exception ex)
            {
                MyLog.Default.WriteLine("Exception during loading audio engine. Game continues, but without sound. Details: " + ex.ToString(), LoggingOptions.AUDIO);
                MyLog.Default.WriteLine("Device ID: " + m_deviceDetails.DeviceID, LoggingOptions.AUDIO);
                MyLog.Default.WriteLine("Device name: " + m_deviceDetails.DisplayName, LoggingOptions.AUDIO);
                MyLog.Default.WriteLine("Device role: " + m_deviceDetails.Role, LoggingOptions.AUDIO);
                MyLog.Default.WriteLine("Output format: " + m_deviceDetails.OutputFormat, LoggingOptions.AUDIO);

                //  This exception is the only way I can know if we can play sound (e.g. if computer doesn't have sound card).
                //  I didn't find other ways of checking it.
                m_canPlay = false;
            }

            if (m_initParams.SimulateNoSoundCard)
                m_canPlay = false;

            if (m_canPlay)
            {
                m_cueBank = new MyCueBank(m_audioEngine, sounds);
                m_cueBank.UseSameSoundLimiter = m_useSameSoundLimiter;
                m_cueBank.SetSameSoundLimiter();
                m_cueBank.DisablePooling = initParams.DisablePooling;
                m_effectBank = new MyEffectBank(effects, m_audioEngine);
                m_3Dsounds = new List<IMy3DSoundEmitter>();
                m_listener = new Listener();
                m_listener.SetDefaultValues();
                m_helperEmitter = new Emitter();
                m_helperEmitter.SetDefaultValues();

                m_musicOn = true;
                m_gameSoundsOn = true;
           
                m_musicAllowed = true;

                if ((m_musicCue != null) && m_musicCue.IsPlaying)
                {
                    // restarts music cue
                    m_musicCue = PlaySound(m_musicCue.CueEnum);
                    if (m_musicCue != null)
                    {
                        m_musicCue.SetOutputVoices(m_musicAudioVoiceDesc);
                        m_musicAudioVoice.SetVolume(m_volumeMusic * m_globalVolumeLevel);
                    }

                    UpdateMusic(0);
                }
                else
                {
                    m_musicState = MyMusicState.Stopped;
                }
                m_loopMusic = true;

                m_transitionForward = false;
                m_timeFromTransitionStart = 0;

                m_soundInstancesTotal2D = 0;
                m_soundInstancesTotal3D = 0;
            }

            MyLog.Default.DecreaseIndent();
            MyLog.Default.WriteLine("MyAudio.LoadData - END");
        }

        public void SetSameSoundLimiter()
        {
            if (m_cueBank != null)
            {
                m_cueBank.UseSameSoundLimiter = m_useSameSoundLimiter;
                m_cueBank.SetSameSoundLimiter();
            }
        }

        public void UnloadData()
        {
            MyLog.Default.WriteLine("MyAudio.UnloadData - START");

            if (m_3Dsounds != null)
                m_3Dsounds.Clear();

            if (m_canPlay)
            {
                m_audioEngine.StopEngine();
                if (m_cueBank != null)
                    m_cueBank.Dispose();
            }

            SoloCue = null;

            DisposeVoices();
            //Debug.Assert(m_musicCue == null || m_musicCue.Voice == null || m_musicCue.Voice.IsDisposed);
            if (m_audioEngine != null)
            {
                m_audioEngine.CriticalError -= m_audioEngine_CriticalError;
                m_audioEngine.Dispose();
                m_audioEngine = null;
            }

            m_canPlay = false;
            m_reverbSet = false;

            MyLog.Default.WriteLine("MyAudio.UnloadData - END");
        }

        public void ClearSounds()
        {
            if (m_cueBank != null)
                m_cueBank.ClearSounds();
        }

        public void ReloadData()
        {
            UnloadData();
            LoadData(m_initParams, m_sounds, m_effects);
        }

        public void ReloadData(ListReader<MySoundData> sounds, ListReader<MyAudioEffect> effects)
        {
            UnloadData();
            LoadData(m_initParams, sounds, effects);
        }

        public bool ApplyReverb
        {
            get
            {
                if (!m_canPlay || !m_enableReverb)
                    return false;

                if (m_cueBank == null)
                    return false;

                if (m_masterVoice.VoiceDetails.InputSampleRate > MyAudio.MAX_SAMPLE_RATE)
                    return false;

                return m_applyReverb;
            }
            set
            {
                if (!m_canPlay || !m_reverbSet)
                    return;

                if (m_cueBank == null)
                    return;

                if (!m_enableReverb && !m_applyReverb)
                    return;

                if (m_gameAudioVoice != null)
                {
                    if (value)
                        m_gameAudioVoice.EnableEffect(0);
                    else
                        m_gameAudioVoice.DisableEffect(0);
                }

                m_cueBank.ApplyReverb = value;
                m_applyReverb = value;
            }
        }

        //  Set/get master volume for all sounds/cues for "Music" category.
        //  Interval <0..1..2>
        //      0.0f  ->   -96 dB (silence) 
        //      1.0f  ->    +0 dB (full pitch as authored) 
        //      2.0f  ->    +6 dB (6 dB greater than authored) 
        public float VolumeMusic
        {
            get
            {
                if (!m_canPlay || !m_musicOn)
                    return 0;

                return m_volumeMusic;
            }

            set
            {
                if (!m_canPlay || !m_musicOn)
                    return;

                //  We need to clamp the volume, because app fails if we set it with zero value
                m_volumeMusic = MathHelper.Clamp(value, MyAudioConstants.MUSIC_MASTER_VOLUME_MIN, MyAudioConstants.MUSIC_MASTER_VOLUME_MAX);
                m_musicAudioVoice.SetVolume(m_volumeMusic * m_globalVolumeLevel);

                if (OnSetVolumeMusic != null)
                    OnSetVolumeMusic(m_volumeMusic);
            }
        }

        //  Set/get master volume for all sounds/cues in "Gui" category.
        //  Interval <0..1..2>
        //      0.0f  ->   -96 dB (silence) 
        //      1.0f  ->    +0 dB (full pitch as authored) 
        //      2.0f  ->    +6 dB (6 dB greater than authored) 
        public float VolumeHud
        {
            get
            {
                if (!m_canPlay)
                    return 0;

                return m_volumeHud;
            }

            set
            {
                if (!m_canPlay)
                    return;

                //  We need to clamp the volume, because app fails if we set it with zero value
                m_volumeHud = MathHelper.Clamp(value, MyAudioConstants.GAME_MASTER_VOLUME_MIN, MyAudioConstants.GAME_MASTER_VOLUME_MAX);
                m_hudAudioVoice.SetVolume(m_volumeHud * m_globalVolumeLevel);

                if (OnSetVolumeHud != null)
                    OnSetVolumeHud(m_volumeHud);
            }
        }

        //  Set/get master volume for all in-game sounds/cues.
        //  Interval <0..1..2>
        //      0.0f  ->   -96 dB (silence) 
        //      1.0f  ->    +0 dB (full pitch as authored) 
        //      2.0f  ->    +6 dB (6 dB greater than authored) 
        public float VolumeGame
        {
            get
            {
                if (!m_canPlay || !m_gameSoundsOn)
                    return 0;

                return m_volumeDefault;
            }

            set
            {
                if (!m_canPlay || !m_gameSoundsOn)
                    return;

                //  We need to clamp the volume, because app fails if we set it with zero value
                m_volumeDefault = MathHelper.Clamp(value, MyAudioConstants.GAME_MASTER_VOLUME_MIN, MyAudioConstants.GAME_MASTER_VOLUME_MAX);
                m_gameAudioVoice.SetVolume(m_volumeDefault * m_globalVolumeLevel);

                if (OnSetVolumeGame != null)
                    OnSetVolumeGame(m_volumeDefault);
            }
        }

        
        public float VolumeVoiceChat
        {
            get
            {
                return m_volumeVoiceChat;
            }
            set
            {
                m_volumeVoiceChat = MathHelper.Clamp(value, MyAudioConstants.VOICE_CHAT_VOLUME_MIN, MyAudioConstants.VOICE_CHAT_VOLUME_MAX);
            }
        }

        public bool EnableVoiceChat 
        { 
            get 
            {
                if (!m_canPlay)
                    return false;
                return m_voiceChatEnabled; 
            } 
            set 
            {
                if (!m_canPlay)
                    return;
                if (m_voiceChatEnabled != value)
                {
                    m_voiceChatEnabled = value;
                    if (VoiceChatEnabled != null)
                        VoiceChatEnabled(m_voiceChatEnabled);
                }
            } 
        }

        public event Action<bool> VoiceChatEnabled;

        public void Pause()
        {
            if (m_canPlay)
                m_audioEngine.StopEngine();
        }

        public void Resume()
        {
            if (m_canPlay)
                m_audioEngine.StartEngine();
        }

        public void PauseGameSounds()
        {
            if (m_canPlay)
            {
                GameSoundIsPaused = true;
                m_gameAudioVoice.SetVolume(0f);
                m_canUpdate3dSounds = false;
                if (m_musicCue != null)
                    m_musicCue.VolumeMultiplier = 0f;
            }
        }

        public void ResumeGameSounds()
        {
            if (m_canPlay)
            {
                GameSoundIsPaused = false;
                if (!Mute)
                    m_gameAudioVoice.SetVolume(m_volumeDefault * m_globalVolumeLevel);

                m_canUpdate3dSounds = true;
                if (m_musicCue != null)
                    m_musicCue.VolumeMultiplier = 1f;
            }
        }

        public bool Mute
        {
            get { return m_mute; }
            set
            {
                if (m_mute != value)
                {
                    m_mute = value;
                    if (m_mute)
                    {
                        if (m_canPlay)
                        {
                            //TODO: temporary hack SOLVE properly, hud sound issue 
                            Thread.Sleep(100);
                            m_gameAudioVoice.SetVolume(0f);
                            m_musicAudioVoice.SetVolume(0f);
                            Thread.Sleep(100);
                        }
                    }
                    else
                    {
                        if (m_canPlay)
                        {
                            if (!GameSoundIsPaused)
                                m_gameAudioVoice.SetVolume(m_volumeDefault * m_globalVolumeLevel);

                            m_musicAudioVoice.SetVolume(m_volumeMusic * m_globalVolumeLevel);
                        }
                    }
                }
            }
        }

        public bool MusicAllowed
        {
            get { return m_musicAllowed; }
            set { m_musicAllowed = value; }
        }

        public bool IsValidTransitionCategory(MyStringId transitionCategory, MyStringId musicCategory)
        {
            return m_canPlay ? m_cueBank.IsValidTransitionCategory(transitionCategory, musicCategory) : false;
        }


        public void PlayMusic(MyMusicTrack? track = null, int priorityForRandom = 0)
        {
            if (!m_canPlay || !m_musicAllowed)
                return;
            Mute = false;
            bool playRandom = false;
            if (track.HasValue)
            {
                if (HasAnyTransition())
                    m_nextTransitions.Clear();
                if(!m_cueBank.IsValidTransitionCategory(track.Value.TransitionCategory, track.Value.MusicCategory))
                    playRandom = true;
                else
                    ApplyTransition(track.Value.TransitionCategory, 1, track.Value.MusicCategory, false);
            }
            else if ((m_musicState == MyMusicState.Stopped) && /*(GetMusicCue() == null) &&*/ !HasAnyTransition())
            {
                playRandom = true;
            }
            if (playRandom)
            {
                var transition = GetRandomTransitionEnum();
                if (transition.HasValue)
                    ApplyTransition(transition.Value, priorityForRandom, null, false);
            }
        }

        public IMySourceVoice PlayMusicCue(MyCueId musicCue, bool overrideMusicAllowed = false)
        {
            if (!m_canPlay || (!m_musicAllowed && !overrideMusicAllowed))
                return null;
            Mute = false;
            m_musicCue = PlaySound(musicCue);
            if (m_musicCue != null)
            {
                m_musicCue.SetOutputVoices(m_musicAudioVoiceDesc);
                m_musicAudioVoice.SetVolume(m_volumeMusic * m_globalVolumeLevel);
            }
            return m_musicCue;
        }

        public void StopMusic()
        {
            m_currentTransition = null;
            m_nextTransitions.Clear();
            m_musicState = MyMusicState.Stopped;
            if (m_musicCue != null)
            {
                try
                {
                    m_musicCue.Stop();
                }
                catch (Exception e)
                {
                    MyLog.Default.WriteLine(e);
                    if (m_audioEngine == null || m_audioEngine.IsDisposed)
                    {
                        MyLog.Default.WriteLine("Audio engine disposed!", LoggingOptions.AUDIO);
                    }
                }
            }
        }

        public void MuteHud(bool mute)
        {
            if (m_canPlay)
                m_hudAudioVoice.SetVolume(mute ? 0f : m_volumeHud * m_globalVolumeLevel);
        }

        public bool HasAnyTransition()
        {
            return m_nextTransitions.Count > 0;
        }

        internal static string AudioPath
        {
            get { return Path.Combine(MyFileSystem.ContentPath, "Audio"); }
        }

        //  Updates the state of music and 3D audio system.
        public void Update(int stepSizeInMS, Vector3 listenerPosition, Vector3 listenerUp, Vector3 listenerFront)
        {
            if (m_canPlay)
                CheckIfDeviceChanged();

            if (Mute)
                return;

            if (m_canPlay && (m_cueBank != null))
                m_cueBank.Update();

            if (m_canPlay)
            {
                m_listener.Position = new SharpDX.Vector3(listenerPosition.X, listenerPosition.Y, listenerPosition.Z);
                m_listener.OrientTop = new SharpDX.Vector3(listenerUp.X, listenerUp.Y, listenerUp.Z);
                m_listener.OrientFront = new SharpDX.Vector3(listenerFront.X, listenerFront.Y, listenerFront.Z);

                UpdateMusic(stepSizeInMS);
                Update3DCuesPositions();
                m_effectBank.Update(stepSizeInMS);
                if (m_globalVolumeChanging)
                    GlobalVolumeUpdate();
            }
        }

        private void UpdateMusic(int stepSizeinMS)
        {
            if (m_musicState == MyMusicState.Transition)
            {
                m_timeFromTransitionStart += stepSizeinMS;
                // if transition time elapsed, we stop actual playing cue and set music state to stopped
                if (m_timeFromTransitionStart >= TRANSITION_TIME)
                {
                    m_musicState = MyMusicState.Stopped;
                    if ((m_musicCue != null) && m_musicCue.IsPlaying)
                    {
                        m_musicCue.Stop(true);
                        m_musicCue = null;
                    }
                }
                // we decrease music volume (because transition effect)
                else if ((m_musicCue != null) && m_musicCue.IsPlaying)
                {
                    if ((m_musicAudioVoice.Volume > 0f) && m_musicOn)
                        m_musicAudioVoice.SetVolume((1f - (float)m_timeFromTransitionStart / TRANSITION_TIME) * m_volumeAtTransitionStart * m_globalVolumeLevel);
                }
            }

            if (m_musicState == MyMusicState.Stopped)
            {
                MyMusicTransition? nextTransition = GetNextTransition();
                // we save current transition as next transition, if we want apply transition with higher priority, so after new transition stop, then this old transition return back
                if ((m_currentTransition != null) && (m_nextTransitions.Count > 0) && (nextTransition != null) && (nextTransition.Value.Priority > m_currentTransition.Value.Priority))
                    m_nextTransitions[m_currentTransition.Value.Priority] = m_currentTransition.Value;

                m_currentTransition = nextTransition;
                // it there is current transition to play, we play it and set state to playing
                if (m_currentTransition != null)
                {
                    m_musicAudioVoice.SetVolume(m_volumeMusic * m_globalVolumeLevel);
                    PlayMusicByTransition(m_currentTransition.Value);
                    m_nextTransitions.Remove(m_currentTransition.Value.Priority);
                    m_musicState = MyMusicState.Playing;
                }
            }

            if (m_musicState == MyMusicState.Playing)
            {
                if ((m_musicCue == null) || !m_musicCue.IsPlaying)
                {
                    if (m_loopMusic && m_currentTransition != null)
                    {
                        // we play current transition in loop
                        Debug.Assert(m_currentTransition != null);
                        PlayMusicByTransition(m_currentTransition.Value);
                    }
                    else
                    {
                        // switches to another, random, track
                        m_currentTransition = null;
                        //MyStringId? newTransitionEnum = GetRandomTransitionEnum();
                        MyStringId? newTransitionEnum = MyStringId.GetOrCompute("Default");
                        if (newTransitionEnum.HasValue)
                            ApplyTransition(newTransitionEnum.Value, 0, null, false);
                    }
                }
            }
        }

        private static MyStringId NO_RANDOM = MyStringId.GetOrCompute("NoRandom");
        internal MyStringId? GetRandomTransitionEnum()
        {
            if (m_cueBank == null)
                return null;

            var music = m_cueBank.GetRandomTransitionEnum();

            //TODO: Make flag for this
            while (music == NO_RANDOM)
            {
                music = m_cueBank.GetRandomTransitionEnum();
            }

            return music;
        }

        public bool ApplyTransition(MyStringId transitionEnum, int priority = 0, MyStringId? category = null, bool loop = true)
        {
            if (!m_canPlay)
                return false;

            if (!m_musicAllowed)
                return false;

            Debug.Assert(priority >= 0);
            if (category.HasValue)
            {
                if (category.Value == MyStringId.NullOrEmpty)
                    category = null;
                else if (!m_cueBank.IsValidTransitionCategory(transitionEnum, category.Value))
                {
                    Debug.Fail("This category doesn't exist for this transition!");
                    MyLog.Default.WriteLine(string.Format("Category {0} doesn't exist for this transition!", category));
                    return false;
                }
            }

            // if we try apply same transition and priority and category
            if ((m_currentTransition != null) &&
                (m_currentTransition.Value.Priority == priority) &&
                (m_currentTransition.Value.TransitionEnum == transitionEnum) &&
                ((category == null) || (m_currentTransition.Value.Category == category)))
            {
                if ((m_musicState == MyMusicState.Transition) && !m_transitionForward)
                {
                    m_musicState = MyMusicState.Playing;
                    return true;
                }
                else
                    return false;
            }

            // if category not set, we take random category from transition cues
            MyStringId transitionCategory = category ?? m_cueBank.GetRandomTransitionCategory(ref transitionEnum, ref NO_RANDOM);
            // we set this transition as next
            m_nextTransitions[priority] = new MyMusicTransition(priority, transitionEnum, transitionCategory);

            // if new transition has lower priority then current, we don't want apply new transition now
            if ((m_currentTransition != null) && (m_currentTransition.Value.Priority > priority))
                return false;

            m_loopMusic = loop;

            if (m_musicState == MyMusicState.Playing)
                StartTransition(true);
            else if (m_musicState == MyMusicState.Transition)
            {
            }
            else if (m_musicState == MyMusicState.Stopped)
            {
            }
            else
                throw new InvalidBranchException();

            return true;
        }

        private MyMusicTransition? GetNextTransition()
        {
            if (m_nextTransitions.Count > 0)
                return m_nextTransitions[m_nextTransitions.Keys[m_nextTransitions.Keys.Count - 1]];
            else
                return null;
        }

        private void StartTransition(bool forward)
        {
            m_transitionForward = forward;
            m_musicState = MyMusicState.Transition;
            m_timeFromTransitionStart = 0;
            m_volumeAtTransitionStart = m_volumeMusic;
        }

        internal void StopTransition(int priority)
        {
            Debug.Assert(priority >= 0);
            // try removes transition with this priority from next transitions queue
            if (m_nextTransitions.ContainsKey(priority))
                m_nextTransitions.Remove(priority);

            // if we actually play current transition with this priorty, we start transition to next (decreasing volume and switch to another)
            if ((m_currentTransition != null) && (priority == m_currentTransition.Value.Priority))
            {
                if (m_musicState != MyMusicState.Transition)
                    StartTransition(false);
            }
        }

        private void PlayMusicByTransition(MyMusicTransition transition)
        {
            if (m_cueBank != null && m_musicAllowed)
            {
                m_musicCue = PlaySound(m_cueBank.GetTransitionCue(transition.TransitionEnum, transition.Category));
                if (m_musicCue != null)
                {
                    m_musicCue.SetOutputVoices(m_musicAudioVoiceDesc);
                    m_musicAudioVoice.SetVolume(m_volumeMusic * m_globalVolumeLevel);
                }
            }
        }

        private float VolumeVariation(MySoundData cue)
        {
            return MyUtils.GetRandomFloat(-1f, 1f) * cue.VolumeVariation * 0.07f;
        }

        private float PitchVariation(MySoundData cue)
        {
            return MyUtils.GetRandomFloat(-1f, 1f) * cue.PitchVariation / 100f;
        }

        public float SemitonesToFrequencyRatio(float semitones)
        {
            return XAudio2.SemitonesToFrequencyRatio(semitones);
        }

        private void Add3DCueToUpdateList(IMy3DSoundEmitter source)
        {
            if(!m_3Dsounds.Contains(source))
                m_3Dsounds.Add(source);
        }

        public int GetUpdating3DSoundsCount()
        {
            return (m_3Dsounds != null) ? m_3Dsounds.Count : 0;
        }

        public int GetSoundInstancesTotal2D()
        {
            return m_soundInstancesTotal2D;
        }

        public int GetSoundInstancesTotal3D()
        {
            return m_soundInstancesTotal3D;
        }

        private void Update3DCuesState(bool updatePosition = false)
        {
            if (!m_canUpdate3dSounds)
                return;

            if (m_3Dsounds == null)
                return;

            int counter = 0;
            lock (m_3Dsounds)
            {
                while (counter < m_3Dsounds.Count)
                {
                    if (m_3Dsounds[counter].Sound == null)
                    {
                        m_3Dsounds.Remove(m_3Dsounds[counter]);
                    }
                    else if (!m_3Dsounds[counter].Sound.IsPlaying)
                    {
                        m_3Dsounds[counter].Sound = null;
                        m_3Dsounds.Remove(m_3Dsounds[counter]);
                    }
                    else
                    {
                        if (updatePosition)
                            Update3DCuePosition(m_3Dsounds[counter]);
                        ++counter;
                    }
                }
            }
        }

        private void Update3DCuesPositions()
        {
            if (!m_canPlay)
                return;

            Update3DCuesState(true);
        }

        public object CalculateDspSettingsDebug(IMy3DSoundEmitter source)
        {
            if (m_cueBank == null) 
                return null;

            MySoundData cue = m_cueBank.GetCue(source.SoundId);
            m_helperEmitter.UpdateValuesOmni(source.SourcePosition, source.Velocity, cue, m_deviceDetails.OutputFormat.Channels, source.CustomMaxDistance);
            DspSettingsRef result = new DspSettingsRef(1, m_deviceDetails.OutputFormat.Channels);
            DspSettings settings = result.DspSettings;
            unsafe
            {
                m_x3dAudio.Calculate(m_listener, m_helperEmitter, m_calculateFlags, &settings);
            }
            return result;
        }

        private void Update3DCuePosition(IMy3DSoundEmitter source)
        {
            if(m_cueBank == null) 
                return;

            MySoundData cue = m_cueBank.GetCue(source.SoundId);
            if (cue == null || source.Sound == null)// || !source.Sound.IsBuffered)
                return;

            var sourceVoice = source.Sound as MySourceVoice;
            if (sourceVoice == null)
                return;

            if (!sourceVoice.IsBuffered)
            {
                m_helperEmitter.UpdateValuesOmni(source.SourcePosition, source.Velocity, cue, m_deviceDetails.OutputFormat.Channels, source.CustomMaxDistance);
                float maxDistance = source.CustomMaxDistance.HasValue ? source.CustomMaxDistance.Value : cue.MaxDistance;
                sourceVoice.distanceToListener = m_x3dAudio.Apply3D(sourceVoice.Voice, m_listener, m_helperEmitter, source.SourceChannels, m_deviceDetails.OutputFormat.Channels, m_calculateFlags, maxDistance, source.Sound.FrequencyRatio, sourceVoice.Silent, !source.Realistic);
            }
            else
            {
                float maxDistance = source.CustomMaxDistance.Value;
                m_helperEmitter.UpdateValuesOmni(source.SourcePosition, source.Velocity, maxDistance, m_deviceDetails.OutputFormat.Channels, cue.VolumeCurve);
                sourceVoice.distanceToListener = m_x3dAudio.Apply3D(sourceVoice.Voice, m_listener, m_helperEmitter, source.SourceChannels, m_deviceDetails.OutputFormat.Channels, m_calculateFlags, maxDistance, sourceVoice.FrequencyRatio, sourceVoice.Silent, !source.Realistic);     
            }
        }

        private void StopUpdating3DCue(IMy3DSoundEmitter source)
        {
            if (!m_canPlay)
                return;

            m_3Dsounds.Remove(source);
        }

        public void StopUpdatingAll3DCues()
        {
            if (!m_canPlay)
                return;

            m_3Dsounds.Clear();
        }

        public bool SourceIsCloseEnoughToPlaySound(Vector3 sourcePosition, MyCueId cueId, float? customMaxDistance = 0)
        {
            if (m_cueBank == null || cueId.Hash == MyStringHash.NullOrEmpty)
                return false;

            MySoundData cueDefinition = m_cueBank.GetCue(cueId);
            if (cueDefinition == null)
                return false;

            float distanceToSound = Vector3.DistanceSquared(new Vector3(m_listener.Position.X, m_listener.Position.Y, m_listener.Position.Z), sourcePosition);

            if (customMaxDistance > 0)
                return (distanceToSound <= customMaxDistance * customMaxDistance);
            else if (cueDefinition.UpdateDistance > 0)
                return (distanceToSound <= cueDefinition.UpdateDistance * cueDefinition.UpdateDistance);
            else
                return (distanceToSound <= cueDefinition.MaxDistance * cueDefinition.MaxDistance);
        }

        internal MySourceVoice PlaySound(MyCueId cueId, IMy3DSoundEmitter source = null, MySoundDimensions type = MySoundDimensions.D2, bool skipIntro = false, bool skipToEnd = false)
        {
			int waveNumber = -1;
			var sound = GetSound(cueId, out waveNumber, source, type);
			if(source != null)
				source.LastPlayedWaveNumber = -1;
			if (sound != null)
			{
                sound.Start(skipIntro, skipToEnd);
				if (source != null)
					source.LastPlayedWaveNumber = waveNumber;
			}
            return sound;
        }

        internal MySourceVoice GetSound(MyCueId cueId, out int waveNumber, IMy3DSoundEmitter source = null, MySoundDimensions type = MySoundDimensions.D2)
        {
			waveNumber = -1;
            //  If this computer can't play sound, we don't create cues
            if (cueId.Hash == MyStringHash.NullOrEmpty || !m_canPlay || m_cueBank == null)
                return null;

            //  If this is one-time cue, we check if it is close enough to hear it and if not, we don't even play - this is for optimization only.
            //  We must add loopable cues always, because if source of cue comes near the camera, we need to update the position, but of course we can do that only if we have reference to it.
            MySoundData cue = m_cueBank.GetCue(cueId);

            if ((SoloCue != null) && (SoloCue != cue))
                return null;

            int waveNumberToIgnore = (source != null ? source.LastPlayedWaveNumber : -1);
            var sound = m_cueBank.GetVoice(cueId, out waveNumber, type, waveNumberToIgnore);
            var originalType = type;
            if (sound == null && source != null && source.Force3D)
            {
                originalType = type == MySoundDimensions.D3 ? MySoundDimensions.D2 : MySoundDimensions.D3;
                sound = m_cueBank.GetVoice(cueId, out waveNumber, originalType, waveNumberToIgnore);
            }
            if (sound == null)
                return null;

            float volume = cue.Volume;
            if (source != null && source.CustomVolume.HasValue)
                volume = source.CustomVolume.Value;
            if (cue.VolumeVariation != 0f)
            {
                float variation = VolumeVariation(cue);
                volume = MathHelper.Clamp(volume + variation, 0f, 1f);
            }
            sound.SetVolume(volume);
            
            float semitones = cue.Pitch;
            if (cue.PitchVariation != 0f)
                semitones += PitchVariation(cue);
            if (cue.DisablePitchEffects)
                semitones = 0f;
            if (semitones != 0f)
                sound.FrequencyRatio = SemitonesToFrequencyRatio(semitones);
            else
                sound.FrequencyRatio = 1f;

            if (cue.IsHudCue)
                sound.Voice.SetOutputVoices(m_hudAudioVoiceDesc);
            else
                sound.Voice.SetOutputVoices(m_gameAudioVoiceDesc);

            if (type == MySoundDimensions.D3)
            {
                m_helperEmitter.UpdateValuesOmni(source.SourcePosition, source.Velocity, cue, m_deviceDetails.OutputFormat.Channels, source.CustomMaxDistance);
                float maxDistance = source.CustomMaxDistance.HasValue ? source.CustomMaxDistance.Value : cue.MaxDistance;

                source.SourceChannels = 1;
                if (originalType == MySoundDimensions.D2)
                    source.SourceChannels = 2;
                sound.distanceToListener = m_x3dAudio.Apply3D(sound.Voice, m_listener, m_helperEmitter, source.SourceChannels, m_deviceDetails.OutputFormat.Channels, m_calculateFlags, maxDistance, sound.FrequencyRatio, sound.Silent, source.Realistic);

                Update3DCuesState();

                // why was this only for loops?
                //if (sound.IsLoopable)
                Add3DCueToUpdateList(source);

                ++m_soundInstancesTotal3D;
            }
            else
            {
                if (m_3Dsounds.Contains(source))
                    StopUpdating3DCue(source);
                ++m_soundInstancesTotal2D;
            }
            return sound;
        }

        IMySourceVoice IMyAudio.PlaySound(MyCueId cueId, IMy3DSoundEmitter source, MySoundDimensions type, bool skipIntro, bool skipToEnd)
        {
            return PlaySound(cueId, source, type, skipIntro, skipToEnd);
        }

        IMySourceVoice IMyAudio.GetSound(MyCueId cueId, IMy3DSoundEmitter source, MySoundDimensions type)
        {
			int waveNumber;
            return GetSound(cueId, out waveNumber, source, type);
        }

        IMySourceVoice IMyAudio.GetSound(IMy3DSoundEmitter source, int sampleRate, int channels, MySoundDimensions dimension)
        {
            if (!m_canPlay)
                return null;

            var waveFormat = new WaveFormat(sampleRate, channels);
            source.SourceChannels = channels;
            var sourceVoice = new MySourceVoice(m_audioEngine, waveFormat);

            float volume = source.CustomVolume.HasValue ? source.CustomVolume.Value : 1;
            float maxDistance = source.CustomMaxDistance.HasValue ? source.CustomMaxDistance.Value : 0;

            sourceVoice.SetVolume(volume);

            if (dimension == MySoundDimensions.D3)
            {
                m_helperEmitter.UpdateValuesOmni(source.SourcePosition, source.Velocity, maxDistance, m_deviceDetails.OutputFormat.Channels, MyCurveType.Linear);
                sourceVoice.distanceToListener = m_x3dAudio.Apply3D(sourceVoice.Voice, m_listener, m_helperEmitter, source.SourceChannels, m_deviceDetails.OutputFormat.Channels, m_calculateFlags, maxDistance, sourceVoice.FrequencyRatio, sourceVoice.Silent, source.Realistic);
                Update3DCuesState();
                Add3DCueToUpdateList(source);

                ++m_soundInstancesTotal3D;
            }

            return sourceVoice;
        }

        public void WriteDebugInfo(StringBuilder sb)
        {
            if(m_cueBank != null)
                m_cueBank.WriteDebugInfo(sb);
        }

        public bool IsLoopable(MyCueId cueId)
        {
            if (cueId.Hash == MyStringHash.NullOrEmpty || m_cueBank == null)
                return false;
            MySoundData cueDefinition = m_cueBank.GetCue(cueId);
            if (cueDefinition == null)
                return false;

            return cueDefinition.Loopable;
        }

        public ListReader<IMy3DSoundEmitter> Get3DSounds()
        {
            return m_3Dsounds;
        }


        public IMyAudioEffect ApplyEffect(IMySourceVoice input, MyStringHash effect, MyCueId[] cueIds = null, float? duration = null, bool musicEffect = false)
        {
            if (m_effectBank == null)
                return null;
			int waveNumber;
            List<MySourceVoice> voices = new List<MySourceVoice>();
            if (cueIds != null)
            {
                foreach (var cueId in cueIds)
                {
                    var sound = GetSound(cueId, out waveNumber);
                    System.Diagnostics.Debug.Assert(sound != null, "Missing sound " + cueId);
                    if (sound != null)
                        voices.Add(sound);
                }
            }
            IMyAudioEffect result = m_effectBank.CreateEffect(input, effect, voices.ToArray(), duration);
            if (musicEffect && result.OutputSound is MySourceVoice)
                (result.OutputSound as MySourceVoice).SetOutputVoices(m_musicAudioVoiceDesc);
            return result;
        }
    }
}
