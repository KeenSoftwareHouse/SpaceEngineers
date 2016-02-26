using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VRage.Network
{
    public enum CallSiteFlags
    {
        None = 0x0,
        Client = 0x1,
        Server = 0x2,
        Broadcast = 0x4,
        Reliable = 0x8,
        RefreshReplicable = 0x10, // Will test conditions for replicable and optionally replicable it to clients before sending event
        BroadcastExcept = 0x20,
        Blocking = 0x40,
    }
}
