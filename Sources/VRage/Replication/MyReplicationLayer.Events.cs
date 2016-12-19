using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using VRage.Library.Collections;
using VRage.Replication;
using VRage.Serialization;
using VRage.Utils;

namespace VRage.Network
{
    public abstract partial class MyReplicationLayer : MyReplicationLayerBase, INetObjectResolver
    {
        /// <summary>
        /// Called when event is raised locally to send it to other peer(s).
        /// Return true to invoke event locally.
        /// </summary>
        /// <remarks>
        /// Invoking event locally is important to be done AFTER event is sent to other peers, 
        /// because invocation can raise another event and order must be preserved.
        /// Local event invocation is done in optimized way without unnecessary deserialization.
        /// </remarks>
        internal abstract bool DispatchEvent(BitStream stream, CallSite site, EndpointId recipient, IMyNetObject eventInstance, float unreliablePriority);

        internal abstract bool DispatchBlockingEvent(BitStream stream, CallSite site, EndpointId recipient, IMyNetObject eventInstance, IMyNetObject blockedNetObject, float unreliablePriority);

        /// <summary>
        /// Called when event is received over network.
        /// Event can be validated, invoked and/or transferred to other peers.
        /// </summary>
        internal abstract void ProcessEvent(BitStream stream, CallSite site, object obj, IMyNetObject sendAs, EndpointId source);

        protected sealed override void DispatchEvent<T1, T2, T3, T4, T5, T6, T7, T8>(CallSite callSite, EndpointId recipient, float unreliablePriority, ref T1 arg1, ref T2 arg2, ref T3 arg3, ref T4 arg4, ref T5 arg5, ref T6 arg6, ref T7 arg7, ref T8 arg8)
        {
            IMyNetObject sendAs;
            NetworkId networkId;
            uint sendId = callSite.Id;

            if (callSite.MethodInfo.IsStatic)
            {
                Debug.Assert(arg1 == null, "First argument (the instance on which is event invoked) should be null for static events");
                sendAs = null;
                networkId = NetworkId.Invalid;
            }
            else if (arg1 == null)
            {
                throw new InvalidOperationException("First argument (the instance on which is event invoked) cannot be null for non-static events");
            }
            else if (arg1 is IMyEventProxy)
            {
                sendAs = GetProxyTarget((IMyEventProxy)arg1);

                if(sendAs == null)
                {
                    string msg = "Raising event on object which is not recognized by replication: " + arg1;
                    Debug.Fail(msg);
                    MyLog.Default.WriteLine(msg);
                    return;
                }

                sendId += (uint)m_typeTable.Get(sendAs.GetType()).EventTable.Count; // Add max id of Proxy
                networkId = GetNetworkIdByObject(sendAs);
                Debug.Assert(object.ReferenceEquals(GetProxyTarget(((IMyProxyTarget)sendAs).Target), sendAs), "There must be one-to-one relationship between IMyEventProxy and IMyEventTarget. Proxy.EventTarget.Target == Proxy");
            }
            else if (arg1 is IMyNetObject)
            {
                sendAs = (IMyNetObject)arg1;
                networkId = GetNetworkIdByObject(sendAs);
            }
            else
            {
                throw new InvalidOperationException("Instance events may be called only on IMyNetObject or IMyEventProxy");
            }

            NetworkId blockingNetworkId = NetworkId.Invalid;
            IMyNetObject blockedNetObj = null;
            if (arg8 is IMyEventProxy && callSite.IsBlocking)
            {
                blockedNetObj = GetProxyTarget((IMyEventProxy)arg8);
                blockingNetworkId = GetNetworkIdByObject(blockedNetObj);
            }
            else if (arg8 is IMyEventProxy && !callSite.IsBlocking)
            {
                throw new InvalidOperationException("Rising blocking event but event itself does not have Blocking attribute");
            }
            else if (!(arg8 is IMyEventProxy) && callSite.IsBlocking)
            {
                throw new InvalidOperationException("Event contain Blocking attribute but blocked event proxy is not set or raised event is not blocking one");
            }

            Debug.Assert(sendId <= 255, "Max 256 events are supported per hierarchy");

            m_sendStreamEvent.ResetWrite();
            m_sendStreamEvent.WriteNetworkId(networkId);
            m_sendStreamEvent.WriteNetworkId(blockingNetworkId);
            m_sendStreamEvent.WriteByte((byte)sendId); // TODO: Compress eventId to necessary number of bits

            var site = (CallSite<T1, T2, T3, T4, T5, T6, T7>)callSite;
            using (MySerializerNetObject.Using(this))
            {
                site.Serializer(arg1, m_sendStreamEvent, ref arg2, ref arg3, ref arg4, ref arg5, ref arg6, ref arg7);
            }

            m_sendStreamEvent.Terminate();

            bool dispatchRes = false;
            // If blocking event, than process a little differently. (Internally it will call DispatchEvent anyway)
            if (!blockingNetworkId.IsInvalid)
                dispatchRes = DispatchBlockingEvent(m_sendStreamEvent, callSite, recipient, sendAs, blockedNetObj, unreliablePriority);
            else
                dispatchRes = DispatchEvent(m_sendStreamEvent, callSite, recipient, sendAs, unreliablePriority);

            if (dispatchRes)
            {
                InvokeLocally(site, arg1, arg2, arg3, arg4, arg5, arg6, arg7);
            }
        }

        /// <summary>
        /// Reads arguments from stream and invokes event. Returns false when validation failed, otherwise true.
        /// </summary>
        public bool Invoke(CallSite callSite, BitStream stream, object obj, EndpointId source, MyClientStateBase clientState, bool validate)
        {
            using (MySerializerNetObject.Using(this))
            using (MyEventContext.Set(source, clientState, validate))
            {
                return callSite.Invoke(stream, obj, validate) && !(validate && MyEventContext.Current.HasValidationFailed);
            }
        }

        public void ProcessEvent(MyPacket packet)
        {
            ReceiveStream.ResetRead(packet);
            ProcessEvent(ReceiveStream, packet.Sender);
        }

        void ProcessEvent(BitStream stream, EndpointId sender)
        {
            NetworkId networkId = stream.ReadNetworkId();
            NetworkId blockedNetworkId = stream.ReadNetworkId();
            uint eventId = (uint)stream.ReadByte(); // TODO: Compress eventId to necessary number of bits

            ProcessEvent(stream, networkId, blockedNetworkId, eventId, sender);
        }

        protected virtual void ProcessEvent(BitStream stream, NetworkId networkId, NetworkId blockedNetId, uint eventId, EndpointId sender)
        {
            CallSite site;
            IMyNetObject sendAs;
            object obj;
            if (networkId.IsInvalid) // Static event
            {
                site = m_typeTable.StaticEventTable.Get(eventId);
                sendAs = null;
                obj = null;
            }
            else // Instance event
            {
                sendAs = GetObjectByNetworkId(networkId);
                if(sendAs == null)
                {
                    return;
                }
                var typeInfo = m_typeTable.Get(sendAs.GetType());
                int eventCount = typeInfo.EventTable.Count;
                if (eventId < eventCount) // Directly
                {
                    obj = sendAs;
                    site = typeInfo.EventTable.Get(eventId);
                }
                else // Through proxy
                {
                    obj = ((IMyProxyTarget)sendAs).Target;
                    typeInfo = m_typeTable.Get(obj.GetType());
                    site = typeInfo.EventTable.Get(eventId - (uint)eventCount); // Subtract max id of Proxy
                    Debug.Assert(object.ReferenceEquals(GetProxyTarget(((IMyProxyTarget)sendAs).Target), sendAs), "There must be one-to-one relationship between IMyEventProxy and IMyEventTarget. Proxy.EventTarget.Target == Proxy");
                }
            }

            ProcessEvent(stream, site, obj, sendAs, sender);
        }

        void INetObjectResolver.Resolve<T>(BitStream stream, ref T obj)
        {
            if (stream.Reading)
            {
                obj = (T)GetObjectByNetworkId(stream.ReadNetworkId());
            }
            else
            {
                NetworkId id;
                stream.WriteNetworkId(TryGetNetworkIdByObject(obj, out id) ? id : NetworkId.Invalid);
            }
        }
    }
}
