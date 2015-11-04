using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.Library.Utils;
using VRageMath;

namespace VRage.Noise
{
    public class MyCompositeNoise : MyModule
    {
        IMyModule[] m_noises = null;
        float[] m_amplitudeScales = null;
        float m_normalizationFactor = 1.0f;

        int m_numNoises = 0;

        public MyCompositeNoise(int numNoises,float startFrequency)
        {
            m_numNoises = numNoises;
            m_noises = new IMyModule[m_numNoises];
            m_amplitudeScales = new float[m_numNoises];
            m_normalizationFactor = 2.0f - 1.0f / (float)Math.Pow(2, m_numNoises - 1);

            float frequency = startFrequency;
            for (int i = 0; i < m_numNoises; ++i)
            {
                m_amplitudeScales[i] = 1.0f / (float)Math.Pow(2.0f, i);
                m_noises[i] = new MySimplexFast(seed: MyRandom.Instance.Next(), frequency: frequency);
                frequency *= 2.01f;
            }

        }

        double NormalizeValue(double value)
        {
            return 0.5 * value/ m_normalizationFactor + 0.5;
        }

        public override double GetValue(double x)
        {
            double value = 0.0;
            for (int i = 0; i < m_numNoises; ++i)
            {
                value += m_amplitudeScales[i] * m_noises[i].GetValue(x);
            }
            return NormalizeValue(value);
        }

        public override double GetValue(double x, double y)
        {
            double value = 0.0;
            for (int i = 0; i < m_numNoises; ++i)
            {
                value += m_amplitudeScales[i] * m_noises[i].GetValue(x,y);
            }
            return NormalizeValue(value);
        }

        public override double GetValue(double x, double y, double z)
        {
            double value = 0.0;
            for (int i = 0; i < m_numNoises; ++i)
            {
                value += m_amplitudeScales[i] * m_noises[i].GetValue(x, y,z);
            }
            return NormalizeValue(value);
        }

        public float GetValue(double x, double y, double z,int numNoises)
        {
            double value = 0.0;
            for (int i = 0; i < numNoises; ++i)
            {
                value += m_amplitudeScales[i] * m_noises[i].GetValue(x, y, z);
            }
            return (float)(0.5 * value + 0.5);
        }
    }
}
