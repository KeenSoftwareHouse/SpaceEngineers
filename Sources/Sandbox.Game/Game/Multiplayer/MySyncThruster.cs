using System;
using Sandbox.Engine.Multiplayer;
using Sandbox.Game.Entities;
using Sandbox.Game.World;
using SteamSDK;
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

        [MessageId(7517, P2PMessageEnum.Reliable)]
        protected struct ChangeThrustColorMsg : IEntityMessage
        {
            public long EntityId;
            public long GetEntityId()
            {
                return EntityId;
            }

            public Color ThrustColor;
        }

        static MySyncThruster()
        {
            MySyncLayer.RegisterMessage<ChangeThrustOverrideMsg>(ChangeThrustOverrideSuccess, MyMessagePermissions.Any, MyTransportMessageEnum.Success);
            MySyncLayer.RegisterMessage<ChangeThrustColorMsg>(ChangeThrustColorSuccess, MyMessagePermissions.Any, MyTransportMessageEnum.Success);
        }

        public MySyncThruster(MyThrust block)
        {
            m_block = block;
        }

        public void SendChangeThrustOverrideRequest(float thrustOverride)
        {
            ChangeThrustOverrideMsg msg = new ChangeThrustOverrideMsg
            {
                EntityId = m_block.EntityId,
                ThrustOverride = thrustOverride
            };

            Sync.Layer.SendMessageToAll(ref msg, MyTransportMessageEnum.Success);
        }

        static void ChangeThrustOverrideSuccess(ref ChangeThrustOverrideMsg msg, MyNetworkClient sender)
        {
            MyEntity entity;
            MyEntities.TryGetEntityById(msg.EntityId, out entity);
            MyThrust block = entity as MyThrust;
            if (block != null)
                block.SetThrustOverride(msg.ThrustOverride);
        }

        public void SendChangeThrustColorRequest(Color color)
        {
            var msg = new ChangeThrustColorMsg
            {
                EntityId = m_block.EntityId,
                ThrustColor = color
            };


            Sync.Layer.SendMessageToServer(ref msg, MyTransportMessageEnum.Success);
        }

        static void ChangeThrustColorSuccess(ref ChangeThrustColorMsg msg, MyNetworkClient sender)
        {
            MyEntity entity;
            MyEntities.TryGetEntityById(msg.EntityId, out entity);
            var block = entity as MyThrust;
            if (block != null)
            {
                block.SetFlameColor(msg.ThrustColor);
            }

        }

    }
}
