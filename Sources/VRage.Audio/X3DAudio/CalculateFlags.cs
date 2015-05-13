using System;

namespace VRage.Audio.X3DAudio
{
    [Flags]
    public enum CalculateFlags
    {
        Matrix = 1,
        Delay = 2,
        LpfDirect = 4,
        LpfReverb = 8,
        Reverb = 16,
        Doppler = 32,
        EmitterAngle = 64,
        ZeroCenter = 65536,
        RedirectToLfe = 131072,
    }
}
