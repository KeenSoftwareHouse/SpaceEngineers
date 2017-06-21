using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using VRageMath;

namespace VRage.Utils
{
    /// <summary>
    /// Mean (average) filtering.
    /// </summary>
    public class MyAverageFiltering
    {
        private readonly List<double> m_samples;
        private readonly int m_sampleMaxCount;
        private int m_sampleCursor;
        private double? m_cachedFilteredValue;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sampleCount">Number of samples used in this mean filter.</param>
        public MyAverageFiltering(int sampleCount)
        {
            Debug.Assert(sampleCount > 0);
            m_sampleMaxCount = sampleCount;
            m_samples = new List<double>(sampleCount);
            m_cachedFilteredValue = null;
        }

        /// <summary>
        /// Add raw value to be filtered.
        /// </summary>
        public void Add(double value)
        {
            m_cachedFilteredValue = null;
            if (m_samples.Count < m_sampleMaxCount)
            {
                m_samples.Add(value);
            }
            else
            {
                m_samples[m_sampleCursor++] = value;
                if (m_sampleCursor >= m_sampleMaxCount)
                    m_sampleCursor = 0;
            }
        }

        /// <summary>
        /// Get filtered value.
        /// </summary>
        public double Get()
        {
            if (m_cachedFilteredValue.HasValue)
                return m_cachedFilteredValue.Value;

            double sampleSum = default(double);
            foreach (double sample in m_samples)
            {
                sampleSum = sampleSum + sample;
            }
            if (m_samples.Count > 0)
            {
                double rtnValue = sampleSum / m_samples.Count;
                m_cachedFilteredValue = rtnValue;
                return rtnValue;
            }
            else
            {
                return default(double);
            }
        }

        public void Clear()
        {
            m_samples.Clear();
            m_cachedFilteredValue = null;
        }
    }
}
