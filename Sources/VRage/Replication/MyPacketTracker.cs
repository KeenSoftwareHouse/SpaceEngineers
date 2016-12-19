using System.Collections.Generic;

namespace VRage.Replication
{
    public class MyPacketTracker
    {
        private const int BUFFER_LENGTH = 5;
        private readonly List<byte> m_ids = new List<byte>();

        public MyPacketStatistics Statistics { get; set; }

        public enum OrderType
        {
            InOrder,
            OutOfOrder,
            Duplicate,
            Drop1,
            Drop2,
            Drop3,
            Drop4,
            DropX
        }

        public OrderType Add(byte id)
        {
            if (m_ids.Count == 1 && id == (byte) (m_ids[0] + 1))
            {
                m_ids[0] = id;
                return OrderType.InOrder;
            }
            if (m_ids.FindIndex(x => x == id) != -1)
                return OrderType.Duplicate;

            m_ids.Add(id);

            for (var i = 2; i < m_ids.Count; i++)
                if ((byte)(m_ids[0] + 1) == m_ids[i])
                {
                    m_ids.RemoveAt(i);
                    m_ids.RemoveAt(0);
                    CleanUp();
                    
                    return OrderType.OutOfOrder;
                }

            if (m_ids.Count >= BUFFER_LENGTH)
            {
                int gapStart = m_ids[0];
                m_ids.RemoveAt(0);
                int min = m_ids[0];
                CleanUp();
                var count = (byte)(min - gapStart) - 2;
                return OrderType.Drop1 + count;
            }
            return OrderType.InOrder;
        }

        private void CleanUp()
        {
            byte last = 0;
            bool first = true;
            bool sequence = true;
            foreach (var j in m_ids)
            {
                sequence &= first || ((byte) (last + 1)) == j;
                last = j;
                first = false;
            }
            if (sequence)
                m_ids.RemoveRange(0, m_ids.Count - 1);
        }
    }
}
