using SharpDX.Multimedia;
using SharpDX.XAudio2;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security;
using VRageMath;

namespace VRage.Audio.X3DAudio
{
    class MyX3DAudio
    {
        // Matrix coeficients valid for 5.1 configuration
        const int FrontLeft = 0;
        const int FrontRight = 1;
        const int Center = 2;
        const int Sub = 3;
        const int RearLeft = 4;
        const int RearRight = 5;

        X3DAudioHandle m_x3dAudioHandle;

        [SuppressUnmanagedCodeSecurity]
        [DllImport("X3DAudio1_7.dll", EntryPoint = "X3DAudioInitialize", CallingConvention = CallingConvention.Cdecl)]
        private static extern unsafe void X3DAudioInitialize_(int arg0, float arg1, void* arg2);

        [SuppressUnmanagedCodeSecurity]
        [DllImport("X3DAudio1_7.dll", EntryPoint = "X3DAudioCalculate", CallingConvention = CallingConvention.Cdecl)]
        private static extern unsafe void X3DAudioCalculate_(void* arg0, void* arg1, void* arg2, int arg3, void* arg4);

        public MyX3DAudio(Speakers speakerMask, float speedOfSound = 343.5f)
        {
            unsafe
            {
                X3DAudioHandle result;
                X3DAudioInitialize_((int)speakerMask, speedOfSound, &result);
                m_x3dAudioHandle = result;
            }
        }

        public unsafe void Calculate(Listener listener, Emitter emitter, CalculateFlags flags, DspSettings* result)
        {
            Debug.Assert((flags & CalculateFlags.Delay) == 0 || (result->DelayTimesPointer != IntPtr.Zero), "When CalculateFlags.Delay is specified, DelayTimesPointer must point to float[DstChannelCount]");
            Debug.Assert((flags & CalculateFlags.Matrix) == 0 || (result->MatrixCoefficientsPointer != IntPtr.Zero), "When CalculateFlags.Matrix is specified, MatrixCoefficientsPointer must point to float[SrcChannelCount*DstChannelCount]");

            Cone emitterCone;
            Emitter.Native nativeEmitter = new Emitter.Native();

            Cone listenerCone;
            Listener.Native nativeListener = new Listener.Native();

            {
                if (emitter.Cone.HasValue)
                {
                    emitterCone = emitter.Cone.Value;
                    nativeEmitter.ConePointer = new IntPtr(&emitterCone);
                }
                else
                {
                    nativeEmitter.ConePointer = IntPtr.Zero;
                }
                nativeEmitter.OrientFront = emitter.OrientFront;
                nativeEmitter.OrientTop = emitter.OrientTop;
                nativeEmitter.Position = emitter.Position;
                nativeEmitter.Velocity = emitter.Velocity;
                nativeEmitter.InnerRadius = emitter.InnerRadius;
                nativeEmitter.InnerRadiusAngle = emitter.InnerRadiusAngle;
                nativeEmitter.ChannelCount = emitter.ChannelCount;
                nativeEmitter.ChannelRadius = emitter.ChannelRadius;
                nativeEmitter.ChannelAzimuthsPointer = emitter.ChannelAzimuths.ToPointer();
                nativeEmitter.VolumeCurvePointer = emitter.VolumeCurve.ToPointer();
                nativeEmitter.LFECurvePointer = emitter.LFECurve.ToPointer();
                nativeEmitter.LPFDirectCurvePointer = emitter.LPFDirectCurve.ToPointer();
                nativeEmitter.LPFReverbCurvePointer = emitter.LPFReverbCurve.ToPointer();
                nativeEmitter.ReverbCurvePointer = emitter.ReverbCurve.ToPointer();
                nativeEmitter.CurveDistanceScaler = emitter.CurveDistanceScaler;
                nativeEmitter.DopplerScaler = emitter.DopplerScaler;

                if (listener.Cone.HasValue)
                {
                    listenerCone = listener.Cone.Value;
                    nativeListener.ConePointer = new IntPtr(&listenerCone);
                }
                else
                {
                    nativeListener.ConePointer = IntPtr.Zero;
                }
                nativeListener.OrientFront = listener.OrientFront;
                nativeListener.OrientTop = listener.OrientTop;
                nativeListener.Position = listener.Position;
                nativeListener.Velocity = listener.Velocity;

                fixed (X3DAudioHandle* handle = &m_x3dAudioHandle)
                {
                    X3DAudioCalculate_(handle, &nativeListener, &nativeEmitter, (int)flags, result);
                }
            }
        }

        public float Apply3D(SourceVoice voice, Listener listener, Emitter emitter, int srcChannels, int dstChannels, CalculateFlags flags, float maxDistance, float frequencyRatio, bool silent, bool use3DCalculation = true)
        {
            unsafe
            {
                DspSettings settings;

                int matrixCoefficientCount = srcChannels * dstChannels;

                float* matrixCoefficients = stackalloc float[matrixCoefficientCount];
                float* delay = stackalloc float[dstChannels];

                settings.SrcChannelCount = srcChannels;
                settings.DstChannelCount = dstChannels;
                settings.MatrixCoefficientsPointer = new IntPtr(matrixCoefficients);
                settings.DelayTimesPointer = new IntPtr(delay);

                if (use3DCalculation)
                {
                    Calculate(listener, emitter, flags, &settings);
                    
                    voice.SetFrequencyRatio(frequencyRatio * settings.DopplerFactor);
                }
                else
                { //realistic sounds
                    settings.EmitterToListenerDistance = Vector3.Distance(new Vector3(listener.Position.X, listener.Position.Y, listener.Position.Z), new Vector3(emitter.Position.X, emitter.Position.Y, emitter.Position.Z));
                    for(int i = 0; i < matrixCoefficientCount; i++)
                        matrixCoefficients[i] = 1f;
                }

                if (emitter.InnerRadius == 0f)
                {
                    // approximated decay by distance
                    float decay;
                    if (silent)
                        decay = 0f;
                    else
                        decay = MathHelper.Clamp(1f - settings.EmitterToListenerDistance / maxDistance, 0f, 1f);
                    for (int i = 0; i < matrixCoefficientCount; i++)
                    {
                        matrixCoefficients[i] *= decay;
                    }
                }
#if !XB1
                voice.SetOutputMatrix(null, settings.SrcChannelCount, settings.DstChannelCount, matrixCoefficients);
#else // XB1
                var matCoefs = new float[matrixCoefficientCount];
                for (int i = 0; i < matrixCoefficientCount; i++)
                    matCoefs[i] = matrixCoefficients[i];
                voice.SetOutputMatrix(null, settings.SrcChannelCount, settings.DstChannelCount, matCoefs);
#endif // XB1
                return settings.EmitterToListenerDistance;
            }
        }
    }
}
