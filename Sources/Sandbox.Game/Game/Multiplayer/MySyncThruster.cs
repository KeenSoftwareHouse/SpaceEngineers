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

        [MessageId(7414, P2PMessageEnum.Reliable)]
        protected struct ChangeThrustOverrideMsg : IEntityMessage
        {
            public long EntityId;
            public long GetEntityId() { return EntityId; }

            public float ThrustOverride;
        }

        static MySyncThruster()
        {
            MySyncLayer.RegisterMessage<ChangeThrustOverrideMsg>(ChangeThrustOverrideSuccess, MyMessagePermissions.ToServer | MyMessagePermissions.FromServer, MyTransportMessageEnum.Success);
        }

        public MySyncThruster(MyThrust block)
        {
            m_block = block;
        }

        public void SendChangeThrustOverrideRequest(float thrustOverride)
        {
            return;
            var msg = new ChangeThrustOverrideMsg();
            msg.EntityId = m_block.EntityId;
            msg.ThrustOverride = thrustOverride;
            Sync.Layer.SendMessageToServer(ref msg, MyTransportMessageEnum.Success);
        }

        static void ChangeThrustOverrideSuccess(ref ChangeThrustOverrideMsg msg, MyNetworkClient sender)
        {
            return;
            MyEntity entity;
            MyEntities.TryGetEntityById(msg.EntityId, out entity);
            var block = entity as MyThrust;
            if (block != null)
            {
                block.SetThrustOverride(msg.ThrustOverride);
                // Prototype: other clients will get it by StateSync
                //if (Sync.IsServer)
                //{
                //    Sync.Layer.SendMessageToAllButOne(ref msg, sender.SteamUserId);
                //}
            }
        }

    }
}
