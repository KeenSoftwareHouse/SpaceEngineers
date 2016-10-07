using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using ParallelTasks;

namespace VRage.Library.Algorithms
{
    /// <summary>
    /// Generates IDs sequentially and reuses old IDs which are returned to pool by calling Return method.
    /// Protection count and time can be set to protect returned IDs. 
    /// Protection is useful especially in multiplayer where clients can still have objects with those IDs.
    /// </summary>
    public class SequenceIdGenerator
    {
        struct Item
        {
            public uint Id;
            public uint Time;

            public Item(uint id, uint time)
            {
                Id = id;
                Time = time;
            }
        }

        /// <summary>
        /// Max used id, zero is reserved and never used.
        /// </summary>
        uint m_maxId = 0;

        Queue<Item> m_reuseQueue;

        /// <summary>
        /// Minimal number of items in reuse queue until first item can be taken.
        /// </summary>
        int m_protecionCount;

        /// <summary>
        /// Minimal time if item spent in reuse queue until it can be returned.
        /// Units are arbitrary
        /// </summary>
        uint m_reuseProtectionTime;

        /// <summary>
        /// Function which returns current time, units are arbitrary and same as reuse protection time.
        /// </summary>
        Func<uint> m_timeFunc;

        SpinLockRef m_lock = new SpinLockRef();

        public int WaitingInQueue
        {
            get { return m_reuseQueue.Count; }
        }

        /// <summary>
        /// Number of reserved ids, zero is also reserved, but not counted.
        /// </summary>
        public uint ReservedCount { get; private set; }

        public SequenceIdGenerator(int reuseProtectionCount = 2048, uint reuseProtectionTime = 60, Func<uint> timeFunc = null)
        {
            m_reuseQueue = new Queue<Item>(reuseProtectionCount);
            m_protecionCount = Math.Max(0, reuseProtectionCount);
            m_reuseProtectionTime = reuseProtectionTime;
            m_timeFunc = timeFunc;
        }

        /// <summary>
        /// Creates new sequence id generator with stopwatch to measure protection time.
        /// </summary>
        /// <param name="reuseProtectionTime">Time to protect returned IDs.</param>
        /// <param name="reuseProtectionCount">Minimum number of IDs in protection queue, before first ID will be reused.</param>
        public static SequenceIdGenerator CreateWithStopwatch(TimeSpan reuseProtectionTime, int reuseProtectionCount = 2048)
        {
            var sw = Stopwatch.StartNew();
            if (reuseProtectionTime.TotalSeconds > 5) // If bigger than 5 seconds, store just seconds
                return new SequenceIdGenerator(reuseProtectionCount, (uint)reuseProtectionTime.TotalSeconds, () => (uint)(sw.Elapsed.TotalSeconds));
            else if (reuseProtectionTime.TotalMilliseconds > 500) // If bigger than 500 ms, store just hundreds of ms
                return new SequenceIdGenerator(reuseProtectionCount, (uint)(reuseProtectionTime.TotalSeconds * 10), () => (uint)(sw.Elapsed.TotalSeconds * 10));
            else if (reuseProtectionTime.TotalMilliseconds > 50) // If bigger than 50 ms, store just tens of ms, overflow in 490 days
                return new SequenceIdGenerator(reuseProtectionCount, (uint)(reuseProtectionTime.TotalSeconds * 100), () => (uint)(sw.Elapsed.TotalSeconds * 100));
            else // Otherwise store milliseconds, overflow in 49 days
                return new SequenceIdGenerator(reuseProtectionCount, (uint)(reuseProtectionTime.TotalMilliseconds), () => (uint)(sw.Elapsed.TotalMilliseconds));
        }

        /// <summary>
        /// Reserves first several IDs, so it's never returned by generator.
        /// Zero is never returned, when reservedIdCount is 2, IDs 1 and 2 won't be ever returned.
        /// </summary>
        /// <param name="reservedIdCount">Number of reserved IDs which will be never returned by generator.</param>
        public void Reserve(uint reservedIdCount)
        {
            if (m_maxId != 0)
                throw new InvalidOperationException("Reserve can be called only once and before any IDs are generated.");

            m_maxId = reservedIdCount;
            ReservedCount = reservedIdCount;
        }

        bool CheckFirstItemTime()
        {
            if (m_timeFunc == null)
                return true;

            uint now = m_timeFunc();
            uint itemTime = m_reuseQueue.Peek().Time;
            if (now < itemTime)
            {
                // Time overflow, reset whole queue
                int num = m_reuseQueue.Count;
                for (int i = 0; i < num; i++)
                {
                    var item = m_reuseQueue.Dequeue();
                    item.Time = now;
                    m_reuseQueue.Enqueue(item);
                }
                return false;
            }
            return itemTime + (ulong)m_reuseProtectionTime < (ulong)now;
        }

        public uint NextId()
        {
            using (m_lock.Acquire())
            {
                if (m_reuseQueue.Count > m_protecionCount && CheckFirstItemTime())
                    return m_reuseQueue.Dequeue().Id;
                else
                    return ++m_maxId;
            }
        }

        public void Return(uint id)
        {
            m_reuseQueue.Enqueue(new Item(id, m_timeFunc()));
        }
    }
}
