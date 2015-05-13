using SharpDX;
using System;
using System.Runtime.InteropServices;

namespace VRage.Audio.X3DAudio
{
    public sealed class ChannelAzimuts : IDisposable
    {
        internal IntPtr Data;

        public readonly int ChannelCount;

        public ChannelAzimuts(params float[] channelAzimuths)
        {
            Data = Marshal.AllocHGlobal(sizeof(float) * channelAzimuths.Length);
            ChannelCount = channelAzimuths.Length;
            Write(channelAzimuths);
        }

        public void SetData(params float[] channelAzimuths)
        {
            if (channelAzimuths.Length != ChannelCount)
                throw new InvalidOperationException("Passed array has wrong length, length must be same as ChannelCount");

            Write(channelAzimuths);
        }

        void Write(float[] channelAzimuths)
        {
            Utilities.Write(Data, channelAzimuths, 0, channelAzimuths.Length);
        }

        void ReleaseNative()
        {
            Marshal.FreeHGlobal(Data);
            Data = IntPtr.Zero;
        }
        
        public void Dispose()
        {
            ReleaseNative();
            GC.SuppressFinalize(this);
        }

        ~ChannelAzimuts()
        {
            ReleaseNative();
        }
    }
}
