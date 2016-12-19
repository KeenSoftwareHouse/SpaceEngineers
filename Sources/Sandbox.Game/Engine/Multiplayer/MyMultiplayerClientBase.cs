using Sandbox.Engine.Utils;
using Sandbox.Game.Gui;
using Sandbox.Game.Multiplayer;
using VRage.Library.Collections;
using VRage.Library.Utils;
using VRage.Network;
using VRage.Replication;

namespace Sandbox.Engine.Multiplayer
{
    public abstract class MyMultiplayerClientBase : MyMultiplayerBase, IReplicationClientCallback
    {
        #region ReplicationClient
        void IReplicationClientCallback.SendClientUpdate(BitStream stream)
        {
            SyncLayer.TransportLayer.SendMessage(MyMessageId.CLIENT_UPDATE, stream,
                !MyFakes.MULTIPLAYER_USE_PLAYOUT_DELAY_BUFFER, new EndpointId(Sync.ServerId));
        }

        void IReplicationClientCallback.SendClientAcks(BitStream stream)
        {
            SyncLayer.TransportLayer.SendMessage(MyMessageId.CLIENT_ACKS, stream, true, new EndpointId(Sync.ServerId));
        }

        void IReplicationClientCallback.SendEvent(BitStream stream, bool reliable)
        {
            SyncLayer.TransportLayer.SendMessage(MyMessageId.RPC, stream, reliable, new EndpointId(Sync.ServerId));
        }

        void IReplicationClientCallback.SendReplicableReady(BitStream stream)
        {
            SyncLayer.TransportLayer.SendMessage(MyMessageId.REPLICATION_READY, stream, true, new EndpointId(Sync.ServerId));
        }

        void IReplicationClientCallback.SendConnectRequest(BitStream stream)
        {
            SyncLayer.TransportLayer.SendMessage(MyMessageId.CLIENT_CONNNECTED, stream, true, new EndpointId(Sync.ServerId));
        }

        void IReplicationClientCallback.ReadCustomState(BitStream stream)
        {
            Sync.ServerSimulationRatio = stream.ReadFloat();

        }

        public MyTimeSpan GetUpdateTime()
        {
            return MySandboxGame.Static.SimulationTime;
        }

        public void SetNextFrameDelayDelta(int delay)
        {
            MySandboxGame.Static.SetNextFrameDelayDelta(delay);
        }

        public void SetPing(long duration)
        {
            if (MyHud.Netgraph != null)
                MyHud.Netgraph.Ping = duration;
        }

        public float GetServerSimulationRatio()
        {
            return Sync.ServerSimulationRatio;
        }

        public void DisconnectFromHost()
        {
            DisconnectClient(0);
        }

        #endregion

        protected MyMultiplayerClientBase(MySyncLayer syncLayer) : base(syncLayer)
        {
        }
    }
}
