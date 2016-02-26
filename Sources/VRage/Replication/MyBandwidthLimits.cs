using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.Network;

namespace VRage.Replication
{
    public class MyBandwidthLimits
    {
        private Dictionary<StateGroupEnum, Ref<int>> m_tmpBandwidthCounters = new Dictionary<StateGroupEnum, Ref<int>>();
        private Dictionary<StateGroupEnum, int> m_limits = new Dictionary<StateGroupEnum, int>();

        /// <summary>
        /// Gets current limit for group (how many bits can be written per frame).
        /// Return zero when there's no limit.
        /// </summary>
        public int GetLimit(StateGroupEnum group)
        {
            return m_limits.GetValueOrDefault(group);
        }

        /// <summary>
        /// Sets limit for group (how many bits can be written per frame).
        /// It's ensured that at least one item is sent every frame.
        /// Setting limit to zero disables limit.
        /// </summary>
        public void SetLimit(StateGroupEnum group, int bitsInMessage)
        {
            if (bitsInMessage > 0)
                m_limits[group] = bitsInMessage;
            else
                m_limits.Remove(group);
        }

        public bool Add(StateGroupEnum group, int bitCount)
        {
            int limit = m_limits.GetValueOrDefault(group);
            Ref<int> transferred;
            if (!m_tmpBandwidthCounters.TryGetValue(group, out transferred))
            {
                transferred = new Ref<int>();
                m_tmpBandwidthCounters[group] = transferred;
            }

            bool forceSend = limit == 0 || transferred.Value == 0;
            if (limit == 0 || transferred.Value < limit) // When limit is zero or nothing from this group has transfered or we're under limit.
            {
                // Increase transferred even when we potencially go over limit and return false (we don't want small messages to starve large ones)
                transferred.Value += bitCount;
            }
            return forceSend || transferred.Value <= limit;
        }

        public void Clear()
        {
            foreach (var entry in m_tmpBandwidthCounters)
            {
                entry.Value.Value = 0;
            }
        }
    }
}
