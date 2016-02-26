using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.Network;

namespace VRage.Replication
{
    public class MyPendingReplicable
    {
        public List<NetworkId> StateGroupIds = new List<NetworkId>();
        public int DebugCounter;
        public IMyReplicable DebugObject;
        public bool IsStreaming;
        public NetworkId StreamingGroupId;
    }
}
