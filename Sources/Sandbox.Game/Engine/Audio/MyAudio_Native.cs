using SharpDX;
using SharpDX.Multimedia;
using SharpDX.XAudio2;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using VRage.Native;

namespace Sandbox.Engine.Audio
{
    static class MyAudio_Native
    {
        [StructLayout(LayoutKind.Sequential, Pack = 2)]
        public struct PCMFORMAT
        {
            /// <summary>
            /// format type
            /// </summary>
            public WaveFormatEncoding waveFormatTag;
            /// <summary>
            /// number of channels
            /// </summary>
            public short channels;
            /// <summary>
            /// sample rate
            /// </summary>
            public int sampleRate;
            /// <summary>
            /// for buffer estimation
            /// </summary>
            public int averageBytesPerSecond;
            /// <summary>
            /// block size of data
            /// </summary>
            public short blockAlign;
            /// <summary>
            /// number of bits per sample of mono data
            /// </summary>
            public short bitsPerSample;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 2)]
        public struct WAVEFORMATEX
        {
            public PCMFORMAT pcmWaveFormat;

            /// <summary>
            /// number of following bytes
            /// </summary>
            public short extraSize;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 2)]
        public struct WAVEFORMATEXTENSIBLE
        {
            public WAVEFORMATEX waveFormat;
            public short wValidBitsPerSample;
            public Speakers dwChannelMask;
            public Guid subFormat;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public unsafe struct XAUDIO2_DEVICE_DETAILS
        {
            public fixed char DeviceId[256];
            public fixed char DisplayName[256];
            public DeviceRole DeviceRole;
            public WAVEFORMATEXTENSIBLE OutputFormat;
        }

        public static unsafe bool HasDeviceChanged(XAudio2 engine, string displayName)
        {
            const int GetDeviceDetailsMethodOffset = 4;
            XAUDIO2_DEVICE_DETAILS details;
            var result = (Result)NativeCall.Function<int, IntPtr, int, IntPtr>(new NativeFunction(engine.NativePointer, GetDeviceDetailsMethodOffset), engine.NativePointer, 0, new IntPtr(&details));
            result.CheckError();
            
            return !displayName.Equals(details.DisplayName, 256);
        }
    }
}
