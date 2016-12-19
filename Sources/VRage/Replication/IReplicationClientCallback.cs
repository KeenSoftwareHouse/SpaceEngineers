using VRage.Library.Collections;

namespace VRage.Replication
{
    public interface IReplicationClientCallback
    {
        void SendClientUpdate(BitStream stream);
        void SendClientAcks(BitStream stream);
        void SendEvent(BitStream stream, bool reliable);
        void SendReplicableReady(BitStream stream);
        void SendConnectRequest(BitStream stream);
        void ReadCustomState(BitStream stream);

        VRage.Library.Utils.MyTimeSpan GetUpdateTime();
        void SetNextFrameDelayDelta(int delay);
        void SetPing(long duration);
        float GetServerSimulationRatio();
        void DisconnectFromHost();
    }
}
