using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using VRage.Library.Utils;
using VRageMath;

namespace VRage.Utils
{
    /// <summary>
    /// A templated class for sampling from a set of objects with given probabilities. Uses MyDiscreteSampler.
    /// </summary>
    public class MyDiscreteSampler<T> : IEnumerable<T>
    {
        private T[] m_values;
        private MyDiscreteSampler m_sampler;

        public bool Initialized
        {
            get
            {
                return m_sampler.Initialized;
            }
        }

        public MyDiscreteSampler(T[] values, IEnumerable<float> densities)
        {
            Debug.Assert(values.Length == densities.Count(), "Count of densities does not correspond to the count of values!");
            m_values = new T[values.Length];
            Array.Copy(values, m_values, values.Length);
            m_sampler = new MyDiscreteSampler();
            m_sampler.Prepare(densities);
        }

        public MyDiscreteSampler(List<T> values, IEnumerable<float> densities)
        {
            Debug.Assert(values.Count == densities.Count(), "Count of densities does not correspond to the count of values!");
            m_values = new T[values.Count];
            for (int i = 0; i < values.Count; ++i)
            {
                m_values[i] = values[i];
            }
            m_sampler = new MyDiscreteSampler();
            m_sampler.Prepare(densities);
        }

        public MyDiscreteSampler(IEnumerable<T> values, IEnumerable<float> densities)
        {
            int count = values.Count();
            Debug.Assert(count == densities.Count(), "Count of densities does not correspond to the count of values!");
            m_values = new T[count];
            int i = 0;
            foreach (var value in values)
            {
                m_values[i] = value;
                i++;
            }
            m_sampler = new MyDiscreteSampler();
            m_sampler.Prepare(densities);
        }

        public MyDiscreteSampler(Dictionary<T, float> densities)
            : this(densities.Keys, densities.Values) { }

        public T Sample(MyRandom rng)
        {
            return m_values[m_sampler.Sample(rng)];
        }

        public T Sample(float sample)
        {
            return m_values[m_sampler.Sample(sample)];
        }

        public T Sample()
        {
            return m_values[m_sampler.Sample()];
        }

        public int Count { get { return m_values.Length; } }

        public IEnumerator<T> GetEnumerator()
        {
            return m_values.AsEnumerable().GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }

    /// <summary>
    /// Provides a simple and efficient way of sampling a discrete probability distribution as described in http://www.jstatsoft.org/v11/i03/paper
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

        public bool Initialized
        {
            get
            {
                return m_initialized;
            }
        }

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
        public void Prepare(IEnumerable<float> densities)
        {
            float cumul = 0.0f;
            int count = 0;
            foreach (var d in densities)
            {
                cumul += d;
                count++;
            }

            if (count == 0) return;

            float normalizationFactor = (float)count / cumul;

            AllocateBins(count);
            InitializeBins(densities, normalizationFactor);
            ProcessDonators();
            m_initialized = true;
        }

        private void InitializeBins(IEnumerable<float> densities, float normalizationFactor)
        {
            int i = 0;
            foreach (var density in densities)
            {
                m_bins[i].BinIndex = i;
                m_bins[i].Split = density * normalizationFactor;
                m_bins[i].Donator = 0;
                i++;
            }
            Debug.Assert(m_binCount == i);
            Array.Sort<SamplingBin>(m_bins, 0, m_binCount, BinComparer.Static);
        }

        private void AllocateBins(int numDensities)
        {
            if (m_bins == null || m_binCount < numDensities)
            {
                m_bins = new SamplingBin[numDensities];
            }

            m_binCount = numDensities;
        }

        private void ProcessDonators()
        {
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
        }

        public int Sample(MyRandom rng)
        {
            Debug.Assert(m_initialized && m_bins != null, "Sampling fron an uninitialized sampler!");
            int entryIndex = rng.Next(m_binCount);
            var entry = m_bins[entryIndex];
            if (rng.NextFloat() <= entry.Split)
            {
                return entry.BinIndex;
            }
            else
            {
                return entry.Donator;
            }
        }

        /**
         * Beware that Cestmir thinks this can be less precise if you have a billiard numbers.
         * 
         * He is probably right. So only use this version if you don't care.
         */
        public int Sample(float rate)
        {
            Debug.Assert(m_initialized && m_bins != null, "Sampling fron an uninitialized sampler!");

            if (rate == 1) rate = .999999f;

            double binSelect = m_binCount*rate;

            int entryIndex = (int)(binSelect);
            var entry = m_bins[entryIndex];

            var split = binSelect - entryIndex;

            if (split <= entry.Split)
            {
                return entry.BinIndex;
            }
            else
            {
                return entry.Donator;
            }
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
