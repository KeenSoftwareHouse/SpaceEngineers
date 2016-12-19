using Sandbox.Engine.Utils;
using Sandbox.Game;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.Entities.Inventory;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.Replication;
using Sandbox.Game.World;
using SteamSDK;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using VRage.Game.Components;
using VRage.Game.Entity;
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

        /// <summary>
        /// Initializes a new instance of the MyMultiplayerServerBase class.
        /// </summary>
        /// <param name="localClientEndpoint">Local client endpoint (for single player or lobby host) or null (for dedicated server)</param>
        public MyMultiplayerServerBase(MySyncLayer syncLayer, EndpointId? localClientEndpoint)
            : base(syncLayer)
        {
            Debug.Assert(MyEntities.GetEntities().Count == 0, "Multiplayer server must be created before any entities are loaded!");

            var replication = new MyReplicationServer(this, localClientEndpoint, MyFakes.MULTIPLAYER_USE_PLAYOUT_DELAY_BUFFER);
            if (MyFakes.MULTIPLAYER_REPLICATION_TEST)
            {
                replication.MaxSleepTime = MyTimeSpan.FromSeconds(30);
            }
            SetReplicationLayer(replication);
            ClientLeft += (steamId, e) => ReplicationLayer.OnClientLeft(new EndpointId(steamId));
            ClientJoined += (steamId) => ReplicationLayer.OnClientJoined(new EndpointId(steamId), CreateClientState());

            MyEntities.OnEntityCreate += CreateReplicableForObject;
            MyEntityComponentBase.OnAfterAddedToContainer += CreateReplicableForObject;
            MyExternalReplicable.Destroyed += DestroyReplicable;

            foreach (var entity in MyEntities.GetEntities())
            {
                CreateReplicableForObject(entity);
                var components = entity.Components;
                if (components != null)
                {
                    foreach (var comp in components)
                        CreateReplicableForObject(comp);
                }
            }

            syncLayer.TransportLayer.Register(MyMessageId.RPC, ReplicationLayer.ProcessEvent);
            syncLayer.TransportLayer.Register(MyMessageId.REPLICATION_READY, ReplicationLayer.ReplicableReady);
            syncLayer.TransportLayer.Register(MyMessageId.CLIENT_UPDATE, ReplicationLayer.OnClientUpdate);
            syncLayer.TransportLayer.Register(MyMessageId.CLIENT_ACKS, ReplicationLayer.OnClientAcks);
            syncLayer.TransportLayer.Register(MyMessageId.CLIENT_READY, ClientReady);
        }

        public void RaiseReplicableCreated(object obj)
        {
            Debug.Assert(Sync.IsServer);
            CreateReplicableForObject(obj);
        }

        void CreateReplicableForObject(object obj)
        {
            Debug.Assert(obj != null);
            if (obj == null)
                return;

            // TODO: Hack to fix replicating aggregate
            if (obj is MyInventoryAggregate)
                return;

            MyEntity entity = obj as MyEntity;
            if(entity != null && entity.IsPreview)
            {
                return;
            }

            var type = m_factory.FindTypeFor(obj);
            if (type != null && ReplicationLayer.IsTypeReplicated(type))
            {
                Debug.Assert(MyExternalReplicable.FindByObject(obj) == null, "Object is already replicated!");

                var replicable = (MyExternalReplicable)Activator.CreateInstance(type);
                replicable.Hook(obj);
                ReplicationLayer.Replicate(replicable);
                replicable.OnServerReplicate();
            }
        }

        void DestroyReplicable(MyExternalReplicable obj)
        {
            ReplicationLayer.Destroy(obj);
        }

        public override void Dispose()
        {
            MyEntities.OnEntityCreate -= CreateReplicableForObject;
            MyEntityComponentBase.OnAfterAddedToContainer -= CreateReplicableForObject;
            MyExternalReplicable.Destroyed -= DestroyReplicable;
            base.Dispose();
        }

        void ClientReady(VRage.MyPacket packet)
        {
            ReplicationLayer.OnClientReady(packet.Sender);
            if (MyPerGameSettings.BlockForVoxels)
            {
                foreach (var voxelMap in MySession.Static.VoxelMaps.Instances)
                {
                    MyMultiplayer.ReplicateImmediatelly(voxelMap);
                }
            }
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

        void IReplicationServerCallback.SendReplicationCreateStreamed(BitStream stream, EndpointId endpoint)
        {
            SyncLayer.TransportLayer.SendMessage(MyMessageId.REPLICATION_STREAM_BEGIN, stream, true, endpoint);
        }


        void IReplicationServerCallback.SendReplicationDestroy(BitStream stream, EndpointId endpoint)
        {
            SyncLayer.TransportLayer.SendMessage(MyMessageId.REPLICATION_DESTROY, stream, true, endpoint);
        }

        void IReplicationServerCallback.SendStateSync(BitStream stream, EndpointId endpoint,bool reliable)
        {
            SyncLayer.TransportLayer.SendMessage(MyMessageId.SERVER_STATE_SYNC, stream, reliable, endpoint);
        }

        void IReplicationServerCallback.SendWorldData(BitStream stream, EndpointId endpoint)
        {
            SyncLayer.TransportLayer.SendMessage(MyMessageId.WORLD_DATA, stream, true, endpoint);
        }

        void IReplicationServerCallback.SendJoinResult(BitStream stream, EndpointId endpoint)
        {
            SyncLayer.TransportLayer.SendMessage(MyMessageId.JOIN_RESULT, stream, true, endpoint);
        }

        void IReplicationServerCallback.SendEvent(BitStream stream, bool reliable, EndpointId endpoint)
        {
            SyncLayer.TransportLayer.SendMessage(MyMessageId.RPC, stream, reliable, endpoint);
        }


        void IReplicationServerCallback.SentClientJoined(BitStream stream, EndpointId endpoint)
        {
            SyncLayer.TransportLayer.SendMessage(MyMessageId.CLIENT_CONNNECTED, stream, true, endpoint);
        }

        void IReplicationServerCallback.SendCustomState(BitStream stream, EndpointId endpoint)
        {
            stream.WriteFloat(Sandbox.Engine.Physics.MyPhysics.SimulationRatio);
        }            



        int IReplicationServerCallback.GetMTUSize(EndpointId clientId)
        {
            // Steam has MTU 1200, one byte is used by transport layer to write message id
            return 1200 - 1;
        }

        int IReplicationServerCallback.GetMTRSize(EndpointId clientId)
        {
            // Steam has MTU 1200, one byte is used by transport layer to write message id
            return 1024*1024 - 1;
        }

        public MyTimeSpan GetUpdateTime()
        {
            return MySandboxGame.Static.SimulationTime;
        }

        #endregion
    }
}
