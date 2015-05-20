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
        protected struct ChangePushFactorMsg
        {
            public long EntityId;

            public float PushFactor;
        }

        static MySyncShipDrill()
        {
            MySyncLayer.RegisterMessage<ChangePushFactorMsg>(ChangePushFactorSuccess, MyMessagePermissions.Any, MyTransportMessageEnum.Success);
        }

        public MySyncShipDrill(MyShipDrill block)
        {
            m_block = block;
        }

        public void SendChangePushFactorRequest(float PushFactor)
        {
            var msg = new ChangePushFactorMsg();
            msg.EntityId = m_block.EntityId;
            msg.PushFactor = PushFactor;

            Sync.Layer.SendMessageToAll(ref msg, MyTransportMessageEnum.Success);
        }

        static void ChangePushFactorSuccess(ref ChangePushFactorMsg msg, MyNetworkClient sender)
        {
            MyEntity entity;
            MyEntities.TryGetEntityById(msg.EntityId, out entity);
            var block = entity as MyShipDrill;
            if (block != null)
            {
                block.PushItemsFactor = msg.PushFactor;
            }
        }
    }
}
