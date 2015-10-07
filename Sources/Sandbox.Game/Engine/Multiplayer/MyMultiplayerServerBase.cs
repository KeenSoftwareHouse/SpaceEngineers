using Sandbox.Engine.Utils;
using Sandbox.Game;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.Replicables;
using SteamSDK;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using VRage.Library.Collections;
using VRage.Library.Utils;
using VRage.Network;
using VRage.Replication;

namespace Sandbox.Engine.Multiplayer
{
    public abstract class MyMultiplayerServerBase : MyMultiplayerBase, IReplicationServerCallback
    {
        private MyReplicableFactory m_factory = new MyReplicableFactory();

        public new MyReplicationServer ReplicationLayer { get { return (MyReplicationServer)base.ReplicationLayer; } }

        public MyMultiplayerServerBase(MySyncLayer syncLayer)
            : base(syncLayer)
        {
            var replication = new MyReplicationServer(this, () => MySandboxGame.Static.UpdateTime);
            if (MyFakes.MULTIPLAYER_REPLICATION_TEST)
            {
                replication.MaxSleepTime = MyTimeSpan.FromSeconds(30);
            }
            SetReplicationLayer(replication);
            ClientLeft += (steamId, e) => ReplicationLayer.OnClientLeft(new EndpointId(steamId));

            MyEntities.OnEntityCreate += CreateReplicableForObject;
            MyInventory.OnCreated += CreateReplicableForObject;
            MyExternalReplicable.Destroyed += DestroyReplicable;

            foreach (var entity in MyEntities.GetEntities())
            {
                CreateReplicableForObject(entity);
            }

            syncLayer.TransportLayer.Register(MyMessageId.RPC, ReplicationLayer.ProcessEvent);
            syncLayer.TransportLayer.Register(MyMessageId.REPLICATION_READY, ReplicationLayer.ReplicableReady);
            syncLayer.TransportLayer.Register(MyMessageId.CLIENT_UPDATE, ReplicationLayer.OnClientUpdate);
            syncLayer.TransportLayer.Register(MyMessageId.CLIENT_READY, (p) => ReplicationLayer.OnClientReady(p.Sender, new MyClientState()));
        }

        void CreateReplicableForObject(object obj)
        {
            var type = m_factory.FindTypeFor(obj);
            if (type != null && ReplicationLayer.IsTypeReplicated(type))
            {
                var replicable = (MyExternalReplicable)Activator.CreateInstance(type);
                replicable.Hook(obj);
                ReplicationLayer.Replicate(replicable);
            }
        }

        void DestroyReplicable(MyExternalReplicable obj)
        {
            ReplicationLayer.Destroy(obj);
        }

        public override void Dispose()
        {
            MyEntities.OnEntityCreate -= CreateReplicableForObject;
            MyInventory.OnCreated -= CreateReplicableForObject;
            MyExternalReplicable.Destroyed -= DestroyReplicable;
            base.Dispose();
        }

        #region ReplicationServer
        void IReplicationServerCallback.SendServerData(BitStream stream, EndpointId endpoint)
        {
            SyncLayer.TransportLayer.SendMessage(MyMessageId.SERVER_DATA, stream, true, endpoint);
        }

        void IReplicationServerCallback.SendReplicationCreate(BitStream stream, EndpointId endpoint)
        {
            SyncLayer.TransportLayer.SendMessage(MyMessageId.REPLICATION_CREATE, stream, true, endpoint);
        }

        void IReplicationServerCallback.SendReplicationDestroy(BitStream stream, EndpointId endpoint)
        {
            SyncLayer.TransportLayer.SendMessage(MyMessageId.REPLICATION_DESTROY, stream, true, endpoint);
        }

        void IReplicationServerCallback.SendStateSync(BitStream stream, EndpointId endpoint)
        {
            SyncLayer.TransportLayer.SendMessage(MyMessageId.SERVER_STATE_SYNC, stream, false, endpoint);
        }

        void IReplicationServerCallback.SendEvent(BitStream stream, bool reliable, EndpointId endpoint)
        {
            SyncLayer.TransportLayer.SendMessage(MyMessageId.RPC, stream, reliable, endpoint);
        }

        int IReplicationServerCallback.GetMTUSize(EndpointId clientId)
        {
            // Steam has MTU 1200, one byte is used by transport layer to write message id
            return 1200 - 1;
        }
        #endregion
    }
}
