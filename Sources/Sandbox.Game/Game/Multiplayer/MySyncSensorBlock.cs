using ProtoBuf;
using Sandbox.Engine.Multiplayer;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Blocks;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.Screens.Helpers;
using Sandbox.Game.World;
using SteamSDK;
using System;
using VRageMath;

namespace Sandbox.Game.Multiplayer
{
    [PreloadRequired]
    class MySyncSensorBlock : MySyncEntity
    {
        private MySensorBlock m_block;
        private bool m_syncing;
        public bool IsSyncing
        {
            get { return m_syncing; }
        }

        [MessageIdAttribute(3141, P2PMessageEnum.Reliable)]
        protected struct ChangeMySensorMinMsg : IEntityMessage
        {
            public long EntityId;

            public long GetEntityId() { return EntityId; }

            public Vector3 FieldMin;

            public override string ToString()
            {
                return String.Format("{0}, {1}", this.GetType().Name, this.GetEntityText());
            }
        }

        [MessageIdAttribute(3142, P2PMessageEnum.Reliable)]
        protected struct ChangeMySensorMaxMsg : IEntityMessage
        {
            public long EntityId;

            public long GetEntityId() { return EntityId; }

            public Vector3 FieldMax;

            public override string ToString()
            {
                return String.Format("{0}, {1}", this.GetType().Name, this.GetEntityText());
            }
        }

        [MessageIdAttribute(3143, P2PMessageEnum.Reliable)]
        protected struct ChangeMySensorFiltersMsg : IEntityMessage
        {
            public long EntityId;
            public long GetEntityId() { return EntityId; }

            public MySensorFilterFlags Filters;

            public override string ToString()
            {
                return String.Format("{0}, {1}", this.GetType().Name, this.GetEntityText());
            }
        }

        [ProtoContract]
        [MessageIdAttribute(3144, P2PMessageEnum.Reliable)]
        protected struct ChangeMySensorToolbarItemMsg : IEntityMessage
        {
            [ProtoMember]
            public long EntityId;
            public long GetEntityId() { return EntityId; }

            [ProtoMember]
            public ToolbarItem Item;

            [ProtoMember]
            public int Index;

            public override string ToString()
            {
                return String.Format("{0}, {1}", this.GetType().Name, this.GetEntityText());
            }
        }

        [MessageIdAttribute(3145, P2PMessageEnum.Reliable)]
        protected struct ChangeMySensorActivityMsg : IEntityMessage
        {
            public long EntityId;
            public long GetEntityId() { return EntityId; }

            public BoolBlit IsActive;
            public override string ToString()
            {
                return String.Format("{0}, {1}", this.GetType().Name, this.GetEntityText());
            }
        }

        [MessageIdAttribute(3146, P2PMessageEnum.Reliable)]
        protected struct ChangeMySensorPlaySoundMsg : IEntityMessage
        {
            public long EntityId;
            public long GetEntityId() { return EntityId; }

            public BoolBlit PlaySound;
            public override string ToString()
            {
                return String.Format("{0}, {1}", this.GetType().Name, this.GetEntityText());
            }
        }

        static MySyncSensorBlock()
        {
            MySyncLayer.RegisterMessage<ChangeMySensorMinMsg>(ChangeSensorMinRequest, MyMessagePermissions.ToServer, MyTransportMessageEnum.Request);
            MySyncLayer.RegisterMessage<ChangeMySensorMinMsg>(ChangeSensorMinSuccess, MyMessagePermissions.FromServer, MyTransportMessageEnum.Success);

            MySyncLayer.RegisterMessage<ChangeMySensorMaxMsg>(ChangeSensorMaxRequest, MyMessagePermissions.ToServer, MyTransportMessageEnum.Request);
            MySyncLayer.RegisterMessage<ChangeMySensorMaxMsg>(ChangeSensorMaxSuccess, MyMessagePermissions.FromServer, MyTransportMessageEnum.Success);
            
            MySyncLayer.RegisterMessage<ChangeMySensorFiltersMsg>(ChangeSensorFiltersRequest, MyMessagePermissions.ToServer, MyTransportMessageEnum.Request);
            MySyncLayer.RegisterMessage<ChangeMySensorFiltersMsg>(ChangeSensorFiltersSuccess, MyMessagePermissions.FromServer, MyTransportMessageEnum.Success);

            MySyncLayer.RegisterMessage<ChangeMySensorActivityMsg>(ChangeSensorIsActiveRequest, MyMessagePermissions.ToServer, MyTransportMessageEnum.Request);
            MySyncLayer.RegisterMessage<ChangeMySensorActivityMsg>(ChangeSensorIsActiveSuccess, MyMessagePermissions.FromServer, MyTransportMessageEnum.Success);

            MySyncLayer.RegisterMessage<ChangeMySensorPlaySoundMsg>(ChangeSensorPlaySoundRequest, MyMessagePermissions.ToServer, MyTransportMessageEnum.Request);
            MySyncLayer.RegisterMessage<ChangeMySensorPlaySoundMsg>(ChangeSensorPlaySoundSuccess, MyMessagePermissions.FromServer, MyTransportMessageEnum.Success);

            MySyncLayer.RegisterEntityMessage<MySyncSensorBlock, ChangeMySensorToolbarItemMsg>(OnToolbarItemChanged, MyMessagePermissions.FromServer|MyMessagePermissions.ToServer);
        }

        public MySyncSensorBlock(MySensorBlock block)
            : base(block)
        {
            m_block = block;
        }              

        public void SendChangeSensorMinRequest(ref Vector3 fieldMin)
        {
            var msg = new ChangeMySensorMinMsg();
            msg.EntityId = m_block.EntityId;
            msg.FieldMin = fieldMin;

            Sync.Layer.SendMessageToServer(ref msg, MyTransportMessageEnum.Request);
        }

        static void ChangeSensorMinRequest(ref ChangeMySensorMinMsg msg, MyNetworkClient sender)
        {
            MyEntity entity;
            MyEntities.TryGetEntityById(msg.EntityId, out entity);
            var block = entity as MySensorBlock;
            if (block != null)
            {
                block.FieldMin = msg.FieldMin;
                Sync.Layer.SendMessageToAllButOne(ref msg, sender.SteamUserId, MyTransportMessageEnum.Success);
            }       
        }

        static void ChangeSensorMinSuccess(ref ChangeMySensorMinMsg msg, MyNetworkClient sender)
        {
            MyEntity entity;
            MyEntities.TryGetEntityById(msg.EntityId, out entity);
            var block = entity as MySensorBlock;
            if (block != null)
            {
                block.FieldMin = msg.FieldMin;
            }
        }

        public void SendChangeSensorMaxRequest(ref Vector3 fieldMax)
        {
            var msg = new ChangeMySensorMaxMsg();
            msg.EntityId = m_block.EntityId;
            msg.FieldMax = fieldMax;

            Sync.Layer.SendMessageToServer(ref msg, MyTransportMessageEnum.Request);
        }

        static void ChangeSensorMaxRequest(ref ChangeMySensorMaxMsg msg, MyNetworkClient sender)
        {
            MyEntity entity;
            MyEntities.TryGetEntityById(msg.EntityId, out entity);
            var block = entity as MySensorBlock;
            if (block != null)
            {
                block.FieldMax = msg.FieldMax;
                Sync.Layer.SendMessageToAllButOne(ref msg, sender.SteamUserId,MyTransportMessageEnum.Success);
            }
        }

        static void ChangeSensorMaxSuccess(ref ChangeMySensorMaxMsg msg, MyNetworkClient sender)
        {
            MyEntity entity;
            MyEntities.TryGetEntityById(msg.EntityId, out entity);
            var block = entity as MySensorBlock;
            if (block != null)
            {
                block.FieldMax = msg.FieldMax;
            }
        }

        public void SendFiltersChangedRequest(MySensorFilterFlags filters)
        {
            var msg = new ChangeMySensorFiltersMsg();
            msg.EntityId = m_block.EntityId;
            msg.Filters = filters;

            Sync.Layer.SendMessageToServer(ref msg, MyTransportMessageEnum.Request);
        }

        static void ChangeSensorFiltersRequest(ref ChangeMySensorFiltersMsg msg, MyNetworkClient sender)
        {
            MyEntity entity;
            MyEntities.TryGetEntityById(msg.EntityId, out entity);
            var block = entity as MySensorBlock;
            if (block != null)
            {
                block.Filters = msg.Filters;
                Sync.Layer.SendMessageToAllButOne(ref msg, sender.SteamUserId, MyTransportMessageEnum.Success);
            }
        }

        static void ChangeSensorFiltersSuccess(ref ChangeMySensorFiltersMsg msg, MyNetworkClient sender)
        {
            MyEntity entity;
            MyEntities.TryGetEntityById(msg.EntityId, out entity);
            var block = entity as MySensorBlock;
            if (block != null)
            {
                block.Filters = msg.Filters;
            }
        }

        public void SendSensorIsActiveChangedRequest(bool IsActive)
        {
            var msg = new ChangeMySensorActivityMsg();
            msg.EntityId = m_block.EntityId;
            msg.IsActive = IsActive;

            Sync.Layer.SendMessageToServer(ref msg, MyTransportMessageEnum.Request);
        }

        static void ChangeSensorIsActiveRequest(ref ChangeMySensorActivityMsg msg, MyNetworkClient sender)
        {
            MyEntity entity;
            MyEntities.TryGetEntityById(msg.EntityId, out entity);
            var block = entity as MySensorBlock;
            if (block != null)
            {
                block.IsActive = msg.IsActive;
                Sync.Layer.SendMessageToAllButOne(ref msg, sender.SteamUserId, MyTransportMessageEnum.Success);
            }
        }

        static void ChangeSensorIsActiveSuccess(ref ChangeMySensorActivityMsg msg, MyNetworkClient sender)
        {
            MyEntity entity;
            MyEntities.TryGetEntityById(msg.EntityId, out entity);
            var block = entity as MySensorBlock;
            if (block != null)
            {
                block.IsActive = msg.IsActive;
            }
        }

        public void SendToolbarItemChanged(ToolbarItem item, int index)
        {
            if (m_syncing)
                return;
            var msg = new ChangeMySensorToolbarItemMsg();
            msg.EntityId = m_block.EntityId;
            msg.Item = item;
            msg.Index = index;

            Sync.Layer.SendMessageToServer(ref msg);
        }

        private static void OnToolbarItemChanged(MySyncSensorBlock sync, ref ChangeMySensorToolbarItemMsg msg, MyNetworkClient sender)
        {
            sync.m_syncing = true;
            MyToolbarItem item = null;
            if (msg.Item.EntityID != 0)
                item = ToolbarItem.ToItem(msg.Item);
            sync.m_block.Toolbar.SetItemAtIndex(msg.Index, item);
            sync.m_syncing = false;
            if (Sync.IsServer)
            {
                Sync.Layer.SendMessageToAllButOne(ref msg, sender.SteamUserId);
            }
        }

        public void SendChangeSensorPlaySoundRequest(bool PlaySound)
        {
            var msg = new ChangeMySensorPlaySoundMsg();
            msg.EntityId = m_block.EntityId;
            msg.PlaySound = PlaySound;

            Sync.Layer.SendMessageToServer(ref msg, MyTransportMessageEnum.Request);
        }

        static void ChangeSensorPlaySoundRequest(ref ChangeMySensorPlaySoundMsg msg, MyNetworkClient sender)
        {
            MyEntity entity;
            MyEntities.TryGetEntityById(msg.EntityId, out entity);
            var block = entity as MySensorBlock;
            if (block != null)
            {
                block.PlayProximitySound = msg.PlaySound;
                Sync.Layer.SendMessageToAllButOne(ref msg, sender.SteamUserId, MyTransportMessageEnum.Success);
            }
        }

        static void ChangeSensorPlaySoundSuccess(ref ChangeMySensorPlaySoundMsg msg, MyNetworkClient sender)
        {
            MyEntity entity;
            MyEntities.TryGetEntityById(msg.EntityId, out entity);
            var block = entity as MySensorBlock;
            if (block != null)
            {
                block.PlayProximitySound = msg.PlaySound;
            }
        }
    }
}
