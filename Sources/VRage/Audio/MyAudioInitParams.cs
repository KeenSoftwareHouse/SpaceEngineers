using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.Data.Audio;

namespace VRage.Audio
{
    public delegate void MySoundErrorDelegate(MySoundData cue, string message);

    public struct MyAudioInitParams
    {
        public IMyAudio Instance;
        public bool SimulateNoSoundCard;
        public bool DisablePooling;
        public MySoundErrorDelegate OnSoundError;
    }
}
