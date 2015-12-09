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
        protected override void DispatchEvent<T1, T2, T3, T4, T5, T6, T7>(CallSite callSite, EndpointId recipient, float unreliablePriority, ref T1 arg1, ref T2 arg2, ref T3 arg3, ref T4 arg4, ref T5 arg5, ref T6 arg6, ref T7 arg7)
        {
            if (callSite.HasServerFlag)
            {
                var site = (CallSite<T1, T2, T3, T4, T5, T6, T7>)callSite;
                InvokeLocally(site, arg1, arg2, arg3, arg4, arg5, arg6, arg7);
            }
        }
    }
}
