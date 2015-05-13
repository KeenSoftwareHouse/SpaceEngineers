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
    class MySyncGyro 
    {
        private MyGyro m_block;

        [MessageId(7586, P2PMessageEnum.Reliable)]
        protected struct ChangeGyroPowerMsg : IEntityMessage
        {
            public long EntityId;
            public long GetEntityId() { return EntityId; }

            public float GyroPower;
        }

        [MessageId(7587, P2PMessageEnum.Reliable)]
        protected struct OverrideGyroControlMsg : IEntityMessage
        {
            public long EntityId;
            public long GetEntityId() { return EntityId; }

            public BoolBlit Override;
        }

        [MessageId(7588, P2PMessageEnum.Reliable)]
        protected struct OverrideGyroTorqueMsg : IEntityMessage
        {
            public long EntityId;
            public long GetEntityId() { return EntityId; }

            public Vector3 Torque;
        }

        static MySyncGyro()
        {
            MySyncLayer.RegisterMessage<ChangeGyroPowerMsg>(ChangeGyroPowerSuccess, MyMessagePermissions.Any, MyTransportMessageEnum.Success);
            MySyncLayer.RegisterMessage<OverrideGyroControlMsg>(OverrideGyroControlSuccess, MyMessagePermissions.Any, MyTransportMessageEnum.Success);
            MySyncLayer.RegisterMessage<OverrideGyroTorqueMsg>(OverrideGyroTorqueSuccess, MyMessagePermissions.Any, MyTransportMessageEnum.Success);
        }

        public MySyncGyro(MyGyro block)
        {
            m_block = block;
        }

        public void SendChangeGyroPowerRequest(float gyroPower)
        {
            var msg = new ChangeGyroPowerMsg();
            msg.EntityId = m_block.EntityId;
            msg.GyroPower = gyroPower;

            Sync.Layer.SendMessageToAll(ref msg, MyTransportMessageEnum.Success);
        }

        static void ChangeGyroPowerSuccess(ref ChangeGyroPowerMsg msg, MyNetworkClient sender)
        {
            MyEntity entity;
            MyEntities.TryGetEntityById(msg.EntityId, out entity);
            var block = entity as MyGyro;
            if (block != null)
                block.GyroPower = msg.GyroPower;
        }

        public void SendGyroOverrideRequest(bool v)
        {
            var msg = new OverrideGyroControlMsg();
            msg.EntityId = m_block.EntityId;
            msg.Override = v;

            Sync.Layer.SendMessageToAll(ref msg, MyTransportMessageEnum.Success);
        }

        static void OverrideGyroControlSuccess(ref OverrideGyroControlMsg msg, MyNetworkClient sender)
        {
            MyEntity entity;
            MyEntities.TryGetEntityById(msg.EntityId, out entity);
            var block = entity as MyGyro;
            if (block != null)
                block.SetGyroOverride(msg.Override);
        }

        public void SendGyroTorqueRequest(Vector3 torque)
        {
            var msg = new OverrideGyroTorqueMsg();
            msg.EntityId = m_block.EntityId;
            msg.Torque = torque;

            Sync.Layer.SendMessageToAll(ref msg, MyTransportMessageEnum.Success);
        }

        static void OverrideGyroTorqueSuccess(ref OverrideGyroTorqueMsg msg, MyNetworkClient sender)
        {
            MyEntity entity;
            MyEntities.TryGetEntityById(msg.EntityId, out entity);
            var block = entity as MyGyro;
            if (block != null)
                block.SetGyroTorque(msg.Torque);
        }
    }
}
