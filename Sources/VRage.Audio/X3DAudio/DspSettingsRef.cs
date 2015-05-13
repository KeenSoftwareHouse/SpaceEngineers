using System;
using System.Runtime.InteropServices;

namespace VRage.Audio.X3DAudio
{
    internal sealed class DspSettingsRef : IDisposable
    {
        public DspSettings DspSettings;

        public readonly IntPtr DelayTimes;
        public readonly IntPtr MatrixCoefficients;

        bool m_disposed = false;

        public DspSettingsRef(int srcChannelCount, int dstChannelCount)
        {
            DspSettings.SrcChannelCount = srcChannelCount;
            DspSettings.DstChannelCount = dstChannelCount;
            DelayTimes = Marshal.AllocHGlobal(sizeof(float) * srcChannelCount);
            MatrixCoefficients = Marshal.AllocHGlobal(sizeof(float) * srcChannelCount * dstChannelCount);

            DspSettings.DelayTimesPointer = DelayTimes;
            DspSettings.MatrixCoefficientsPointer = MatrixCoefficients;
        }

        void ReleaseNative()
        {
            if (!m_disposed)
            {
                Marshal.FreeHGlobal(DelayTimes);
                Marshal.FreeHGlobal(MatrixCoefficients);
                m_disposed = true;
            }
        }

        public void Dispose()
        {
            ReleaseNative();
            GC.SuppressFinalize(this);
        }

        ~DspSettingsRef()
        {
            ReleaseNative();
        }
    }
}
