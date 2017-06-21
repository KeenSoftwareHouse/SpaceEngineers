using System;
using System.Collections.Generic;
using VRage.Library.Collections;

namespace VRage.Network
{
    public class MyPacketQueue
    {
        class Item
        {
            public BitStream Stream;
            public float Priority;
            public EndpointId Recipient;
            public int Order;
        }

        class ItemComparer : IComparer<Item>
        {
            public int Compare(Item x, Item y)
            {
                // Highest priority first, when priority is same, lowest order is first.
                var d = x.Priority - y.Priority;
                return d > 0 ? 1 : (d < 0 ? -1 : y.Order - x.Order);
            }
        }

        int m_typicalMaxPacketSize;
        int m_maxOrder = 0;
        Stack<Item> m_cache = new Stack<Item>();
        List<Item> m_queue = new List<Item>();
        ItemComparer m_comparer = new ItemComparer();
        Action<BitStream, EndpointId> m_sender;

        public MyPacketQueue(int typicalMaxPacketSize, Action<BitStream, EndpointId> sender)
        {
            m_typicalMaxPacketSize = typicalMaxPacketSize;
            m_sender = sender;
        }

        Item GetItem(int capacity)
        {
            if (m_cache.Count > 0)
            {
                return m_cache.Pop();
            }
            else
            {
                return new Item() { Stream = new BitStream(Math.Max(capacity, m_typicalMaxPacketSize)) };
            }
        }

        /// <summary>
        /// Clears whole queue.
        /// </summary>
        public void Clear()
        {
            foreach (var item in m_queue)
            {
                m_cache.Push(item);
            }
            m_queue.Clear();
            m_maxOrder = 0;
        }

        /// <summary>
        /// Sends message, when broadcasting, recipient is peer who won't receive the message.
        /// </summary>
        public void Enqueue(BitStream stream, float priority, EndpointId recipient)
        {
            var item = GetItem(stream.ByteLength);
            item.Stream.ResetWrite(stream);
            item.Priority = priority;
            item.Recipient = recipient;
            item.Order = m_maxOrder++;
            m_queue.Add(item);
        }

        /// <summary>
        /// Sends packets in queue, sends no more than maxBytesToSend.
        /// Returns number of bytes sent.
        /// </summary>
        public int Send(int maxBytesToSend = int.MaxValue)
        {
            m_queue.Sort(m_comparer);

            int bytesSent = 0;
            while (m_queue.Count > 0)
            {
                Item item = m_queue[m_queue.Count - 1];
                if (bytesSent + item.Stream.BytePosition > maxBytesToSend)
                    break;

                Send(item);
                bytesSent += item.Stream.BytePosition;

                m_queue.RemoveAt(m_queue.Count - 1);
                m_cache.Push(item);
            }
            if (m_queue.Count == 0)
                m_maxOrder = 0;
            return bytesSent;
        }

        uint Send(Item item)
        {
            m_sender(item.Stream, item.Recipient);
            return (uint)item.Stream.BytePosition;
        }

        public void Dispose()
        {
            foreach (var item in m_cache)
            {
                item.Stream.Dispose();
            }
        }
    }
}
