using Sandbox.Engine.Multiplayer;
using Sandbox.Engine.Networking;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.EnvironmentItems;
using Sandbox.Game.World;
using SteamSDK;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using VRage.Utils;
using VRageMath;

namespace Sandbox.Game.Multiplayer
{
    [PreloadRequired]
    public static class MySyncEnvironmentItems
    {
        [MessageId(3251, P2PMessageEnum.Reliable)]
        struct RemoveEnvironmentItemMsg
        {
            public long EntityId;
            public int ItemInstanceId;
        }

        [MessageId(3252, P2PMessageEnum.Reliable)]
        struct ModifyModelMsg
        {
            public long EntityId;
            public int InstanceId;
            public int ModelId;
        }

        [MessageId(3255, P2PMessageEnum.Reliable)]
        struct BeginBatchMsg
        {
            public long EntityId;
        }

        [MessageId(3256, P2PMessageEnum.Reliable)]
        struct BatchAddItemMsg
        {
            public long EntityId;
            public Vector3D Position;
            public MyStringHash SubtypeId;
            public int ModelId;
        }

        [MessageId(3257, P2PMessageEnum.Reliable)]
        struct BatchModifyItemMsg
        {
            public long EntityId;
            public int LocalId;
            public int ModelLocalId;
        }

        [MessageId(3258, P2PMessageEnum.Reliable)]
        struct EndBatchMsg
        {
            public long EntityId;
        }

        public static Action<MyEntity, int> OnRemoveEnvironmentItem;

        static MySyncEnvironmentItems()
        {
            MySyncLayer.RegisterMessage<RemoveEnvironmentItemMsg>(OnRemoveEnvironmentItemMessage, MyMessagePermissions.Any, MyTransportMessageEnum.Request);
            MySyncLayer.RegisterMessage<ModifyModelMsg>(OnModifyModelMessage, MyMessagePermissions.FromServer, MyTransportMessageEnum.Request);
            MySyncLayer.RegisterMessage<BeginBatchMsg>(OnBeginBatchAddMessage, MyMessagePermissions.FromServer, MyTransportMessageEnum.Request);
            MySyncLayer.RegisterMessage<BatchAddItemMsg>(OnBatchAddItemMessage, MyMessagePermissions.FromServer, MyTransportMessageEnum.Request);
            MySyncLayer.RegisterMessage<BatchModifyItemMsg>(OnBatchModifyItemMessage, MyMessagePermissions.FromServer, MyTransportMessageEnum.Request);
            MySyncLayer.RegisterMessage<EndBatchMsg>(OnEndBatchAddMessage, MyMessagePermissions.FromServer, MyTransportMessageEnum.Request);
        }

        public static void RemoveEnvironmentItem(long entityId, int itemInstanceId)
        {
            var msg = new RemoveEnvironmentItemMsg();
            msg.EntityId = entityId;
            msg.ItemInstanceId = itemInstanceId;
            MySession.Static.SyncLayer.SendMessageToAll(ref msg);
        }

        static void OnRemoveEnvironmentItemMessage(ref RemoveEnvironmentItemMsg msg, MyNetworkClient sender)
        {
            MyEntity entity;
            if (MyEntities.TryGetEntityById(msg.EntityId, out entity))
            {
                if (OnRemoveEnvironmentItem != null)
                    OnRemoveEnvironmentItem(entity, msg.ItemInstanceId);
            }
        }

        public static void SendModifyModelMessage(long entityId, int instanceId, int modelId)
        {
            var msg = new ModifyModelMsg()
            {
                EntityId = entityId,
                InstanceId = instanceId,
                ModelId = modelId,
            };

            Sync.Layer.SendMessageToAllButOne(ref msg, MySteam.UserId, MyTransportMessageEnum.Request);
        }

        static void OnModifyModelMessage(ref ModifyModelMsg msg, MyNetworkClient sender)
        {
            Debug.Assert(!Sync.IsServer);
            MyEnvironmentItems entity;
            if (MyEntities.TryGetEntityById<MyEnvironmentItems>(msg.EntityId, out entity))
            {
                entity.ModifyItemModel(msg.InstanceId, msg.ModelId, true, false);
            }
        }

        public static void SendBeginBatchAddMessage(long entityId)
        {
            var msg = new BeginBatchMsg()
            {
                EntityId = entityId,
            };

            Sync.Layer.SendMessageToAllButOne(ref msg, MySteam.UserId, MyTransportMessageEnum.Request);
        }

        static void OnBeginBatchAddMessage(ref BeginBatchMsg msg, MyNetworkClient sender)
        {
            Debug.Assert(!Sync.IsServer);
            MyEnvironmentItems entity;
            if (MyEntities.TryGetEntityById<MyEnvironmentItems>(msg.EntityId, out entity))
            {
                entity.BeginBatch(false);
            }
        }

        public static void SendBatchAddItemMessage(long entityId, Vector3D position, MyStringHash subtypeId, int modelId)
        {
            var msg = new BatchAddItemMsg()
            {
                EntityId = entityId,
                Position = position,
                SubtypeId = subtypeId,
                ModelId = modelId,
            };

            Sync.Layer.SendMessageToAllButOne(ref msg, MySteam.UserId, MyTransportMessageEnum.Request);
        }

        static void OnBatchAddItemMessage(ref BatchAddItemMsg msg, MyNetworkClient sender)
        {
            Debug.Assert(!Sync.IsServer);
            MyEnvironmentItems entity;
            if (MyEntities.TryGetEntityById<MyEnvironmentItems>(msg.EntityId, out entity))
            {
                entity.BatchAddItem(msg.Position, msg.SubtypeId, msg.ModelId, false);
            }
        }

        public static void SendBatchModifyItemMessage(long entityId, int localId, int modelLocalId)
        {
            var msg = new BatchModifyItemMsg()
            {
                EntityId = entityId,
                LocalId = localId,
                ModelLocalId = modelLocalId,
            };

            Sync.Layer.SendMessageToAllButOne(ref msg, MySteam.UserId, MyTransportMessageEnum.Request);
        }

        static void OnBatchModifyItemMessage(ref BatchModifyItemMsg msg, MyNetworkClient sender)
        {
            Debug.Assert(!Sync.IsServer);
            MyEnvironmentItems entity;
            if (MyEntities.TryGetEntityById<MyEnvironmentItems>(msg.EntityId, out entity))
            {
                entity.BatchModifyItem(msg.LocalId, msg.ModelLocalId, false);
            }
        }

        public static void SendEndBatchAddMessage(long entityId)
        {
            var msg = new EndBatchMsg()
            {
                EntityId = entityId,
            };

            Sync.Layer.SendMessageToAllButOne(ref msg, MySteam.UserId, MyTransportMessageEnum.Request);
        }

        static void OnEndBatchAddMessage(ref EndBatchMsg msg, MyNetworkClient sender)
        {
            Debug.Assert(!Sync.IsServer);
            MyEnvironmentItems entity;
            if (MyEntities.TryGetEntityById<MyEnvironmentItems>(msg.EntityId, out entity))
            {
                entity.EndBatch(false);
            }
        }
    }
}
