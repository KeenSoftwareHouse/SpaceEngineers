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
    class MySyncDoor
    {
        MyDoor m_block;

        [MessageIdAttribute(667, P2PMessageEnum.Reliable)]
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

        static MySyncDoor()
        {
            MySyncLayer.RegisterMessage<ChangeDoorMsg>(ChangeDoorRequest, MyMessagePermissions.ToServer, MyTransportMessageEnum.Request);
            MySyncLayer.RegisterMessage<ChangeDoorMsg>(ChangeDoorSuccess, MyMessagePermissions.FromServer, MyTransportMessageEnum.Success);
            MySyncLayer.RegisterMessage<ChangeDoorMsg>(ChangeDoorFailure, MyMessagePermissions.FromServer, MyTransportMessageEnum.Failure);
        }

        public MySyncDoor(MyDoor block)
        {
            m_block = block;
        }

        public void SendChangeDoorRequest(bool open, long identityId)
        {
            var msg = new ChangeDoorMsg();

            msg.EntityId = m_block.EntityId;
            msg.PlayerId = identityId;
            msg.Open     = open;

            Sync.Layer.SendMessageToServer(ref msg, MyTransportMessageEnum.Request);
        }

        static void ChangeDoorRequest(ref ChangeDoorMsg msg, MyNetworkClient sender)
        {
            MyEntity entity;
            MyEntities.TryGetEntityById(msg.EntityId, out entity);
            if (entity is MyDoor)
            {
                MyRelationsBetweenPlayerAndBlock relation = MyRelationsBetweenPlayerAndBlock.NoOwnership;
                var cubeBlock = entity as MyCubeBlock;
                if (cubeBlock != null)
                {
                    relation = cubeBlock.GetUserRelationToOwner(msg.PlayerId);
                }

                if (relation.IsFriendly())
                {
                    Sync.Layer.SendMessageToAllAndSelf(ref msg, MyTransportMessageEnum.Success);
                }
            }
        }

        static void ChangeDoorSuccess(ref ChangeDoorMsg msg, MyNetworkClient sender)
        {
            MyEntity entity;
            MyEntities.TryGetEntityById(msg.EntityId, out entity);
            MyDoor block = entity as MyDoor;
            if (block != null)
            {
                block.Open = msg.Open;
            }
        }

        static void ChangeDoorFailure(ref ChangeDoorMsg msg, MyNetworkClient sender)
        {
        }
    }
}
