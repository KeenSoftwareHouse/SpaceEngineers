using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VRage.Audio
{
    public interface IMyAudioEffect
    {
        bool AutoUpdate { get; set; }
        void Update(int stepInMsec);
        void SetPosition(float msecs);
        void SetPositionRelative(float position);
        IMySourceVoice OutputSound { get; }
        bool Finished { get; }
    }
}
