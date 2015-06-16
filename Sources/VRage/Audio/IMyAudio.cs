using System;
using System.Collections.Generic;
using System.Text;
using VRage.Collections;
using VRage.Data.Audio;
using VRage.Library.Utils;
using VRage.Utils;
using VRageMath;

namespace VRage.Audio
{
    public interface IMyAudio 
    {
        Dictionary<MyCueId, MySoundData>.ValueCollection CueDefinitions { get; }
        List<MyStringId> GetCategories();
        MySoundData GetCue(MyCueId cue);

        //IMyCueBank CueBank { get; }
        MySoundData SoloCue
        {
            get;
            set;
        }

        bool ApplyReverb
        {
            get;
            set;
        }

        //  Set/get master volume for all sounds/cues for "Music" category.
        //  Interval <0..1..2>
        //      0.0f  ->   -96 dB (silence) 
        //      1.0f  ->    +0 dB (full pitch as authored) 
        //      2.0f  ->    +6 dB (6 dB greater than authored) 
        float VolumeMusic
        {
            get;
            set;
        }

        //  Set/get master volume for all sounds/cues in "Gui" category.
        //  Interval <0..1..2>
        //      0.0f  ->   -96 dB (silence) 
        //      1.0f  ->    +0 dB (full pitch as authored) 
        //      2.0f  ->    +6 dB (6 dB greater than authored) 
        float VolumeHud
        {
            get;
            set;
        }

        //  Set/get master volume for all in-game sounds/cues.
        //  Interval <0..1..2>
        //      0.0f  ->   -96 dB (silence) 
        //      1.0f  ->    +0 dB (full pitch as authored) 
        //      2.0f  ->    +6 dB (6 dB greater than authored) 
        float VolumeGame
        {
            get;
            set;
        }

        void Pause();
        void Resume();
        void PauseGameSounds();
        void ResumeGameSounds();
        
        bool Mute
        {
            get;
            set;
        }

        bool MusicAllowed
        {
            get;
            set;
        }

        bool GameSoundIsPaused
        {
            get;
        }

        bool EnableVoiceChat
        {
            get;
            set;
        }

        event Action<bool> VoiceChatEnabled;

        void PlayMusic(MyMusicTrack? track = null);
        void StopMusic();
        void MuteHud(bool mute);
        
        bool HasAnyTransition();

        void LoadData(MyAudioInitParams initParams, ListReader<MySoundData> cues, ListReader<MyAudioEffect> effects);
        void UnloadData();
        void ReloadData();
        void ReloadData(ListReader<MySoundData> cues, ListReader<MyAudioEffect> effects);

        //  Updates the state of music and 3D audio system.
        void Update(int stepSizeInMS, Vector3 listenerPosition, Vector3 listenerUp, Vector3 listenerFront);

        //  Add new cue and starts playing it. This can be used for one-time or for looping cues.
        //  Method returns reference to the cue, so if it's looping cue, we can update its position. Or we can stop playing it.
        IMySourceVoice PlaySound(MyCueId cueId, IMy3DSoundEmitter source = null, MySoundDimensions type = MySoundDimensions.D2, bool skipIntro = false, bool skipToEnd = false);

        IMySourceVoice GetSound(MyCueId cueId, IMy3DSoundEmitter source = null, MySoundDimensions type = MySoundDimensions.D2);

        IMySourceVoice GetSound(IMy3DSoundEmitter source, int sampleRate, int channels, MySoundDimensions dimension);

        float SemitonesToFrequencyRatio(float semitones);
        
        int GetUpdating3DSoundsCount();
        int GetSoundInstancesTotal2D();
        int GetSoundInstancesTotal3D();
        
        void StopUpdatingAll3DCues();
        bool SourceIsCloseEnoughToPlaySound(IMy3DSoundEmitter source, MyCueId cueId);
        bool IsLoopable(MyCueId cueId);

        object CalculateDspSettingsDebug(IMy3DSoundEmitter source);

        bool ApplyTransition(MyStringId transitionEnum, int priority = 0, MyStringId? category = null, bool loop = true);

        void WriteDebugInfo(StringBuilder sb);

        ListReader<IMy3DSoundEmitter> Get3DSounds();

        /// <summary>
        /// Creates effect on input emitter
        /// </summary>
        /// <param name="input">Emitter to work with</param>
        /// <param name="effect"></param>
        /// <param name="cueIds">additional cues if effect mixes them (ie. crossfade)</param>
        /// <returns>effect output sound</returns>
        IMyAudioEffect ApplyEffect(IMySourceVoice input, MyStringHash effect, MyCueId[] cueIds = null, float? duration = null);
    }
}
