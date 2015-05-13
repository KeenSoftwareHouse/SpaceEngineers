using SharpDX;
using System;
using System.Runtime.InteropServices;

namespace VRage.Audio.X3DAudio
{
    public class Emitter
    {
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        internal struct Native
        {
            public IntPtr ConePointer;
            public Vector3 OrientFront;
            public Vector3 OrientTop;
            public Vector3 Position;
            public Vector3 Velocity;
            public float InnerRadius;
            public float InnerRadiusAngle;
            public int ChannelCount;
            public float ChannelRadius;
            public IntPtr ChannelAzimuthsPointer;
            public IntPtr VolumeCurvePointer;
            public IntPtr LFECurvePointer;
            public IntPtr LPFDirectCurvePointer;
            public IntPtr LPFReverbCurvePointer;
            public IntPtr ReverbCurvePointer;
            public float CurveDistanceScaler;
            public float DopplerScaler;
        }

        public Cone? Cone;
        public Vector3 OrientFront;
        public Vector3 OrientTop;
        public Vector3 Position;
        public Vector3 Velocity;
        public float InnerRadius;
        public float InnerRadiusAngle;
        public int ChannelCount;
        public float ChannelRadius;
        public ChannelAzimuts ChannelAzimuths;
        public DistanceCurve VolumeCurve;
        public DistanceCurve LFECurve;
        public DistanceCurve LPFDirectCurve;
        public DistanceCurve LPFReverbCurve;
        public DistanceCurve ReverbCurve;
        public float CurveDistanceScaler;
        public float DopplerScaler;
    }
}
