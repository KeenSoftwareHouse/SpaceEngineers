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

        public static void UnloadData()
        {
            if (Static != null)
            {
                Static.UnloadData();
                Static = null;
            }
        }
    }
}
