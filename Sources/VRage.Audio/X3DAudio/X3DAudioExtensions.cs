using SharpDX.XAudio2;
using System;
using VRage.Data.Audio;
using VRage.Native;
using VRageMath;
using System.Diagnostics;

namespace VRage.Audio.X3DAudio
{
    public static class X3DAudioExtensions
    {
        internal static IntPtr ToPointer(this ChannelAzimuts channelAzimuths)
        {
            return channelAzimuths != null ? channelAzimuths.Data : IntPtr.Zero;
        }

        internal static IntPtr ToPointer(this DistanceCurve distanceCurve)
        {
            unsafe
            {
                return distanceCurve != null ? new IntPtr(distanceCurve.DataPointer) : IntPtr.Zero;
            }
        }

        /// <summary>
        /// Sets default values for emitter, makes it valid
        /// </summary>
        internal static void SetDefaultValues(this Emitter emitter)
        {
            emitter.Position = SharpDX.Vector3.Zero;
            emitter.Velocity = SharpDX.Vector3.Zero;
            emitter.OrientFront = SharpDX.Vector3.UnitZ;
            emitter.OrientTop = SharpDX.Vector3.UnitY;
            emitter.ChannelCount = 1;
            emitter.CurveDistanceScaler = float.MinValue;

            emitter.Cone = null;
        }

        /// <summary>
        /// Sets default values for listener, makes it valid
        /// </summary>
        internal static void SetDefaultValues(this Listener listener)
        {
            listener.Position = SharpDX.Vector3.Zero;
            listener.Velocity = SharpDX.Vector3.Zero;
            listener.OrientFront = SharpDX.Vector3.UnitZ;
            listener.OrientTop = SharpDX.Vector3.UnitY;
        }

        /// <summary>
        /// Updates values of omnidirectional emitter.
        /// Omnidirectional means it's same for all directions. There's no Cone and Front/Top vectors are not used.
        /// </summary>
        internal static void UpdateValuesOmni(this Emitter emitter, Vector3 position, Vector3 velocity, MySoundData cue, int channelsCount, float? customMaxDistance)
        {
            emitter.Position = new SharpDX.Vector3(position.X, position.Y, position.Z);
            emitter.Velocity = new SharpDX.Vector3(velocity.X, velocity.Y, velocity.Z);

            float maxDistance = customMaxDistance.HasValue ? customMaxDistance.Value : cue.MaxDistance;
            emitter.DopplerScaler = 1f;
            emitter.CurveDistanceScaler = maxDistance;
            emitter.VolumeCurve = MyDistanceCurves.Curves[(int)cue.VolumeCurve];

            emitter.InnerRadius = (channelsCount > 2) ? maxDistance : 0f;
            emitter.InnerRadiusAngle = (channelsCount > 2) ? 0.5f * SharpDX.AngleSingle.RightAngle.Radians : 0f;
        }

        internal static void UpdateValuesOmni(this Emitter emitter, Vector3 position, Vector3 velocity, float maxDistance, int channelsCount, MyCurveType volumeCurve)
        {
            emitter.Position = new SharpDX.Vector3(position.X, position.Y, position.Z);
            emitter.Velocity = new SharpDX.Vector3(velocity.X, velocity.Y, velocity.Z);

            emitter.DopplerScaler = 1f;
            emitter.CurveDistanceScaler = maxDistance;
            emitter.VolumeCurve = MyDistanceCurves.Curves[(int)volumeCurve];

            emitter.InnerRadius = (channelsCount > 2) ? maxDistance : 0f;
            emitter.InnerRadiusAngle = (channelsCount > 2) ? 0.5f * SharpDX.AngleSingle.RightAngle.Radians : 0f;
        }

#if !XB1
        internal static unsafe void SetOutputMatrix(this SourceVoice sourceVoice, Voice destionationVoice, int sourceChannels, int destinationChannels, float* matrix, int operationSet = 0)
        {
#if UNSHARPER
			Debug.Assert(false);
			return;
#else
            IntPtr destPtr = destionationVoice != null ? destionationVoice.NativePointer : IntPtr.Zero;
            int result = NativeCall<int>.Method<IntPtr, uint, uint, IntPtr, uint>(sourceVoice.NativePointer, 16, destPtr, (uint)sourceChannels, (uint)destinationChannels, new IntPtr(matrix), (uint)operationSet);
            ((SharpDX.Result)result).CheckError();
#endif
        }
#endif // !XB1

    }
}
