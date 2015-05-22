using System;
using Sandbox.Game.Weapons;
using Sandbox.Engine.Multiplayer;
using SteamSDK;
using Sandbox.Game.World;
using Sandbox.Game.Entities;

namespace Sandbox.Game.Multiplayer
{
    [PreloadRequired]
    class MySyncShipDrill
    {
        private MyShipDrill m_block;

        [MessageIdAttribute(821, P2PMessageEnum.Reliable)]
        protected struct ChangeUsePushItemsMsg
        {
            public long EntityId;

            public BoolBlit UsePushItems;
        }

        static MySyncShipDrill()
        {
            MySyncLayer.RegisterMessage<ChangeUsePushItemsMsg>(ChangeUsePushItemsSuccess, MyMessagePermissions.Any, MyTransportMessageEnum.Success);
        }

        public MySyncShipDrill(MyShipDrill block)
        {
            m_block = block;
        }

        public void SendChangeUsePushItemsRequest(bool UsePushItems)
        {
            var msg = new ChangeUsePushItemsMsg();
            msg.EntityId = m_block.EntityId;
            msg.UsePushItems = UsePushItems;

            Sync.Layer.SendMessageToAll(ref msg, MyTransportMessageEnum.Success);
        }

        static void ChangeUsePushItemsSuccess(ref ChangeUsePushItemsMsg msg, MyNetworkClient sender)
        {
            MyEntity entity;
            MyEntities.TryGetEntityById(msg.EntityId, out entity);
            var block = entity as MyShipDrill;
            if (block != null)
            {
                block.UsePushItems = msg.UsePushItems;
            }
        }
    }
}
