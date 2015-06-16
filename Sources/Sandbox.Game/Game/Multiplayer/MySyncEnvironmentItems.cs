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
        struct BeginBatchAddMsg
        {
            public long EntityId;
        }

        [MessageId(3256, P2PMessageEnum.Reliable)]
        struct BatchAddItemMsg
        {
            public long EntityId;
            public Vector3D Position;
            public MyStringHash SubtypeId;
        }

        [MessageId(3257, P2PMessageEnum.Reliable)]
        struct EndBatchAddMsg
        {
            public long EntityId;
        }

        public static Action<MyEntity, int> OnRemoveEnvironmentItem;

        static MySyncEnvironmentItems()
        {
            MySyncLayer.RegisterMessage<RemoveEnvironmentItemMsg>(OnRemoveEnvironmentItemMessage, MyMessagePermissions.Any, MyTransportMessageEnum.Request);
            MySyncLayer.RegisterMessage<ModifyModelMsg>(OnModifyModelMessage, MyMessagePermissions.FromServer, MyTransportMessageEnum.Request);
            MySyncLayer.RegisterMessage<BeginBatchAddMsg>(OnBeginBatchAddMessage, MyMessagePermissions.FromServer, MyTransportMessageEnum.Request);
            MySyncLayer.RegisterMessage<BatchAddItemMsg>(OnBatchAddItemMessage, MyMessagePermissions.FromServer, MyTransportMessageEnum.Request);
            MySyncLayer.RegisterMessage<EndBatchAddMsg>(OnEndBatchAddMessage, MyMessagePermissions.FromServer, MyTransportMessageEnum.Request);
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
                entity.ModifyItemModel(msg.InstanceId, msg.ModelId, false);
            }
        }


        public static void SendBeginBatchAddMessage(long entityId)
        {
            var msg = new BeginBatchAddMsg()
            {
                EntityId = entityId,
            };

            Sync.Layer.SendMessageToAllButOne(ref msg, MySteam.UserId, MyTransportMessageEnum.Request);
        }

        static void OnBeginBatchAddMessage(ref BeginBatchAddMsg msg, MyNetworkClient sender)
        {
            Debug.Assert(!Sync.IsServer);
            MyEnvironmentItems entity;
            if (MyEntities.TryGetEntityById<MyEnvironmentItems>(msg.EntityId, out entity))
            {
                entity.BeginBatchAdd(false);
            }
        }

        public static void SendBatchAddItemMessage(long entityId, Vector3D position, MyStringHash subtypeId)
        {
            var msg = new BatchAddItemMsg()
            {
                EntityId = entityId,
                Position = position,
                SubtypeId = subtypeId,
            };

            Sync.Layer.SendMessageToAllButOne(ref msg, MySteam.UserId, MyTransportMessageEnum.Request);
        }

        static void OnBatchAddItemMessage(ref BatchAddItemMsg msg, MyNetworkClient sender)
        {
            Debug.Assert(!Sync.IsServer);
            MyEnvironmentItems entity;
            if (MyEntities.TryGetEntityById<MyEnvironmentItems>(msg.EntityId, out entity))
            {
                entity.BatchAddItem(msg.Position, msg.SubtypeId, false);
            }
        }

        public static void SendEndBatchAddMessage(long entityId)
        {
            var msg = new EndBatchAddMsg()
            {
                EntityId = entityId,
            };

            Sync.Layer.SendMessageToAllButOne(ref msg, MySteam.UserId, MyTransportMessageEnum.Request);
        }

        static void OnEndBatchAddMessage(ref EndBatchAddMsg msg, MyNetworkClient sender)
        {
            Debug.Assert(!Sync.IsServer);
            MyEnvironmentItems entity;
            if (MyEntities.TryGetEntityById<MyEnvironmentItems>(msg.EntityId, out entity))
            {
                entity.EndBatchAdd(false);
            }
        }
    }
}
