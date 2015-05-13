using Sandbox.Engine.Multiplayer;
using Sandbox.Game.Entities;
using Sandbox.Game.World;
using SteamSDK;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sandbox.Game.Multiplayer
{
    [PreloadRequired]
    class MySyncThruster
    {
        private MyThrust m_block;

        [MessageId(7416, P2PMessageEnum.Reliable)]
        protected struct ChangeThrustOverrideMsg : IEntityMessage
        {
            public long EntityId;
            public long GetEntityId() { return EntityId; }

            public float ThrustOverride;
        }

        static MySyncThruster()
        {
            MySyncLayer.RegisterMessage<ChangeThrustOverrideMsg>(ChangeThrustOverrideSuccess, MyMessagePermissions.Any, MyTransportMessageEnum.Success);
        }

        public MySyncThruster(MyThrust block)
        {
            m_block = block;
        }

        public void SendChangeThrustOverrideRequest(float thrustOverride)
        {
            var msg = new ChangeThrustOverrideMsg();
            msg.EntityId = m_block.EntityId;
            msg.ThrustOverride = thrustOverride;

            Sync.Layer.SendMessageToAll(ref msg, MyTransportMessageEnum.Success);
        }

        static void ChangeThrustOverrideSuccess(ref ChangeThrustOverrideMsg msg, MyNetworkClient sender)
        {
            MyEntity entity;
            MyEntities.TryGetEntityById(msg.EntityId, out entity);
            var block = entity as MyThrust;
            if (block != null)
                block.SetThrustOverride(msg.ThrustOverride);
        }

    }
}
