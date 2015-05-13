//#define DEBUG_AUDIO

using System;
using System.Collections.Generic;
using VRageMath;
using Sandbox;
using Sandbox.Engine.TransparentGeometry;
using Sandbox.Game.Entities;
using Sandbox.Engine.Utils;
using SysUtils.Utils;
using VRage.CommonLib.Utils;
using Sandbox.Engine.Physics;
using SysUtils;
using SharpDX.XAudio2;
using SharpDX.Multimedia;
using SharpDX.X3DAudio;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;
using System.Security;
using System.Threading;
using VRage.Trace;
using Sandbox.Game.World;
using System.Diagnostics;
using System.Text;
using Sandbox.Engine.Platform;
using VRage;
using Sandbox.CommonLib.ObjectBuilders;
using Sandbox.CommonLib.ObjectBuilders.Audio;
using Sandbox.CommonLib;
using Sandbox.Game.GUI;
using Sandbox.Game.Screens;

namespace Sandbox.Engine.Audio
{
    enum MyMusicState
    {
        Stopped,
        Playing,
        Transition,
    }

    [MySessionComponentDescriptor(MySessionComponentID.GameAudio, MyUpdateOrder.NoUpdate)]
    class MyAudio : MySessionComponentBase
    {
        static XAudio2 m_audioEngine;
        static DeviceDetails m_deviceDetails;
        static MasteringVoice m_masterVoice;
        static SubmixVoice m_gameAudioVoice;
        static SubmixVoice m_musicAudioVoice;
        static SubmixVoice m_hudAudioVoice;
        static VoiceSendDescriptor[] m_gameAudioVoiceDesc;
        static VoiceSendDescriptor[] m_musicAudioVoiceDesc;
        static VoiceSendDescriptor[] m_hudAudioVoiceDesc;

        static MyCueBank m_cueBank;

        static MyX3DAudio m_x3dAudio;

        static bool m_canPlay;

        static float m_volumeHud;
        static float m_volumeDefault;
        static float m_volumeMusic;

        static bool m_mute;
        static bool m_musicAllowed;

        static bool m_musicOn;
        static bool m_gameSoundsOn;

        static MyMusicState m_musicState;
        static bool m_loopMusic;

        static MySourceVoice m_musicCue;

        //  Music cues
        struct MyMusicTransition
        {
            public int Priority;
            public MyMusicTransitionEnum TransitionEnum;
            public string Category;

            public MyMusicTransition(int priority, MyMusicTransitionEnum transitionEnum, string category)
            {
                Priority = priority;
                TransitionEnum = transitionEnum;
                Category = category;
            }
        }

        static SortedList<int, MyMusicTransition> m_nextTransitions = new SortedList<int, MyMusicTransition>();
        static MyMusicTransition? m_currentTransition;
        static bool m_transitionForward;
        private static StringBuilder m_currentTransitionDescription = new StringBuilder();

        static float m_volumeAtTransitionStart;
        static int m_timeFromTransitionStart; // in ms
        const int TRANSITION_TIME = 1000;     // in ms

        static int m_lastUpdateTime = MyConstants.FAREST_TIME_IN_PAST; // in ms

        static Listener m_listener;
        static Emitter m_helperEmitter; // The emitter describes an entity which is making a 3D sound.
        static List<IMy3DSoundEmitter> m_3Dsounds; // List of currently playing 3D sounds to update
        static bool m_canUpdate3dSounds = true;

        //  Number of sound instances (cue) created/added from the application start
        static int m_soundInstancesTotal2D;
        static int m_soundInstancesTotal3D;

        //  Events
        public delegate void VolumeChangeHandler(float newVolume);
        public static event VolumeChangeHandler OnSetVolumeHud, OnSetVolumeGame, OnSetVolumeMusic;

        public static MyCueBank CueBank { get { return m_cueBank; } }
        public static MyObjectBuilder_CueDefinition SoloCue { get; set; }
        public static Listener Listener { get { return m_listener; } }
        public static bool GameSoundIsPaused { get; private set; }

        private static void Init()
        {
            StartEngine();
            CreateX3DAudio();
        }

        private static void StartEngine()
        {
            if (m_audioEngine != null)
            {
                DisposeVoices();
                m_audioEngine.Dispose();
            }

            // Init/reinit engine
            m_audioEngine = new XAudio2();
            m_masterVoice = new MasteringVoice(m_audioEngine);
            m_gameAudioVoice = new SubmixVoice(m_audioEngine);
            m_musicAudioVoice = new SubmixVoice(m_audioEngine);
            m_hudAudioVoice = new SubmixVoice(m_audioEngine);
            m_gameAudioVoiceDesc = new VoiceSendDescriptor[] { new VoiceSendDescriptor(m_gameAudioVoice) };
            m_musicAudioVoiceDesc = new VoiceSendDescriptor[] { new VoiceSendDescriptor(m_musicAudioVoice) };
            m_hudAudioVoiceDesc = new VoiceSendDescriptor[] { new VoiceSendDescriptor(m_hudAudioVoice) };
        }

        private static void CreateX3DAudio()
        {
            if (m_audioEngine == null)
                return;

            m_deviceDetails = m_audioEngine.GetDeviceDetails(0);
            m_x3dAudio = new MyX3DAudio(m_deviceDetails.OutputFormat);

            MySandboxGame.Log.WriteLine(string.Format("MyAudio.CreateX3DAudio - Device: {0} - Channel #: {1}", m_deviceDetails.DisplayName, m_deviceDetails.OutputFormat.Channels));
        }

        private static void DisposeVoices()
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

        private static void CheckIfDeviceChanged()
        {
            if (MyAudio_Native.HasDeviceChanged(m_audioEngine, m_deviceDetails.DisplayName))
            {
                try
                {
                    Init();
                }
                catch (Exception ex)
                {
                    MySandboxGame.Log.WriteLine("Exception during loading audio engine. Game continues, but without sound. Details: " + ex.ToString(), LoggingOptions.AUDIO);
                    MySandboxGame.Log.WriteLine("Device ID: " + m_deviceDetails.DeviceID, LoggingOptions.AUDIO);
                    MySandboxGame.Log.WriteLine("Device name: " + m_deviceDetails.DisplayName, LoggingOptions.AUDIO);
                    MySandboxGame.Log.WriteLine("Device role: " + m_deviceDetails.Role, LoggingOptions.AUDIO);
                    MySandboxGame.Log.WriteLine("Output format: " + m_deviceDetails.OutputFormat, LoggingOptions.AUDIO);

                    //  This exception is the only way I can know if we can play sound (e.g. if computer doesn't have sound card).
                    //  I didn't find other ways of checking it.
                    m_canPlay = false;
                }

                if (MyFakes.SIMULATE_NO_SOUND_CARD)
                    m_canPlay = false;

                if (m_canPlay)
                {
                    if (m_cueBank != null)
                        m_cueBank.SetAudioEngine(m_audioEngine);

                    m_gameAudioVoice.SetVolume(m_volumeDefault);
                    m_hudAudioVoice.SetVolume(m_volumeHud);
                    m_musicAudioVoice.SetVolume(m_volumeMusic);

                    if ((m_musicCue != null) && m_musicCue.IsPlaying)
                    {
                        // restarts music cue
                        m_musicCue = PlayCue2D(m_musicCue.CueEnum);
                        if (m_musicCue != null)
                            m_musicCue.Voice.SetOutputVoices(m_musicAudioVoiceDesc);

                        UpdateMusic();
                    }
                }
            }
        }

        public static new void LoadData()
        {
            MySandboxGame.Log.WriteLine("MyAudio.LoadData - START");
            MySandboxGame.Log.IncreaseIndent();

            m_canPlay = true;
            MyObjectBuilder_CueDefinitions ob = null;
            try
            {
#if DEBUG
                bool result = false;
                try
                {
                    result = MyObjectBuilder_Base.DeserializeXML(MyCreateFileAudioSBA.GetFilenameSBA(), out ob);
                }
                catch (FileNotFoundException)
                {
                    // generates the Audio.sba file
                    MyCreateFileAudioSBA.Create();
                    result = MyObjectBuilder_Base.DeserializeXML(MyCreateFileAudioSBA.GetFilenameSBA(), out ob);
                }
#else
                bool result = MyObjectBuilder_Base.DeserializeXML(MyCreateFileAudioSBA.GetFilenameSBA(), out ob);
#endif //DEBUG

                if (result)
                {
                    Init();
                }
                else
                {
                    MySandboxGame.Log.WriteLine("Unable to load audio data. Game continues, but without sound", LoggingOptions.AUDIO);
                    m_canPlay = false;
                }
            }
            catch (Exception ex)
            {
                MySandboxGame.Log.WriteLine("Exception during loading audio engine. Game continues, but without sound. Details: " + ex.ToString(), LoggingOptions.AUDIO);
                MySandboxGame.Log.WriteLine("Device ID: " + m_deviceDetails.DeviceID, LoggingOptions.AUDIO);
                MySandboxGame.Log.WriteLine("Device name: " + m_deviceDetails.DisplayName, LoggingOptions.AUDIO);
                MySandboxGame.Log.WriteLine("Device role: " + m_deviceDetails.Role, LoggingOptions.AUDIO);
                MySandboxGame.Log.WriteLine("Output format: " + m_deviceDetails.OutputFormat, LoggingOptions.AUDIO);

                //  This exception is the only way I can know if we can play sound (e.g. if computer doesn't have sound card).
                //  I didn't find other ways of checking it.
                m_canPlay = false;
            }

            if (MyFakes.SIMULATE_NO_SOUND_CARD)
                m_canPlay = false;

            if (m_canPlay)
            {
                m_cueBank = new MyCueBank(m_audioEngine, ob);
                m_3Dsounds = new List<IMy3DSoundEmitter>();
                m_listener = new Listener();
                m_listener.SetDefaultValues();
                m_helperEmitter = new Emitter();
                m_helperEmitter.SetDefaultValues();

                //  This is reverb turned to off, so we hear sounds as they are defined in wav files
                ApplyReverb = false;

                m_musicOn = true;
                m_gameSoundsOn = true;

                //  Volume from config
                VolumeMusic = MyConfig.MusicVolume;
                VolumeGame = MyConfig.GameVolume;
                VolumeHud = MyConfig.GameVolume;
                MyConfig.MusicVolume = VolumeMusic;
                MyConfig.GameVolume = VolumeGame;

                m_mute = false;
                m_musicAllowed = true;

                m_musicState = MyMusicState.Stopped;
                m_loopMusic = true;

                m_transitionForward = false;
                m_timeFromTransitionStart = 0;

                m_soundInstancesTotal2D = 0;
                m_soundInstancesTotal3D = 0;
            }

            MySandboxGame.Log.DecreaseIndent();
            MySandboxGame.Log.WriteLine("MyAudio.LoadData - END");
        }

        public static new void UnloadData()
        {
            MySandboxGame.Log.WriteLine("MyAudio.UnloadData - START");

            if (m_canPlay)
            {
                m_audioEngine.StopEngine();
                if (m_cueBank != null)
                    m_cueBank.Dispose();
            }

            SoloCue = null;

            DisposeVoices();

            if (m_audioEngine != null)
                m_audioEngine.Dispose();

            MySandboxGame.Log.WriteLine("MyAudio.UnloadData - END");
        }

        public static bool ApplyReverb
        {
            get
            {
                if (!m_canPlay)
                    return false;

                if (m_cueBank == null)
                    return false;

                return m_cueBank.ApplyReverb;
            }
            set
            {
                if (!m_canPlay)
                    return;

                if (m_cueBank == null)
                    return;

                m_cueBank.ApplyReverb = value;
            }
        }

        //  Set/get master volume for all sounds/cues for "Music" category.
        //  Interval <0..1..2>
        //      0.0f  ->   -96 dB (silence) 
        //      1.0f  ->    +0 dB (full pitch as authored) 
        //      2.0f  ->    +6 dB (6 dB greater than authored) 
        public static float VolumeMusic
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
                m_musicAudioVoice.SetVolume(m_volumeMusic);

                if (OnSetVolumeMusic != null)
                    OnSetVolumeMusic(m_volumeMusic);
            }
        }

        //  Set/get master volume for all sounds/cues in "Gui" category.
        //  Interval <0..1..2>
        //      0.0f  ->   -96 dB (silence) 
        //      1.0f  ->    +0 dB (full pitch as authored) 
        //      2.0f  ->    +6 dB (6 dB greater than authored) 
        public static float VolumeHud
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
                m_hudAudioVoice.SetVolume(m_volumeHud);

                if (OnSetVolumeHud != null)
                    OnSetVolumeHud(m_volumeHud);
            }
        }

        //  Set/get master volume for all in-game sounds/cues.
        //  Interval <0..1..2>
        //      0.0f  ->   -96 dB (silence) 
        //      1.0f  ->    +0 dB (full pitch as authored) 
        //      2.0f  ->    +6 dB (6 dB greater than authored) 
        public static float VolumeGame
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
                m_gameAudioVoice.SetVolume(m_volumeDefault);

                if (OnSetVolumeGame != null)
                    OnSetVolumeGame(m_volumeDefault);
            }
        }

        public static void Pause()
        {
            if (m_canPlay)
                m_audioEngine.StopEngine();
        }

        public static void Resume()
        {
            if (m_canPlay)
                m_audioEngine.StartEngine();
        }

        public static void PauseGameSounds()
        {
            if (m_canPlay)
            {
                GameSoundIsPaused = true;
                m_gameAudioVoice.SetVolume(0f);
                m_canUpdate3dSounds = false;
            }
        }

        public static void ResumeGameSounds()
        {
            if (m_canPlay)
            {
                GameSoundIsPaused = false;
                if (!Mute)
                    m_gameAudioVoice.SetVolume(m_volumeDefault);

                m_canUpdate3dSounds = true;
            }
        }

        public static bool Mute
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
                            WaitForPlayingHudSounds();
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
                                m_gameAudioVoice.SetVolume(m_volumeDefault);

                            m_musicAudioVoice.SetVolume(m_volumeMusic);
                        }
                    }
                }
            }
        }

        public static bool MusicAllowed
        {
            get { return m_musicAllowed; }
            set { m_musicAllowed = value; }
        }

        public static MySourceVoice GetMusicCue()
        {
            return m_musicCue;
        }

        public static MyMusicState GetMusicState()
        {
            return m_musicState;
        }

        public static void MuteHud(bool mute)
        {
            if (m_canPlay)
                m_hudAudioVoice.SetVolume(mute ? 0f : m_volumeHud);
        }

        public static bool HasAnyTransition()
        {
            return m_nextTransitions.Count > 0;
        }

        public static string AudioPath
        {
            get { return Path.Combine(GameEnvironment.ContentPath, "Audio"); }
        }

        //  Updates the state of music and 3D audio system.
        public static void Update(bool ignoreGameReadyStatus = false)
        {
            if (!MySandboxGame.IsGameReady && !ignoreGameReadyStatus)
                return;

            //if (m_canPlay)
                //CheckIfDeviceChanged();

            if (Mute)
                return;

            if (m_canPlay && (m_cueBank != null))
                m_cueBank.Update();

            int currTime = MySandboxGame.TotalTimeInMilliseconds;
            int diffTime = currTime - m_lastUpdateTime; 
            if (diffTime < 30)
                return;

            m_lastUpdateTime = currTime;

            if (m_canPlay)
            {
                if (!MySandboxGame.IsPaused)
                {
                    //UpdateCollisionCues();
                }

                UpdateMusic();
                Update3DCuesPositions();
            }
        }

        private static void UpdateMusic()
        {
            if (m_musicState == MyMusicState.Transition)
            {
                m_timeFromTransitionStart += MyConstants.PHYSICS_STEP_SIZE_IN_MILLISECONDS;
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
                        m_musicAudioVoice.SetVolume((1f - (float)m_timeFromTransitionStart / TRANSITION_TIME) * m_volumeAtTransitionStart);
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
                    m_musicAudioVoice.SetVolume(m_volumeMusic);
                    PlayMusicByTransition(m_currentTransition.Value);
                    m_nextTransitions.Remove(m_currentTransition.Value.Priority);
                    m_musicState = MyMusicState.Playing;
                }
            }

            if (m_musicState == MyMusicState.Playing)
            {
                if ((m_musicCue != null) && !m_musicCue.IsPlaying)
                {
                    if (m_loopMusic)
                    {
                        // we play current transition in loop
                        Debug.Assert(m_currentTransition != null);
                        PlayMusicByTransition(m_currentTransition.Value);
                    }
                    else
                    {
                        // switches to another, random, track
                        m_currentTransition = null;
                        MyMusicTransitionEnum? newTransitionEnum = GetRandomTransitionEnum();
                        if (newTransitionEnum.HasValue)
                            ApplyTransition(newTransitionEnum.Value, 0, null, false);
                    }
                }
            }
        }

        public static MyMusicTransitionEnum? GetRandomTransitionEnum()
        {
            if (m_cueBank == null)
                return null;

            return m_cueBank.GetRandomTransitionEnum();
        }

        public static bool ApplyTransition(MyMusicTransitionEnum transitionEnum, int priority = 0, string category = null, bool loop = true)
        {
            if (!m_canPlay)
                return false;

            if (!m_musicAllowed)
                return false;

            Debug.Assert(priority >= 0);
            if (category != null)
            {
                if (!m_cueBank.IsValidTransitionCategory(transitionEnum, category))
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
            string transitionCategory = category ?? m_cueBank.GetRandomTransitionCategory(transitionEnum);
            // we set this transition as next
            m_nextTransitions[priority] = new MyMusicTransition(priority, transitionEnum, transitionCategory);
            MyTrace.Send(TraceWindow.Server, string.Format("Applying transition {0} {1} (priority = {2})", transitionEnum, transitionCategory, priority));

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

        private static MyMusicTransition? GetNextTransition()
        {
            if (m_nextTransitions.Count > 0)
                return m_nextTransitions[m_nextTransitions.Keys[m_nextTransitions.Keys.Count - 1]];
            else
                return null;
        }

        private static void StartTransition(bool forward)
        {
            m_transitionForward = forward;
            m_musicState = MyMusicState.Transition;
            m_timeFromTransitionStart = 0;
            m_volumeAtTransitionStart = m_volumeMusic;
        }

        public static void StopTransition(int priority)
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

        private static void PlayMusicByTransition(MyMusicTransition transition)
        {
            m_musicCue = PlayCue2D(m_cueBank.GetTransitionCue(transition.TransitionEnum, transition.Category));
            if (m_musicCue != null)
            {
                m_musicCue.Voice.SetOutputVoices(m_musicAudioVoiceDesc);
                m_musicAudioVoice.SetVolume(m_volumeMusic);
            }
        }

        //  Add new cue and starts playing it. This can be used for one-time or for looping cues.
        //  Method returns reference to the cue, so if it's looping cue, we can update its position. Or we can stop playing it.
        //  These are 2D cues played with stereo. Used for in-cockpit sounds.
        //  We don't do any max instance limiting or distance cutting, because it's not needed.
        public static MySourceVoice PlayCue2D(MySoundCuesEnum cueEnum, float volume = -1f)
        {
            if (cueEnum == MySoundCuesEnum.None)
                return null;

            //  If this computer can't play sound, we don't create cues
            if (!m_canPlay)
                return null;

            if (!MySandboxGame.IsGameReady)
                return null;

            if (m_cueBank == null)
                return null;

            MyObjectBuilder_CueDefinition cue = m_cueBank.GetCue(cueEnum);

            if ((SoloCue != null) && (SoloCue != cue))
                return null;

            volume = (volume != -1f) ? volume : cue.Volume;
            if (cue.VolumeVariation != 0f)
            {
                float variation = VolumeVariation(cue);
                volume = MathHelper.Clamp(volume + variation, 0f, 1f);
            }

            return PlayCue2DInternal(cueEnum, volume);
        }

        private static MySourceVoice PlayCue2DInternal(MySoundCuesEnum cueEnum, float volume)
        {
            if (!m_canPlay)
                return null;

            if (!MySandboxGame.IsGameReady)
                return null;

            if (m_cueBank == null)
                return null;

            MySourceVoice sound = m_cueBank.GetVoice(cueEnum);
            if (sound == null)
                return null;

            MyObjectBuilder_CueDefinition cue = m_cueBank.GetCue(cueEnum);

            if ((SoloCue != null) && (SoloCue != cue))
                return null;

            sound.SetVolume(volume);

            if (cue.PitchVariation != 0f)
            {
                float semitones = PitchVariation(cue);
                sound.FrequencyRatio = SemitonesToFrequencyRatio(semitones);
            }
            else
                sound.FrequencyRatio = 1f;

            sound.Voice.SetFrequencyRatio(sound.FrequencyRatio);

            if (cue.IsHudCue)
                sound.Voice.SetOutputVoices(m_hudAudioVoiceDesc);
            else
                sound.Voice.SetOutputVoices(m_gameAudioVoiceDesc);

            sound.Start();

            ++m_soundInstancesTotal2D;

            return sound;
        }

        static public void WaitForPlayingHudSounds()
        {
            if (m_cueBank == null)
                return;

            while (m_cueBank.IsPlayingHudSounds()) { }
        }

        private static MySourceVoice TrySwitchToCue2D(IMy3DSoundEmitter source, MyObjectBuilder_CueDefinition cue3D)
        {
            MyEntity entity = source.Entity;
            MyEntity owner = source.OwnedBy;
            MyEntity parent = (owner != null) ? owner.Parent : null;
            bool isControlledObject = (entity == MySession.ControlledObject) || (owner == MySession.ControlledObject) || (parent == MySession.ControlledObject);
            if ((MyGuiScreenTerminal.IsOpen && (MyGuiScreenTerminal.Interacted == entity)) || (isControlledObject && (cue3D.Alternative2D != string.Empty)))
            {
                try
                {
                    MySoundCuesEnum cueEnum = (MySoundCuesEnum)Enum.Parse(typeof(MySoundCuesEnum), cue3D.Alternative2D);
                    MyObjectBuilder_CueDefinition cue2D = m_cueBank.GetCue(cueEnum);
                    float volume = cue2D.Volume;
                    if (cue2D.VolumeVariation != 0f)
                    {
                        float variation = VolumeVariation(cue2D);
                        volume = MathHelper.Clamp(volume + variation, 0f, 1f);
                    }

                    if (m_3Dsounds.Contains(source))
                        StopUpdating3DCue(source);

                    return PlayCue2DInternal(cueEnum, volume);
                }
                catch (Exception)
                {
                    return null;
                }
            }

            return null;
        }

        private static bool TrySwitchToIntroLoopPair2D(IMy3DSoundEmitter source, ref MySoundCuesEnum introCueEnum, ref MySoundCuesEnum loopCueEnum)
        {
            MyObjectBuilder_CueDefinition introCue = m_cueBank.GetCue(introCueEnum);
            MyObjectBuilder_CueDefinition loopCue = m_cueBank.GetCue(loopCueEnum);
            MyEntity entity = source.Entity;
            MyEntity owner = source.OwnedBy;
            MyEntity parent = (owner != null) ? owner.Parent : null;
            bool isControlledObject = (entity == MySession.ControlledObject) || (owner == MySession.ControlledObject) || (parent == MySession.ControlledObject);
            if ((MyGuiScreenTerminal.IsOpen && (MyGuiScreenTerminal.Interacted == entity)) || (isControlledObject && (introCue.Alternative2D != string.Empty) && (loopCue.Alternative2D != string.Empty)))
            {
                try
                {
                    MySoundCuesEnum introEnum = (MySoundCuesEnum)Enum.Parse(typeof(MySoundCuesEnum), introCue.Alternative2D);
                    MySoundCuesEnum loopEnum = (MySoundCuesEnum)Enum.Parse(typeof(MySoundCuesEnum), loopCue.Alternative2D);
                    introCueEnum = introEnum;
                    loopCueEnum = loopEnum;
                    return true;
                }
                catch (Exception)
                {
                    return false;
                }
            }

            return false;
        }

        private static float VolumeVariation(MyObjectBuilder_CueDefinition cue)
        {
            return MyVRageUtils.GetRandomFloat(-1f, 1f) * cue.VolumeVariation * 0.07f;
        }

        private static float PitchVariation(MyObjectBuilder_CueDefinition cue)
        {
            return MyVRageUtils.GetRandomFloat(-1f, 1f) * cue.PitchVariation / 100f;
        }

        public static float SemitonesToFrequencyRatio(float semitones)
        {
            return XAudio2.SemitonesToFrequencyRatio(semitones);
        }

        public static MySourceVoice PlayIntroLoopPair3D(IMy3DSoundEmitter source, MySoundCuesEnum introCueEnum, MySoundCuesEnum loopCueEnum, bool addToUpdateList = true)
        {
            if ((introCueEnum == MySoundCuesEnum.None) || (loopCueEnum == MySoundCuesEnum.None))
                return null;

            //  If this computer can't play sound, we don't create cues
            if (!m_canPlay)
                return null;

            if (!MySandboxGame.IsGameReady)
                return null;

            if (m_cueBank == null)
                return null;

            MyObjectBuilder_CueDefinition introCue = m_cueBank.GetCue(introCueEnum);
            MyObjectBuilder_CueDefinition loopCue = m_cueBank.GetCue(loopCueEnum);
            Debug.Assert(!introCue.IsHudCue && !loopCue.IsHudCue, "Hud cue can't be played as 3d sound!");
            Debug.Assert(!introCue.Loopable && loopCue.Loopable, "MyAudio.PlayIntroLoopPair3D is meant to play only pairs intro-loop");

            MySourceVoice voice = null;
            if (TrySwitchToIntroLoopPair2D(source, ref introCueEnum, ref loopCueEnum))
            {
                introCue = m_cueBank.GetCue(introCueEnum);
                loopCue = m_cueBank.GetCue(loopCueEnum);

                voice = m_cueBank.GetVoice(introCueEnum);
                if (voice != null)
                {
                    if (loopCue.PitchVariation != 0f)
                    {
                        float semitones = PitchVariation(loopCue);
                        voice.FrequencyRatio = SemitonesToFrequencyRatio(semitones);
                    }
                    else
                        voice.FrequencyRatio = 1f;

                    float volume2D = loopCue.Volume;
                    if (loopCue.VolumeVariation != 0f)
                    {
                        float variation = VolumeVariation(loopCue);
                        volume2D = MathHelper.Clamp(volume2D + variation, 0f, 1f);
                    }

                    voice.SetVolume(volume2D);
                    voice.Voice.SetFrequencyRatio(voice.FrequencyRatio);
                    voice.Voice.SetOutputVoices(m_gameAudioVoiceDesc);

                    MyInMemoryWave wave2D = m_cueBank.GetRandomWave(loopCue);
                    if (wave2D != null)
                    {
                        if (m_3Dsounds.Contains(source))
                            StopUpdating3DCue(source);

                        Debug.Assert(voice.Owner.WaveFormat.Encoding == wave2D.WaveFormat.Encoding, "Intro and loop cues must have the same encoding!");
                        Debug.Assert((voice.Owner.WaveFormat.Channels == 2) && (voice.Owner.WaveFormat.Channels == wave2D.WaveFormat.Channels), "Both intro and loop cues must have 2 channels");
                        if (voice.Owner.WaveFormat.Encoding == wave2D.WaveFormat.Encoding)
                        {
                            voice.SubmitSourceBuffer(loopCueEnum, wave2D.Buffer, wave2D.Stream.DecodedPacketsInfo, wave2D.WaveFormat.SampleRate);
                            voice.Start();
                            ++m_soundInstancesTotal2D;
                        }
                    }
                }
                return voice;
            }

            voice = m_cueBank.GetVoice(introCueEnum);
            if (voice == null)
                return null;

            if (loopCue.PitchVariation != 0f)
            {
                float semitones = PitchVariation(loopCue);
                voice.FrequencyRatio = SemitonesToFrequencyRatio(semitones);
            }
            else
                voice.FrequencyRatio = 1f;

            float volume = loopCue.Volume;
            if (loopCue.VolumeVariation != 0f)
            {
                float variation = VolumeVariation(loopCue);
                volume = MathHelper.Clamp(volume + variation, 0f, 1f);
            }

            voice.SetVolume(volume);
            voice.Voice.SetFrequencyRatio(voice.FrequencyRatio);
            voice.Voice.SetOutputVoices(m_gameAudioVoiceDesc);

            MyInMemoryWave wave3D = m_cueBank.GetRandomWave(loopCue);
            if (wave3D == null)
                return null;

            Debug.Assert(voice.Owner.WaveFormat.Encoding == wave3D.WaveFormat.Encoding, "Intro and loop cues must have the same encoding!");
            Debug.Assert((voice.Owner.WaveFormat.Channels == 1) && (voice.Owner.WaveFormat.Channels == wave3D.WaveFormat.Channels), "Both intro and loop cues must have 1 channel");
            if (voice.Owner.WaveFormat.Encoding != wave3D.WaveFormat.Encoding)
                return null;

            voice.SubmitSourceBuffer(loopCueEnum, wave3D.Buffer, wave3D.Stream.DecodedPacketsInfo, wave3D.WaveFormat.SampleRate);
            voice.Start();

            if (!m_listener.UpdateFromPlayer())
                m_listener.UpdateFromMainCamera();

            m_helperEmitter.UpdateValues(source.SourcePosition, source.DirForward, source.DirUp, source.Velocity, loopCue, m_deviceDetails.OutputFormat.Channels);
            m_x3dAudio.Apply3D(voice.Voice, m_listener, m_helperEmitter, introCue.MaxDistance, voice.FrequencyRatio);

            Update3DCuesState();

            if (addToUpdateList)
                Add3DCueToUpdateList(source, loopCueEnum);

            ++m_soundInstancesTotal3D;

            return voice;
        }

        private static void Add3DCueToUpdateList(IMy3DSoundEmitter source, MySoundCuesEnum cueEnum)
        {
            m_3Dsounds.Add(source);
            MyHudEntityParams hudEntityParams = new MyHudEntityParams()
            {
                FlagsEnum = MyHudIndicatorFlagsEnum.SHOW_ALL,
                Text = new StringBuilder(cueEnum.ToString()),
            };
            if (MyFakes.DEBUG_DRAW_AUDIO)
                MyHud.LocationMarkers.RegisterMarker(m_3Dsounds[m_3Dsounds.Count - 1].Entity, hudEntityParams);
        }

        //  Adds new cue and starts playing it.
        //  Method returns reference to the cue, so that we can stop playing it.
        public static MySourceVoice PlayCue3D(IMy3DSoundEmitter source, MySoundCuesEnum cueEnum, bool addToUpdateList = true)
        {
            if (cueEnum == MySoundCuesEnum.None)
                return null;

            //  If this computer can't play sound, we don't create cues
            if (!m_canPlay)
                return null;

            if (!MySandboxGame.IsGameReady)
                return null;

            if (m_cueBank == null)
                return null;

            //  If this is one-time cue, we check if it is close enough to hear it and if not, we don't even play - this is for optimization only.
            //  We must add loopable cues always, because if source of cue comes near the camera, we need to update the position, but of course we can do that only if we have reference to it.
            MyObjectBuilder_CueDefinition cue = m_cueBank.GetCue(cueEnum);
            Debug.Assert(!cue.IsHudCue, "Hud cue can't be played as 3d sound!");

            if ((SoloCue != null) && (SoloCue != cue))
                return null;

            if (!cue.Loopable)
            {
                float distanceToSound;
                if ((MySession.Player != null) && (MySession.Player.PlayerEntity != null) && (MySession.Player.PlayerEntity.Entity != null))
                    distanceToSound = Vector3.DistanceSquared(MySession.Player.PlayerEntity.Entity.GetPosition(), source.SourcePosition);
                else if (MySector.MainCamera != null)
                    distanceToSound = Vector3.DistanceSquared(MySector.MainCamera.Position, source.SourcePosition);
                else
                    return null;

                if (distanceToSound > cue.MaxDistance * cue.MaxDistance)
                    return null;
            }

            MySourceVoice sound = null;
            sound = TrySwitchToCue2D(source, cue);
            if (sound != null)
                return sound;

            sound = m_cueBank.GetVoice(cueEnum);
            if (sound == null)
                return null;

            if (cue.PitchVariation != 0f)
            {
                float semitones = PitchVariation(cue);
                sound.FrequencyRatio = SemitonesToFrequencyRatio(semitones);
            }
            else
                sound.FrequencyRatio = 1f;

            float volume = cue.Volume;
            if (cue.VolumeVariation != 0f)
            {
                float variation = VolumeVariation(cue);
                volume = MathHelper.Clamp(volume + variation, 0f, 1f);
            }

            sound.SetVolume(volume);
            sound.Voice.SetOutputVoices(m_gameAudioVoiceDesc);

            //if (cue.UseOcclusion)
            //{
            //    //occlusions are disabled;
            //}

            //  Play the cue
            sound.Start();

            if (!m_listener.UpdateFromPlayer())
                m_listener.UpdateFromMainCamera();

            m_helperEmitter.UpdateValues(source.SourcePosition, source.DirForward, source.DirUp, source.Velocity, cue, m_deviceDetails.OutputFormat.Channels);
            m_x3dAudio.Apply3D(sound.Voice, m_listener, m_helperEmitter, cue.MaxDistance, sound.FrequencyRatio);

            Update3DCuesState();

            if (addToUpdateList)
                Add3DCueToUpdateList(source, cueEnum);

            ++m_soundInstancesTotal3D;

            return sound;
        }

        //public static float CalculateOcclusion(ref Vector3 position)
        //{
        //    // Occlusions are disabled
        //    return 0f;
        //}

        //public static void CalculateOcclusion(MySourceVoice cue, Vector3 position)
        //{
        //    // Occlusions are disabled
        //    return;
        //}

        public static void WriteDebugInfo(StringBuilder builder)
        {
            if (m_cueBank != null)
                m_cueBank.WriteDebugInfo(builder);
        }

        public static int GetUpdating3DSoundsCount()
        {
            return (m_3Dsounds != null) ? m_3Dsounds.Count : 0;
        }

        public static int GetSoundInstancesTotal2D()
        {
            return m_soundInstancesTotal2D;
        }

        public static int GetSoundInstancesTotal3D()
        {
            return m_soundInstancesTotal3D;
        }

        private static void Update3DCuesState(bool updatePosition = false)
        {
            if (!m_canUpdate3dSounds)
                return;

            int counter = 0;
            while (counter < m_3Dsounds.Count)
            {
                if (m_3Dsounds[counter].Sound == null)
                {
                    if (MyFakes.DEBUG_DRAW_AUDIO)
                        MyHud.LocationMarkers.UnregisterMarker(m_3Dsounds[counter].Entity);

                    m_3Dsounds.Remove(m_3Dsounds[counter]);
                }
                else if (!m_3Dsounds[counter].Sound.IsPlaying)
                {
                    if (MyFakes.DEBUG_DRAW_AUDIO)
                        MyHud.LocationMarkers.UnregisterMarker(m_3Dsounds[counter].Entity);

                    m_3Dsounds[counter].Sound = null;
                    m_3Dsounds.Remove(m_3Dsounds[counter]);
                }
                else
                {
                    if (updatePosition)
                    {
                        Update3DCuePosition(m_3Dsounds[counter]);
                        if (MyFakes.DEBUG_DRAW_AUDIO)
                            VRageRender.MyRenderProxy.DebugDrawSphere(m_3Dsounds[counter].SourcePosition, 0.25f, Color.Red.ToVector3(), 1f, true, true);
                    }
                    ++counter;
                }
            }
        }

        private static void Update3DCuesPositions()
        {
            if (!m_canPlay)
                return;

            if (!m_listener.UpdateFromPlayer())
                m_listener.UpdateFromMainCamera();

            Update3DCuesState(true);
        }

        private static void Update3DCuePosition(IMy3DSoundEmitter source)
        {
            //// Occlusions are disabled
            //if (!MyFakes.OPTIMIZATION_FOR_300_SMALLSHIPS)
            //{
            //    CalculateOcclusion(cue, position);
            //}

            MyObjectBuilder_CueDefinition cue = m_cueBank.GetCue(source.CueEnum);
            m_helperEmitter.UpdateValues(source.SourcePosition, source.DirForward, source.DirUp, source.Velocity, cue, m_deviceDetails.OutputFormat.Channels);
            m_x3dAudio.Apply3D(source.Sound.Voice, m_listener, m_helperEmitter, cue.MaxDistance, source.Sound.FrequencyRatio);
        }

        private static void StopUpdating3DCue(IMy3DSoundEmitter source)
        {
            if (!m_canPlay)
                return;

            if (MyFakes.DEBUG_DRAW_AUDIO)
                MyHud.LocationMarkers.UnregisterMarker(source.Entity);

            m_3Dsounds.Remove(source);
        }

        public static void StopUpdatingAll3DCues()        
        {
            if (!m_canPlay)
                return;

            while (m_3Dsounds.Count > 0)
            {
                if (MyFakes.DEBUG_DRAW_AUDIO)
                    MyHud.LocationMarkers.UnregisterMarker(m_3Dsounds[0].Entity);

                m_3Dsounds.Remove(m_3Dsounds[0]);
            }
        }

        public static bool SourceIsCloseEnoughToPlaySound(IMy3DSoundEmitter source, MySoundCuesEnum cueEnum)
        {
            if (m_cueBank == null)
                return false;

            if (cueEnum == MySoundCuesEnum.None)
                return false;

            MyObjectBuilder_CueDefinition cue = m_cueBank.GetCue(cueEnum);
            if (cue == null)
                return false;

            float distanceToSound;
            if ((MySession.Player != null) && (MySession.Player.PlayerEntity != null) && (MySession.Player.PlayerEntity.Entity != null))
                distanceToSound = Vector3.DistanceSquared(MySession.Player.PlayerEntity.Entity.GetPosition(), source.SourcePosition);
            else if (MySector.MainCamera != null)
                distanceToSound = Vector3.DistanceSquared(MySector.MainCamera.Position, source.SourcePosition);
            else
                return false;

            return (distanceToSound <= cue.MaxDistance * cue.MaxDistance);
        }

        public static MySourceVoice PlayTestSound(MySoundCuesEnum cueEnum)
        {
            if (cueEnum == MySoundCuesEnum.None)
                return null;

            MyObjectBuilder_CueDefinition cue = m_cueBank.GetCue(cueEnum);
            string name = cue.Name.ToLower();
            if (name.EndsWith("2d") || cue.IsHudCue)
                return PlayCue2D(cueEnum);

            //  If this computer can't play sound, we don't create cues
            if (!m_canPlay)
                return null;

            if (!MySandboxGame.IsGameReady)
                return null;

            if (m_cueBank == null)
                return null;

            if (!name.EndsWith("3d"))
                return null;

            MySourceVoice sound = m_cueBank.GetVoice(cueEnum);
            if (sound == null)
                return null;

            if (cue.PitchVariation != 0f)
            {
                float semitones = PitchVariation(cue);
                sound.FrequencyRatio = SemitonesToFrequencyRatio(semitones);
            }
            else
                sound.FrequencyRatio = 1f;

            float volume = cue.Volume;
            if (cue.VolumeVariation != 0f)
            {
                float variation = VolumeVariation(cue);
                volume = MathHelper.Clamp(volume + variation, 0f, 1f);
            }

            sound.SetVolume(volume);
            sound.Voice.SetOutputVoices(m_gameAudioVoiceDesc);

            //  Play the cue
            sound.Start();

            return sound;
        }

        internal static void SaveCueDefinitions()
        {
            MyObjectBuilder_CueDefinitions ob = CueBank.GetObjectBuilder();
            MyObjectBuilder_Base.SerializeXML(MyCreateFileAudioSBA.GetFilenameSBA(), ob);
        }
    }
}
