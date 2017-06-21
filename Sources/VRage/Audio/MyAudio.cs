using VRage.Collections;
using VRage.Data.Audio;

namespace VRage.Audio
{
    public class MyAudio
    {
        public static MySoundErrorDelegate OnSoundError
        {
            get;
            private set;
        }

        public static IMyAudio Static
        {
            get;
            private set;
        }

        public static void LoadData(MyAudioInitParams initParams, ListReader<MySoundData> sounds, ListReader<MyAudioEffect> effects)
        {
            Static = initParams.Instance;
            OnSoundError = initParams.OnSoundError;
            Static.LoadData(initParams, sounds, effects);
        }

        //GR: Use Reload data on uload Session in order to load only GUI relate cues (about 56 where all cues are about 602)
        //This saves 300 MB of memory when exiting to main menu
        public static void ReloadData(ListReader<MySoundData> sounds, ListReader<MyAudioEffect> effects)
        {
            Static.ReloadData(sounds, effects);
        }

        public static void UnloadData()
        {
            if (Static != null)
            {
                Static.UnloadData();
                Static = null;
            }
        }

        public static readonly int MAX_SAMPLE_RATE = 48000;
    }
}
