using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using VRage.Library.Collections;
using VRage.Network;
using VRage.Collections;

namespace VRage.Replication
{
    public class MyEventsBuffer : IDisposable
    {
        public delegate void Handler(BitStream stream, NetworkId objectInstance, NetworkId blockedNetId, uint eventId, EndpointId sender);
        public delegate bool IsBlockedHandler(NetworkId objectInstance, NetworkId blockedNetId);

        class MyBufferedEvent
        {
            /// <summary>
            /// Stream with the event
            /// </summary>
            public BitStream Stream = new BitStream(32);
            /// <summary>
            /// Target object net id of the event.
            /// </summary>
            public NetworkId TargetObjectId;
            /// <summary>
            /// Object network id that is blocking this event. If 'NetworkId.Invalid' than no blocking object.
            /// </summary>
            public NetworkId BlockingObjectId;
            public uint EventId;
            public EndpointId Sender;
            /// <summary>
            /// Indicates if this event is a barrier.
            /// </summary>
            public bool IsBarrier = false;
        }

        struct MyObjectEventsBuffer
        {
            /// <summary>
            /// Events to be processed for network object.
            /// </summary>
            public Queue<MyBufferedEvent> Events;
            /// <summary>
            /// Indicates if events are currently processed.
            /// </summary>
            public bool IsProcessing;
        }

        static Handler m_emptyHandler = (s, o, o2, e, se) => { };
        static IsBlockedHandler m_emptyBlockerHandler = (o, o2) => { return false; };

        Stack<MyBufferedEvent> m_eventPool;
        Stack<Queue<MyBufferedEvent>> m_listPool;
        Dictionary<NetworkId, MyObjectEventsBuffer> m_buffer = new Dictionary<NetworkId, MyObjectEventsBuffer>(16);

        public MyEventsBuffer(int eventCapacity = 32)
        {
            m_listPool = new Stack<Queue<MyBufferedEvent>>(16);
            for (int i = 0; i < 16; i++)
            {
                m_listPool.Push(new Queue<MyBufferedEvent>(16));
            }

            m_eventPool = new Stack<MyBufferedEvent>(eventCapacity);
            for (int i = 0; i < eventCapacity; i++)
            {
                m_eventPool.Push(new MyBufferedEvent());
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
                foreach(var e in b.Value.Events)
                {
                    e.Stream.Dispose();
                }
            }
            m_buffer.Clear();
        }

        MyBufferedEvent ObtainEvent()
        {
            if (m_eventPool.Count > 0)
                return m_eventPool.Pop();
            else
                return new MyBufferedEvent();
        }

        void ReturnEvent(MyBufferedEvent evnt)
        {
            m_eventPool.Push(evnt);
        }

        Queue<MyBufferedEvent> ObtainList()
        {
            if (m_listPool.Count > 0)
                return m_listPool.Pop();
            else
                return new Queue<MyBufferedEvent>(16);
        }

        void ReturnList(Queue<MyBufferedEvent> list)
        {
            Debug.Assert(list.Count == 0);
            m_listPool.Push(list);
        }

        /// <summary>
        /// Enqueues event that have to be done on target object.
        /// </summary>
        /// <param name="stream">Stream with event data.</param>
        /// <param name="targetObjectId">Object id that is a target of the event.</param>
        /// <param name="blockingObjectId">Object id that is blocking target to be processed. 'NetworkId.Invalid' if none.</param>
        /// <param name="eventId">Event id.</param>
        /// <param name="sender">Endpoint.</param>
        public void EnqueueEvent(BitStream stream, NetworkId targetObjectId, NetworkId blockingObjectId, uint eventId, EndpointId sender)
        {
            int requiredByteSize = stream.ByteLength - stream.BytePosition + 1;

            var e = ObtainEvent();
            e.Stream.ResetWrite();
            e.Stream.WriteBitStream(stream);
            e.Stream.ResetRead();
            e.TargetObjectId = targetObjectId;
            e.BlockingObjectId = blockingObjectId;
            e.EventId = eventId;
            e.Sender = sender;
            e.IsBarrier = false;

            MyObjectEventsBuffer eventBuffer;
            if (!m_buffer.TryGetValue(targetObjectId, out eventBuffer))
            {
                eventBuffer = new MyObjectEventsBuffer();
                eventBuffer.Events = ObtainList();
                m_buffer.Add(targetObjectId, eventBuffer);
            }
            eventBuffer.IsProcessing = false;
            eventBuffer.Events.Enqueue(e);

        }

        /// <summary>
        /// Enqueues barrier for an entity that is targeting network object with blocking event. WARNING: Have to be in
        /// pair with blocking event!
        /// </summary>
        /// <param name="targetObjectId">Network object id that will get barrier event.</param>
        /// <param name="blockingObjectId">Network object that have blocking event.</param>
        public void EnqueueBarrier(NetworkId targetObjectId, NetworkId blockingObjectId)
        {
            var e = ObtainEvent();
            e.TargetObjectId = targetObjectId;
            e.BlockingObjectId = blockingObjectId;
            e.IsBarrier = true;

            MyObjectEventsBuffer eventsBuffer;
            if (!m_buffer.TryGetValue(targetObjectId, out eventsBuffer))
            {
                eventsBuffer = new MyObjectEventsBuffer();
                eventsBuffer.Events = ObtainList();
                m_buffer.Add(targetObjectId, eventsBuffer);
            }
            eventsBuffer.IsProcessing = false;
            eventsBuffer.Events.Enqueue(e);

        }

        /// <summary>
        /// Removes all events from target id.
        /// </summary>
        /// <param name="objectInstance">Target object network id.</param>
        public void RemoveEvents(NetworkId objectInstance)
        {
            // To correctly return everything into pools
            MyObjectEventsBuffer eventBuffer;
            if (m_buffer.TryGetValue(objectInstance, out eventBuffer))
            {
                foreach (MyBufferedEvent e in eventBuffer.Events)
                {
                    ReturnEvent(e);
                }
                eventBuffer.Events.Clear();
                ReturnList(eventBuffer.Events);
                eventBuffer.Events = null;

            }
            m_buffer.Remove(objectInstance);
        }

        /// <summary>
        /// Tries to lift barrier from target network object. If successfull, removes this barrier from
        /// target object events queue. Also barrier must be aiming target object id.
        /// </summary>
        /// <param name="targetObjectId">Target network object id.</param>
        /// <returns>True if barrier found on the top of target object events queue. Otherwise false.</returns>
        private bool TryLiftBarrier(NetworkId targetObjectId)
        {
            MyObjectEventsBuffer eventBuffer;
            if (m_buffer.TryGetValue(targetObjectId, out eventBuffer))
            {
                MyBufferedEvent firstEvent = eventBuffer.Events.Peek();
                // Check if first event is a barrier and if it is designed for target object id.
                if (firstEvent.IsBarrier && firstEvent.TargetObjectId.Equals(targetObjectId))
                {
                    eventBuffer.Events.Dequeue();
                    ReturnEvent(firstEvent);
                    return true;
                }
            }

            return false;
        }

        public bool ContainsEvents(NetworkId netId)
        {
            MyObjectEventsBuffer eventBuffer;
            if (m_buffer.TryGetValue(netId, out eventBuffer))
            {
                return eventBuffer.Events.Count > 0;
            }
            return false;
        }

        /// <summary>
        /// Tries to process events for prarticular object id (network id).
        /// </summary>
        /// <param name="targetObjectId">Target object network id.</param>
        /// <param name="eventHandler">Handler for processing events.</param>
        /// <param name="isBlockedHandler">Handler for checking if processing of events should be canceled.</param>
        /// <param name="caller">Parent Network id from which this is called. Set NetworkId.Invalid if no parent.</param>
        /// <returns>True if all sucessfull.</returns>
        public bool ProcessEvents(NetworkId targetObjectId, Handler eventHandler, IsBlockedHandler isBlockedHandler, NetworkId caller)
        {

            // Unblock it for now (may be blocked again in the handler).
            MyObjectEventsBuffer eventsBuffer;

            bool fullyProcessed = false;
            
            // Queue of network objects that have to be processed at the end. 
            Queue<NetworkId> postProcessQueue = new Queue<NetworkId>();

            if (!m_buffer.TryGetValue(targetObjectId, out eventsBuffer))
                return false;

            // Already processing so no need to do again.
            if (eventsBuffer.IsProcessing)
                return false;

            eventsBuffer.IsProcessing = true;

            bool result = this.ProcessEventsBuffer(eventsBuffer, targetObjectId, eventHandler, isBlockedHandler, caller, ref postProcessQueue);

            eventsBuffer.IsProcessing = false;

            if (!result)
            {
                return false;
            }

            // If there are still events here that means it is still blocked!
            if (eventsBuffer.Events.Count == 0)
            {
                ReturnList(eventsBuffer.Events);
                eventsBuffer.Events = null;
                fullyProcessed = true;
            }

            // If not fully processed it has to stay in the buffer
            if(fullyProcessed)
                m_buffer.Remove(targetObjectId);

            // Process network objects that are outside of processing stack.
            while (postProcessQueue.Count > 0)
            {
                NetworkId netId = postProcessQueue.Dequeue();
                ProcessEvents(netId, eventHandler, isBlockedHandler, targetObjectId);
            }

            return true;

        }

        private bool ProcessEventsBuffer(MyObjectEventsBuffer eventsBuffer, NetworkId targetObjectId, Handler eventHandler, 
            IsBlockedHandler isBlockedHandler, NetworkId caller, ref Queue<NetworkId> postProcessQueue)
        {
            while (eventsBuffer.Events.Count > 0)
            {

                bool success = true;
                MyBufferedEvent e = eventsBuffer.Events.Peek();

                if (e.IsBarrier)
                {
                    success = this.ProcessBarrierEvent(targetObjectId, e, eventHandler, isBlockedHandler);
                }
                else
                {
                    // If you have blocking entity id, than try to check if it has your barrier on top,
                    // If yes, than process yourself, and put the other id to process later.
                    // If no, than process that id first, as you cannot proceede without it.
                    if (e.BlockingObjectId.IsValid)
                    {
                        success = this.ProcessBlockingEvent(targetObjectId, e, caller, eventHandler, isBlockedHandler, ref postProcessQueue);
                    }
                    else
                    {
                        eventHandler(e.Stream, e.TargetObjectId, e.BlockingObjectId, e.EventId, e.Sender);
                    }

                    if (success)
                    {
                        e.Stream.CheckTerminator();
                        eventsBuffer.Events.Dequeue();
                        ReturnEvent(e);
                    }

                }

                if(!success)
                {
                    eventsBuffer.IsProcessing = false;
                    return false;
                }

            }

            return true;

        }

        /// <summary>
        /// Process barrier event.
        /// </summary>
        /// <param name="targetObjectId">Target of the barrier event.</param>
        /// <param name="eventToProcess">Event to process.</param>
        /// <param name="eventHandler">Handler for processing event.</param>
        /// <param name="isBlockedHandler">Handler for checking if processing of events should be canceled.</param>
        /// <returns>True if success.</returns>
        private bool ProcessBarrierEvent(NetworkId targetObjectId, MyBufferedEvent eventToProcess, Handler eventHandler, IsBlockedHandler isBlockedHandler)
        {
            if (isBlockedHandler(eventToProcess.TargetObjectId, eventToProcess.BlockingObjectId))
            {
                return false;
            }

            bool success = ProcessEvents(eventToProcess.BlockingObjectId, eventHandler, isBlockedHandler, targetObjectId);

            return success;
        }

        /// <summary>
        /// Process blocking event.
        /// </summary>
        /// <param name="targetObjectId">Target object id for which to process.</param>
        /// <param name="eventToProcess">Event to be processed.</param>
        /// <param name="caller">Parent Network id from which this is called. Set NetworkId.Invalid if no parent.</param>
        /// <param name="eventHandler">Handler for processing event.</param>
        /// <param name="isBlockedHandler">Handler for checking if processing of events should be canceled.</param>
        /// <param name="postProcessQueue">Queue that should be post processed.</param>
        /// <returns>True if was success.</returns>
        private bool ProcessBlockingEvent(NetworkId targetObjectId, MyBufferedEvent eventToProcess, NetworkId caller, Handler eventHandler,
            IsBlockedHandler isBlockedHandler, ref Queue<NetworkId> postProcessQueue)
        {
            if (isBlockedHandler(eventToProcess.TargetObjectId, eventToProcess.BlockingObjectId))
            {
                return false;
            }

            bool result = this.TryLiftBarrier(eventToProcess.BlockingObjectId);
            // If success than we can proceede with blocking event.
            // If not, that means barrier holder have some events before actual barrier. Then we need to process that first.
            if (result)
            {

                eventHandler(eventToProcess.Stream, eventToProcess.TargetObjectId, eventToProcess.BlockingObjectId, eventToProcess.EventId, eventToProcess.Sender);

                if (eventToProcess.BlockingObjectId.IsValid && !eventToProcess.BlockingObjectId.Equals(caller))
                    postProcessQueue.Enqueue(eventToProcess.BlockingObjectId);
            }
            else
            {
                bool success = ProcessEvents(eventToProcess.BlockingObjectId, eventHandler, isBlockedHandler, targetObjectId);
                return success;
            }

            return true;
        }

        #region Debug methods

        /// <summary>
        /// Gets events buffer statistics.
        /// </summary>
        /// <returns>Formatted events buffer statistics.</returns>
        public string GetEventsBufferStat()
        {
            StringBuilder bufferStats = new StringBuilder();

            bufferStats.AppendLine("Pending Events Buffer:");

            foreach(var networkId in m_buffer)
            {
                string networkIdInfo = "    NetworkId: " + networkId.Key + ", EventsCount: " + networkId.Value.Events.Count;
                bufferStats.AppendLine(networkIdInfo);
            }

            return bufferStats.ToString();
        }

        #endregion

    }
}
