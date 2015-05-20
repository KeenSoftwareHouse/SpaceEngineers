using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
        MyShipDrill m_block;

        [MessageIdAttribute(8201, P2PMessageEnum.Reliable)]
        protected struct ChangePushFactorMsg : IEntityMessage
        {
            public long EntityId;

            public long GetEntityId() { return EntityId; }

            public float PushFactor;

            public override string ToString()
            {
                return String.Format("{0}, {1}", this.GetType().Name, this.GetEntityText());
            }
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
