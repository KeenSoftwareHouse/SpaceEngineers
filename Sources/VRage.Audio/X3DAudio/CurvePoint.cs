using System.Runtime.InteropServices;

namespace VRage.Audio.X3DAudio
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct CurvePoint
    {
        public float Distance;
        public float DspSetting;

        public CurvePoint(float distance, float dspSetting)
        {
            Distance = distance;
            DspSetting = dspSetting;
        }
    }
}
