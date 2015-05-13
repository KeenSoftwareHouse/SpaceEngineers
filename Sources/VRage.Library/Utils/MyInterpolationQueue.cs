using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using VRage.Collections;

namespace VRage.Library.Utils
{
    public delegate void InterpolationHandler<T>(T item1, T item2, float interpolator, out T result);

    public class MyInterpolationQueue<T>
    {
        struct Item
        {
            public T Userdata;
            public MyTimeSpan Timestamp;

            public Item(T userdata, MyTimeSpan timespan)
            {
                Userdata = userdata;
                Timestamp = timespan;
            }
        }

        MyQueue<Item> m_queue;
        InterpolationHandler<T> m_interpolator;
        MyTimeSpan m_lastTimeStamp = MyTimeSpan.Zero;

        public MyTimeSpan LastSample
        {
            get { return m_lastTimeStamp; }
        }

        public int Count
        {
            get { return m_queue.Count; }
        }

        public MyInterpolationQueue(int defaultCapacity, InterpolationHandler<T> interpolator)
        {
            m_queue = new MyQueue<Item>(defaultCapacity);
            m_interpolator = interpolator;
        }

        /// <summary>
        /// Discards old samples, keeps at least 2 samples to be able to interpolate or extrapolate.
        /// </summary>
        public void DiscardOld(MyTimeSpan currentTimestamp)
        {
            int discardCount = -1;

            for (int i = 0; i < m_queue.Count; i++)
            {
                if (m_queue[i].Timestamp < currentTimestamp)
                    discardCount++;
                else
                    break;
            }

            for (int i = 0; i < discardCount && m_queue.Count > 2; i++)
            {
                m_queue.Dequeue();
            }
        }

        public void Clear()
        {
            m_queue.Clear();
            m_lastTimeStamp = MyTimeSpan.Zero;
        }

        /// <summary>
        /// Adds sample with timestamp, it must be larger than last timestamp!
        /// </summary>
        public void AddSample(ref T item, MyTimeSpan sampleTimestamp)
        {
            if (sampleTimestamp < m_lastTimeStamp)
            {
                //Debug.Fail("Adding sample out of order!");
                return;
            }

            if (sampleTimestamp == m_lastTimeStamp && m_queue.Count > 0)
            {
                m_queue[m_queue.Count - 1] = new Item(item, sampleTimestamp);
            }
            else
            {
                m_queue.Enqueue(new Item(item, sampleTimestamp));
                m_lastTimeStamp = sampleTimestamp;
            }
        }

        /// <summary>
        /// Discards old frame (keeps one older) and interpolates between two samples using interpolator.
        /// Returns interpolator
        /// There must be at least one sample!
        /// </summary>
        public float Interpolate(MyTimeSpan currentTimestamp, out T result)
        {
            Debug.Assert(m_queue.Count > 0, "Queue is empty, there must be at least one sample!");

            DiscardOld(currentTimestamp);

            if (m_queue.Count > 1)
            {
                Item lower = m_queue[0];
                Item upper = m_queue[1];

                float interpolator = (float)((currentTimestamp - lower.Timestamp).Seconds / (upper.Timestamp - lower.Timestamp).Seconds);

                m_interpolator(lower.Userdata, upper.Userdata, interpolator, out result);

                //return (float)((currentTimestamp - lower.Timestamp).Seconds / (m_queue[m_queue.Count -1].Timestamp - lower.Timestamp).Seconds);

                return interpolator;
            }
            else
            {
                result = m_queue[0].Userdata;
                return 0;
            }
        }
    }
}
