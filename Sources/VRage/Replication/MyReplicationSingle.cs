using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using VRage.Network;

namespace VRage.Network
{
    public class MyReplicationSingle : MyReplicationLayerBase
    {
        private EndpointId m_localEndpoint;

        public MyReplicationSingle(EndpointId localEndpoint)
        {
            Debug.Assert(localEndpoint.IsValid, "localEndpoint even for singleplayer cannot be zero!");
            m_localEndpoint = localEndpoint;
        }

        protected override void DispatchEvent<T1, T2, T3, T4, T5, T6, T7, T8>(CallSite callSite, EndpointId recipient, float unreliablePriority, ref T1 arg1, ref T2 arg2, ref T3 arg3, ref T4 arg4, ref T5 arg5, ref T6 arg6, ref T7 arg7, ref T8 arg8)
        {
            // Invoke locally when it's server method or client/broadcast method and target is my EndpointId
            if (ShouldServerInvokeLocally(callSite, m_localEndpoint, recipient))
            {
                var site = (CallSite<T1, T2, T3, T4, T5, T6, T7>)callSite;
                InvokeLocally(site, arg1, arg2, arg3, arg4, arg5, arg6, arg7);
            }
        }
    }
}
