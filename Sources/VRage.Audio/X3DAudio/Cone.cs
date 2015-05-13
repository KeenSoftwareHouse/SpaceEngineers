using System.Runtime.InteropServices;

namespace VRage.Audio.X3DAudio
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct Cone
    {
        public float InnerAngle;
        public float OuterAngle;
        public float InnerVolume;
        public float OuterVolume;
        public float InnerLpf;
        public float OuterLpf;
        public float InnerReverb;
        public float OuterReverb;
    }
}
