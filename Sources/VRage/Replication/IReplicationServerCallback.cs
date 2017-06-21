using VRage.Library.Collections;
using VRage.Network;

namespace VRage.Replication
{
    public interface IReplicationServerCallback
    {
        void SendServerData(BitStream stream, EndpointId endpoint);
        void SendReplicationCreate(BitStream stream, EndpointId endpoint);
        void SendReplicationCreateStreamed(BitStream stream, EndpointId endpoint);
        void SendReplicationDestroy(BitStream stream, EndpointId endpoint);
        void SendStateSync(BitStream stream, EndpointId endpoint,bool reliable);
        void SendJoinResult(BitStream stream, EndpointId endpoint);
        void SendWorldData(BitStream stream, EndpointId endpoint);
        void SendCustomState(BitStream stream, EndpointId endpoint);

        void SentClientJoined(BitStream stream, EndpointId endpoint);
        void SendEvent(BitStream stream, bool reliable, EndpointId endpoint);
        int GetMTUSize(EndpointId clientId);
        int GetMTRSize(EndpointId clientId);

        void DisconnectClient(ulong clientId);

        VRage.Library.Utils.MyTimeSpan GetUpdateTime();
    }
}
