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

        static MySyncThruster()
        {
            MySyncLayer.RegisterMessage<      ChangeThrustOverrideMsg>(ChangeThrustOverrideSuccess, MyMessagePermissions.Any, MyTransportMessageEnum.Success);
            MySyncLayer.RegisterMessage<    ChangeThrustLinearModeMsg>(         OnLinearModeChange, MyMessagePermissions.Any, MyTransportMessageEnum.Success);
            MySyncLayer.RegisterMessage<ChangeThrustRotationalModeMsg>(     OnRotationalModeChange, MyMessagePermissions.Any, MyTransportMessageEnum.Success);
        }

        public MySyncThruster(MyThrust block)
        {
            m_block = block;
        }

        [MessageId(7416, P2PMessageEnum.Reliable)]
        protected struct ChangeThrustOverrideMsg : IEntityMessage
        {
            public long EntityId;
            public long GetEntityId() { return EntityId; }

            public float ThrustOverride;
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

        [MessageId(7421, P2PMessageEnum.Reliable)]
        protected struct ChangeThrustLinearModeMsg : IEntityMessage
        {
            public long EntityId;
            public long GetEntityId() { return EntityId; }

            public BoolBlit linearModeOn;
        }

        public void SendLinearModeChangeRequest(bool linearModeOn)
        {
            //m_block.SetLinearMode(linearModeOn);

            var msg = new ChangeThrustLinearModeMsg();
            msg.EntityId = m_block.EntityId;
            msg.linearModeOn = linearModeOn;

            Sync.Layer.SendMessageToAllAndSelf(ref msg, MyTransportMessageEnum.Success);
        }

        static void OnLinearModeChange(ref ChangeThrustLinearModeMsg msg, MyNetworkClient sender)
        {
            MyEntity entity;
            MyEntities.TryGetEntityById(msg.EntityId, out entity);
            var block = entity as MyThrust;
            if (block != null)
                block.SetLinearMode(msg.linearModeOn);
        }

        [MessageId(7422, P2PMessageEnum.Reliable)]
        protected struct ChangeThrustRotationalModeMsg : IEntityMessage
        {
            public long EntityId;
            public long GetEntityId() { return EntityId; }

            public BoolBlit rotationalModeOn;
        }

        public void SendRotationalModeChangeRequest(bool rotationalModeOn)
        {
            //m_block.SetRotationalMode(rotationalModeOn);
            
            var msg = new ChangeThrustRotationalModeMsg();
            msg.EntityId = m_block.EntityId;
            msg.rotationalModeOn = rotationalModeOn;

            Sync.Layer.SendMessageToAllAndSelf(ref msg, MyTransportMessageEnum.Success);
        }

        static void OnRotationalModeChange(ref ChangeThrustRotationalModeMsg msg, MyNetworkClient sender)
        {
            MyEntity entity;
            MyEntities.TryGetEntityById(msg.EntityId, out entity);
            var block = entity as MyThrust;
            if (block != null)
                block.SetRotationalMode(msg.rotationalModeOn);
        }
    }
}
