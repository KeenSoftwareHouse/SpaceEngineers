using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SharpDX.X3DAudio;
using SharpDX.XAudio2;
using SharpDX.Multimedia;
using VRageMath;

namespace Sandbox.Engine.Audio
{
    class MyX3DAudio
    {
        X3DAudio m_x3dAudio;
        DspSettings m_dsp;

        public MyX3DAudio(WaveFormatExtensible format)
        {
            m_x3dAudio = new X3DAudio(format.ChannelMask);
            m_dsp = new DspSettings(1, format.Channels);
        }

        public void Apply3D(SourceVoice voice, Listener listener, Emitter emitter, float maxDistance, float frequencyRatio)
        {
            m_x3dAudio.Calculate(listener, emitter, CalculateFlags.Matrix | CalculateFlags.Doppler, m_dsp);

            if (emitter.InnerRadius == 0f)
            {
                // approximated decay by distance
                float decay = MathHelper.Clamp(1f - m_dsp.EmitterToListenerDistance / maxDistance, 0f, 1f);
                for (int i = 0; i < m_dsp.MatrixCoefficients.Length; ++i)
                {
                    m_dsp.MatrixCoefficients[i] *= decay;
                }
            }

            voice.SetOutputMatrix(m_dsp.SourceChannelCount, m_dsp.DestinationChannelCount, m_dsp.MatrixCoefficients);
            voice.SetFrequencyRatio(frequencyRatio * m_dsp.DopplerFactor);
        }
    }
}
