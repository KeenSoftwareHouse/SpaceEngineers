using System;
using System.Runtime.InteropServices;

namespace VRage.Audio.X3DAudio
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    internal struct DspSettings
    {
        public IntPtr MatrixCoefficientsPointer;
        public IntPtr DelayTimesPointer;
        public int SrcChannelCount;
        public int DstChannelCount;
        public float LPFDirectCoefficient;
        public float LPFReverbCoefficient;
        public float ReverbLevel;
        public float DopplerFactor;
        public float EmitterToListenerAngle;
        public float EmitterToListenerDistance;
        public float EmitterVelocityComponent;
        public float ListenerVelocityComponent;
    }
}
