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

            public long InterlockTargetId;

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
            msg.InterlockTargetId = m_block.InterlockTargetId ?? 0;

            Sync.Layer.SendMessageToServer(ref msg, MyTransportMessageEnum.Request);
        }

        public void PasteCoordinates(string clipboardText, long identityId)
        {
            StringBuilder termName = new StringBuilder();
            Vector3D temp = new Vector3D();
            if (MyGpsCollection.ParseOneGPS(clipboardText, termName, ref temp))
            {
                MyDoor targetDoor = m_block.CubeGrid.GetCubeBlock(new Vector3I(temp)).FatBlock as MyDoor;
                if (targetDoor != null)
                {
                    var msg1 = new ChangeDoorMsg
                    {
                        EntityId = m_block.EntityId,
                        PlayerId = identityId,
                        Open = m_block.Open,
                        InterlockTargetId = targetDoor.EntityId
                    };
                    Sync.Layer.SendMessageToServer(ref msg1, MyTransportMessageEnum.Request);

                    var msg2 = new ChangeDoorMsg
                    {
                        EntityId = targetDoor.EntityId,
                        PlayerId = targetDoor.OwnerId,
                        Open = targetDoor.Open,
                        InterlockTargetId = m_block.EntityId
                    };
                    Sync.Layer.SendMessageToServer(ref msg2, MyTransportMessageEnum.Request);

                }
            }
        }
        public void ClearTarget(long identityId)
        {
            var msg1 = new ChangeDoorMsg
            {
                EntityId = m_block.EntityId,
                PlayerId = identityId,
                Open = m_block.Open,
                InterlockTargetId = -1
            };

            Sync.Layer.SendMessageToServer(ref msg1, MyTransportMessageEnum.Request);

            if (m_block.InterlockTargetId.HasValue)
            {
                var msg2 = new ChangeDoorMsg
                {
                    EntityId = m_block.InterlockTargetId.Value,
                    PlayerId = identityId,
                    Open = m_block.Open,
                    InterlockTargetId = -1
                };

                Sync.Layer.SendMessageToServer(ref msg2, MyTransportMessageEnum.Request);
            }
        }

        static void ChangeDoorRequest(ref ChangeDoorMsg msg, MyNetworkClient sender)
        {
            MyEntity entity;
            MyEntities.TryGetEntityById(msg.EntityId, out entity);
            if (entity is MyDoor)
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
            MyDoor block = entity as MyDoor;
            if (block != null)
            {
                block.Open = msg.Open;
                block.InterlockTargetId = msg.InterlockTargetId;
            }
        }

        static void ChangeDoorFailure(ref ChangeDoorMsg msg, MyNetworkClient sender)
        {
        }



     
    }
}
