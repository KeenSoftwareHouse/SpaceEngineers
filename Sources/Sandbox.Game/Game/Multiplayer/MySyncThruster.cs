using Sandbox.Engine.Multiplayer;
using Sandbox.Game.Entities;
using Sandbox.Game.World;
using SteamSDK;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using VRageMath;

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

        [MessageId(7317, P2PMessageEnum.Reliable)]
        protected struct ChangeFlameColorMsg : IEntityMessage
        {
            public long EntityId;

            public long GetEntityId() { return EntityId; }

            public Color Color;
            public override string ToString()
            {
                return String.Format("{0}, {1}", this.GetType().Name, this.GetEntityText());
            }
        }

        static MySyncThruster()
        {
            MySyncLayer.RegisterMessage<ChangeThrustOverrideMsg>(ChangeThrustOverrideSuccess, MyMessagePermissions.Any, MyTransportMessageEnum.Success);

            MySyncLayer.RegisterMessage<ChangeFlameColorMsg>(ChangeFlameColorRequest, MyMessagePermissions.ToServer, MyTransportMessageEnum.Request);
            MySyncLayer.RegisterMessage<ChangeFlameColorMsg>(ChangeFlameColorSuccess, MyMessagePermissions.FromServer, MyTransportMessageEnum.Success);
        }

        public MySyncThruster(MyThrust block)
        {
            m_block = block;
        }


        public void SendChangeFlameColorRequest(Color color)
        {
            var msg = new ChangeFlameColorMsg();

            msg.EntityId = m_block.EntityId;
            msg.Color = color;

            Sync.Layer.SendMessageToServer(ref msg, MyTransportMessageEnum.Request);
        }
        static void ChangeFlameColorRequest(ref ChangeFlameColorMsg msg, MyNetworkClient sender)
        {
            MyEntity entity;
            MyEntities.TryGetEntityById(msg.EntityId, out entity);
            if (entity is MyThrust)
            {
                Sync.Layer.SendMessageToAllAndSelf(ref msg, MyTransportMessageEnum.Success);
            }
        }
        static void ChangeFlameColorSuccess(ref ChangeFlameColorMsg msg, MyNetworkClient sender)
        {
            MyEntity entity;
            MyEntities.TryGetEntityById(msg.EntityId, out entity);
            var thrust = entity as MyThrust;
            if (thrust != null)
            {
                thrust.ThrustColor = msg.Color;
            }
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
