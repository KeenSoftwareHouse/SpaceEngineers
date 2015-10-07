using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using VRage.Library.Collections;
using VRage.Network;

namespace VRage.Replication
{
    public class MyEventBuffer : IDisposable
    {
        public delegate void Handler(BitStream stream, NetworkId objectInstance, uint eventId, EndpointId sender);

        class BufferedEvent
        {
            public BitStream Stream = new BitStream(32);
            public NetworkId ObjectInstance;
            public uint EventId;
            public EndpointId Sender;
        }

        static Handler m_emptyHandler = (s, o, e, se) => { };

        Stack<BufferedEvent> m_eventPool;
        Stack<List<BufferedEvent>> m_listPool;
        Dictionary<NetworkId, List<BufferedEvent>> m_buffer = new Dictionary<NetworkId, List<BufferedEvent>>(16);

        public MyEventBuffer(int eventCapacity = 32)
        {
            m_listPool = new Stack<List<BufferedEvent>>(16);
            for (int i = 0; i < 16; i++)
            {
                m_listPool.Push(new List<BufferedEvent>(16));
            }

            m_eventPool = new Stack<BufferedEvent>(eventCapacity);
            for (int i = 0; i < eventCapacity; i++)
            {
                m_eventPool.Push(new BufferedEvent());
            }
        }

        public void Dispose()
        {
            foreach(var e in m_eventPool)
            {
                e.Stream.Dispose();
            }
            m_eventPool.Clear();
            foreach(var b in m_buffer)
            {
                foreach(var e in b.Value)
                {
                    e.Stream.Dispose();
                }
            }
            m_buffer.Clear();
        }

        BufferedEvent ObtainEvent()
        {
            if (m_eventPool.Count > 0)
                return m_eventPool.Pop();
            else
                return new BufferedEvent();
        }

        void ReturnEvent(BufferedEvent evnt)
        {
            m_eventPool.Push(evnt);
        }

        List<BufferedEvent> ObtainList()
        {
            if (m_listPool.Count > 0)
                return m_listPool.Pop();
            else
                return new List<BufferedEvent>(16);
        }

        void ReturnList(List<BufferedEvent> list)
        {
            Debug.Assert(list.Count == 0);
            m_listPool.Push(list);
        }

        public void EnqueueEvent(BitStream stream, NetworkId objectInstance, uint eventId, EndpointId sender)
        {
            int requiredByteSize = stream.ByteLength - stream.BytePosition + 1;

            var e = ObtainEvent();
            e.Stream.ResetWrite();
            e.Stream.WriteBitStream(stream);
            e.Stream.ResetRead();
            e.ObjectInstance = objectInstance;
            e.EventId = eventId;
            e.Sender = sender;

            List<BufferedEvent> events;
            if (!m_buffer.TryGetValue(objectInstance, out events))
            {
                events = ObtainList();
                m_buffer.Add(objectInstance, events);
            }
            events.Add(e);
        }

        public void RemoveEvents(NetworkId objectInstance)
        {
            // To correctly return everything into pools
            ProcessEvents(objectInstance, m_emptyHandler);
        }

        public void ProcessEvents(NetworkId objectInstance, Handler handler)
        {
            List<BufferedEvent> events;
            if(m_buffer.TryGetValue(objectInstance, out events))
            {
                foreach(var e in events)
                {
                    handler(e.Stream, e.ObjectInstance, e.EventId, e.Sender);
                    ReturnEvent(e);
                }
                events.Clear();
                ReturnList(events);
            }
            m_buffer.Remove(objectInstance);
        }
    }
}
