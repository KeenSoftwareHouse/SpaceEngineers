using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using VRageMath;

namespace VRage.Utils
{
    /// <summary>
    /// Provides a simple and efficient way of sampling a discrete probability distribution.
    /// Instances can be reused by calling the Prepare method every time you want to change the distribution.
    /// Sampling a value is O(1), while the storage requirements are O(N), where N is number of possible values
    /// </summary>
    public class MyDiscreteSampler
    {
        private struct SamplingBin
        {
            public float Split;
            public int BinIndex;
            public int Donator;

            public override string ToString()
            {
                return "[" + BinIndex + "] <- (" + Donator + ") : " + Split;
            }
        }

        private class BinComparer : IComparer<SamplingBin>
        {
            public static BinComparer Static = new BinComparer();

            public int Compare(SamplingBin x, SamplingBin y)
            {
                float diff = x.Split - y.Split;
                if (diff < 0) return -1;
                if (diff > 0) return 1;
                return 0;
            }
        }

        private SamplingBin[] m_bins;
        private int m_binCount;
        private bool m_initialized;

        public MyDiscreteSampler()
        {
            m_binCount = 0;
            m_bins = null;
            m_initialized = false;
        }

        public MyDiscreteSampler(int prealloc)
            : this()
        {
            m_bins = new SamplingBin[prealloc];
        }

        /// The list supplied to the method does not have to add up to 1.0f, that's why it's called "densities" instead of "probabilities"
        public void Prepare(List<float> densities)
        {
            if (m_bins == null || m_binCount < densities.Count)
            {
                m_bins = new SamplingBin[densities.Count];
            }

            m_binCount = densities.Count;

            float cumul = 0.0f;
            foreach (var d in densities)
            {
                cumul += d;
            }

            float normalizationFactor = (float)m_binCount / cumul;
            for (int i = 0; i < m_binCount; i++)
            {
                m_bins[i].BinIndex = i;
                m_bins[i].Split = densities[i] * normalizationFactor;
                m_bins[i].Donator = 0;
            }

            Array.Sort<SamplingBin>(m_bins, 0, m_binCount, BinComparer.Static);

            int receiver = 0;
            int lower = 1;
            int upper = m_binCount - 1;

            while (lower <= upper)
            {
                Debug.Assert(m_bins[upper].Split >= 1.0f);
                Debug.Assert(m_bins[receiver].Split <= 1.00001f);

                m_bins[receiver].Donator = m_bins[upper].BinIndex;
                m_bins[upper].Split -= 1.0f - m_bins[receiver].Split;

                if (m_bins[upper].Split < 1.0f)
                {
                    receiver = upper;
                    upper--;
                }
                else
                {
                    receiver = lower;
                    lower++;
                }
            }

            Debug.Assert(lower == upper + 1);

            m_initialized = true;
        }

        public int Sample()
        {
            Debug.Assert(m_initialized && m_bins != null, "Sampling fron an uninitialized sampler!");
            int entryIndex = MyUtils.GetRandomInt(m_binCount);
            var entry = m_bins[entryIndex];
            if (MyUtils.GetRandomFloat() <= entry.Split)
            {
                return entry.BinIndex;
            }
            else
            {
                return entry.Donator;
            }
        }
    }
}
