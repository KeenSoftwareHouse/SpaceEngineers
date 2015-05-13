using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VRage.Generics
{
    /// <summary>
    /// Contains items of any type. Each item has weight (float value).
    /// Allows to get item based on weight.
    /// </summary>
    /// <typeparam name="T">The item type</typeparam>
    public class MyWeightDictionary<T>
    {
        Dictionary<T, float> m_data;
        float m_sum;

        /// <summary>
        /// Initializes a new instance of the MyWeightDictionary class.
        /// </summary>
        /// <param name="data">Dictionary with items and weights.</param>
        public MyWeightDictionary(Dictionary<T, float> data)
        {
            m_data = data;
            m_sum = 0;
            foreach (var e in data)
            {
                m_sum += e.Value;
            }
        }

        public int Count 
        { 
            get
            {
                return m_data.Count;
            }
        }

        /// <summary>
        /// Gets sum of weights.
        /// </summary>
        /// <returns>The sum of all weights.</returns>
        public float GetSum()
        {
            return m_sum;
        }

        /// <summary>
        /// Gets item based on weight.
        /// </summary>
        /// <param name="weightNormalized">Weight, value from 0 to 1.</param>
        /// <returns>The item.</returns>
        public T GetItemByWeightNormalized(float weightNormalized)
        {
            return GetItemByWeight(weightNormalized * m_sum);
        }

        /// <summary>
        /// Gets item based on weight.
        /// </summary>
        /// <param name="weight">Weight, value from 0 to sum.</param>
        /// <returns></returns>
        public T GetItemByWeight(float weight)
        {
            float sum = 0;
            T last = default(T);
            foreach (var e in m_data)
            {
                last = e.Key;
                sum += e.Value;
                if (sum > weight)
                    return last;
            }
            return last;
        }

        /// <summary>
        /// Gets random item based on weight.
        /// </summary>
        /// <returns>The item.</returns>
        public T GetRandomItem(Random rnd)
        {
            float val = (float)rnd.NextDouble() * m_sum;
            return GetItemByWeight(val);
        }
    }
}
