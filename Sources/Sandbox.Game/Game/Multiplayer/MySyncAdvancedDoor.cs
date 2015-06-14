using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sandbox.Common;
using Sandbox.Engine.Multiplayer;
using Sandbox.Game.Entities;
using Sandbox.Game.World;
using SteamSDK;
using VRageMath;

namespace Sandbox.Game.Multiplayer
{
    [PreloadRequired]
    class MySyncAdvancedDoor
    {
        MyAdvancedDoor m_block;

        [MessageIdAttribute(2607, P2PMessageEnum.Reliable)]
        protected struct ChangeDoorMsg : IEntityMessage
        {
            public long EntityId;
            public long GetEntityId() { return EntityId; }
            public long PlayerId;
            public BoolBlit Open;

            public override string ToString()
            {
                return String.Format("{0}, {1}", this.GetType().Name, this.GetEntityText());
            }
        }

        [MessageIdAttribute(2608, P2PMessageEnum.Reliable)]
        protected struct ChangeAutocloseIntervalMsg : IEntityMessage
        {
            public long EntityId;
            public long GetEntityId() { return EntityId; }

            public float Interval;
            public override string ToString()
            {
                return String.Format("{0}, {1}", this.GetType().Name, this.GetEntityText());
            }
        }

        [MessageIdAttribute(2609, P2PMessageEnum.Reliable)]
        protected struct ChangeAutocloseMsg : IEntityMessage
        {
            public long EntityId;
            public long PlayerId;
            public long GetEntityId() { return EntityId; }
            public BoolBlit Autoclose;

            public override string ToString()
            {
                return String.Format("{0}, {1}", this.GetType().Name, this.GetEntityText());
            }
        }

        static MySyncAdvancedDoor()
        {
            MySyncLayer.RegisterMessage<ChangeDoorMsg>(ChangeDoorRequest, MyMessagePermissions.ToServer, MyTransportMessageEnum.Request);
            MySyncLayer.RegisterMessage<ChangeDoorMsg>(ChangeDoorSuccess, MyMessagePermissions.FromServer, MyTransportMessageEnum.Success);
            MySyncLayer.RegisterMessage<ChangeDoorMsg>(ChangeDoorFailure, MyMessagePermissions.FromServer, MyTransportMessageEnum.Failure);

            MySyncLayer.RegisterMessage<ChangeAutocloseIntervalMsg>(ChangeAutocloseIntervalRequest, MyMessagePermissions.ToServer, MyTransportMessageEnum.Request);
            MySyncLayer.RegisterMessage<ChangeAutocloseIntervalMsg>(ChangeAutocloseIntervalSuccess, MyMessagePermissions.FromServer, MyTransportMessageEnum.Success);

            MySyncLayer.RegisterMessage<ChangeAutocloseMsg>(ChangeAutocloseRequest, MyMessagePermissions.ToServer, MyTransportMessageEnum.Request);
            MySyncLayer.RegisterMessage<ChangeAutocloseMsg>(ChangeAutocloseSuccess, MyMessagePermissions.FromServer, MyTransportMessageEnum.Success);
        }

        public MySyncAdvancedDoor(MyAdvancedDoor block)
        {
            m_block = block;
        }

        public void SendChangeDoorRequest(bool open, long identityId)
        {
            var msg = new ChangeDoorMsg();

            msg.EntityId = m_block.EntityId;
            msg.PlayerId = identityId;
            msg.Open = open;

            Sync.Layer.SendMessageToServer(ref msg, MyTransportMessageEnum.Request);
        }

        static void ChangeDoorRequest(ref ChangeDoorMsg msg, MyNetworkClient sender)
        {
            MyEntity entity;
            MyEntities.TryGetEntityById(msg.EntityId, out entity);
            if (entity is MyAdvancedDoor)
            {
                MyRelationsBetweenPlayerAndBlock relation = MyRelationsBetweenPlayerAndBlock.FactionShare;
                var cubeBlock = entity as MyCubeBlock;
                if (cubeBlock != null)
                {
                    relation = cubeBlock.GetUserRelationToOwner(msg.PlayerId);
                }

                if (relation == MyRelationsBetweenPlayerAndBlock.FactionShare || relation == MyRelationsBetweenPlayerAndBlock.Owner)
                {
                    Sync.Layer.SendMessageToAllAndSelf(ref msg, MyTransportMessageEnum.Success);
                }
            }
        }

        static void ChangeDoorSuccess(ref ChangeDoorMsg msg, MyNetworkClient sender)
        {
            MyEntity entity;
            MyEntities.TryGetEntityById(msg.EntityId, out entity);
            MyAdvancedDoor block = entity as MyAdvancedDoor;
            if (block != null)
            {
                block.Open = msg.Open;
            }
        }

        static void ChangeDoorFailure(ref ChangeDoorMsg msg, MyNetworkClient sender)
        {
        }

        #region Autoclose

        public void SendChangeAutocloseIntervalRequest(float interval)
        {
            var msg = new ChangeAutocloseIntervalMsg();

            msg.EntityId = m_block.EntityId;
            msg.Interval = interval;

            Sync.Layer.SendMessageToServer(ref msg, MyTransportMessageEnum.Request);
        }
        static void ChangeAutocloseIntervalRequest(ref ChangeAutocloseIntervalMsg msg, MyNetworkClient sender)
        {
            MyEntity entity;
            MyEntities.TryGetEntityById(msg.EntityId, out entity);
            if (entity is MyAdvancedDoor)
            {
                Sync.Layer.SendMessageToAllAndSelf(ref msg, MyTransportMessageEnum.Success);
            }
        }
        static void ChangeAutocloseIntervalSuccess(ref ChangeAutocloseIntervalMsg msg, MyNetworkClient sender)
        {
            MyEntity entity;
            MyEntities.TryGetEntityById(msg.EntityId, out entity);
            var door = entity as MyAdvancedDoor;
            if (door != null)
            {
                door.AutoCloseInterval = msg.Interval;
            }
        }

        public void SendChangeAutocloseRequest(bool autoclose, long identityId)
        {
            var msg = new ChangeAutocloseMsg();

            msg.EntityId = m_block.EntityId;
            msg.PlayerId = identityId;
            msg.Autoclose = autoclose;

            Sync.Layer.SendMessageToServer(ref msg, MyTransportMessageEnum.Request);
        }

        static void ChangeAutocloseRequest(ref ChangeAutocloseMsg msg, MyNetworkClient sender)
        {
            MyEntity entity;
            MyEntities.TryGetEntityById(msg.EntityId, out entity);
            if (entity is MyAdvancedDoor)
            {
                MyRelationsBetweenPlayerAndBlock relation = MyRelationsBetweenPlayerAndBlock.FactionShare;
                var cubeBlock = entity as MyCubeBlock;
                if (cubeBlock != null)
                {
                    relation = cubeBlock.GetUserRelationToOwner(msg.PlayerId);
                }

                if (relation == MyRelationsBetweenPlayerAndBlock.FactionShare || relation == MyRelationsBetweenPlayerAndBlock.Owner)
                {
                    Sync.Layer.SendMessageToAllAndSelf(ref msg, MyTransportMessageEnum.Success);
                }
            }
        }

        static void ChangeAutocloseSuccess(ref ChangeAutocloseMsg msg, MyNetworkClient sender)
        {
            MyEntity entity;
            MyEntities.TryGetEntityById(msg.EntityId, out entity);
            MyAdvancedDoor block = entity as MyAdvancedDoor;
            if (block != null)
            {
                block.AutoClose = msg.Autoclose;
            }
        }

        #endregion
    }
}
