using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.Library.Collections;
using VRage.Network;

namespace VRage.Replication
{
    public interface IReplicationServerCallback
    {
        void SendServerData(BitStream stream, EndpointId endpoint);
        void SendReplicationCreate(BitStream stream, EndpointId endpoint);
        void SendReplicationDestroy(BitStream stream, EndpointId endpoint);
        void SendStateSync(BitStream stream, EndpointId endpoint);
        void SendEvent(BitStream stream, bool reliable, EndpointId endpoint);
        int GetMTUSize(EndpointId clientId);
    }
}
