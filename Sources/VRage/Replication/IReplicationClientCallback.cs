using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.Library.Collections;
using VRage.Network;

namespace VRage.Replication
{
    public interface IReplicationClientCallback
    {
        void SendClientUpdate(BitStream stream);
        void SendEvent(BitStream stream, bool reliable);
        void SendReplicableReady(BitStream stream);
    }
}
