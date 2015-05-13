using ProtoBuf;
using Sandbox.Engine.Multiplayer;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.World;
using SteamSDK;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace Sandbox.Game.Multiplayer
{
    [PreloadRequired]
    class MySyncMotorStator : MySyncMotorBase
    {
        [MessageId(222, P2PMessageEnum.Reliable)]
        struct ChangeStatorTorqueMsg : IEntityMessage
        {
            public long EntityId;
            public long GetEntityId() { return EntityId; }
            public float Torque;
        }
        [MessageId(235, P2PMessageEnum.Reliable)]
        struct ChangeStatorBrakingTorqueMsg : IEntityMessage
        {
            public long EntityId;
            public long GetEntityId() { return EntityId; }
            public float BrakingTorque;
        }
        [MessageId(236, P2PMessageEnum.Reliable)]
        struct ChangeStatorTargetVelocityMsg : IEntityMessage
        {
            public long EntityId;
            public long GetEntityId() { return EntityId; }
            public float TargetVelocity;
        }
        [MessageId(237, P2PMessageEnum.Reliable)]
        struct ChangeStatorMinAngleMsg : IEntityMessage
        {
            public long EntityId;
            public long GetEntityId() { return EntityId; }
            public float MinAngleNormalized;
        }
        [MessageId(238, P2PMessageEnum.Reliable)]
        struct ChangeStatorMaxAngleMsg : IEntityMessage
        {
            public long EntityId;
            public long GetEntityId() { return EntityId; }
            public float MaxAngleNormalized;
        }

        [MessageId(231, P2PMessageEnum.Reliable)]
        struct SetRotorDisplacementMsg : IEntityMessage
        {
            public long EntityId;
            public long GetEntityId() { return EntityId; }

            public float Displacement;
        }

        [MessageId(232, SteamSDK.P2PMessageEnum.Unreliable)]
        struct SetCurrentAngleMsg : IEntityMessage
        {
            public long EntityId;
            public long GetEntityId() { return EntityId; }

            public float CurrentAngle;
        }

        [MessageId(233, SteamSDK.P2PMessageEnum.Unreliable)]
        struct AttachRotorMsg : IEntityMessage
        {
            public long EntityId;
            public long GetEntityId() { return EntityId; }
        }

        [MessageId(234, SteamSDK.P2PMessageEnum.Unreliable)]
        struct DetachRotorMsg : IEntityMessage
        {
            public long EntityId;
            public long GetEntityId() { return EntityId; }
        }

        static MySyncMotorStator()
        {
            MySyncLayer.RegisterEntityMessage<MySyncMotorStator, ChangeStatorTorqueMsg>(OnChangeStatorTorque, MyMessagePermissions.FromServer | MyMessagePermissions.ToServer);
            MySyncLayer.RegisterEntityMessage<MySyncMotorStator, ChangeStatorBrakingTorqueMsg>(OnChangeStatorBrakingTorque, MyMessagePermissions.FromServer | MyMessagePermissions.ToServer);
            MySyncLayer.RegisterEntityMessage<MySyncMotorStator, ChangeStatorTargetVelocityMsg>(OnChangeStatorTargetVelocity, MyMessagePermissions.FromServer | MyMessagePermissions.ToServer);
            MySyncLayer.RegisterEntityMessage<MySyncMotorStator, ChangeStatorMinAngleMsg>(OnChangeStatorMinAngle, MyMessagePermissions.FromServer | MyMessagePermissions.ToServer);
            MySyncLayer.RegisterEntityMessage<MySyncMotorStator, ChangeStatorMaxAngleMsg>(OnChangeStatorMaxAngle, MyMessagePermissions.FromServer | MyMessagePermissions.ToServer);

            MySyncLayer.RegisterEntityMessage<MySyncMotorStator, SetRotorDisplacementMsg>(OnChangeRotorDisplacement, MyMessagePermissions.FromServer | MyMessagePermissions.ToServer);
            MySyncLayer.RegisterEntityMessage<MySyncMotorStator, SetCurrentAngleMsg>(OnSetCurrentAngle, MyMessagePermissions.Any);
            MySyncLayer.RegisterEntityMessage<MySyncMotorStator, DetachRotorMsg>(OnDetachRotor, MyMessagePermissions.Any);
            MySyncLayer.RegisterEntityMessage<MySyncMotorStator, AttachRotorMsg>(OnAttachRotor, MyMessagePermissions.Any);
        
        }

        public Action<float> SetAngle;

        public MySyncMotorStator(MyMotorStator stator)
            : base(stator)
        {
        }

        public void ChangeStatorTorque(float torque)
        {
            var msg = new ChangeStatorTorqueMsg();
            msg.EntityId = Entity.EntityId;
            msg.Torque = torque;
            Sync.Layer.SendMessageToServer(ref msg);
        }
        public void ChangeStatorBrakingTorque(float brakingTorque)
        {
            var msg = new ChangeStatorBrakingTorqueMsg();
            msg.EntityId = Entity.EntityId;
            msg.BrakingTorque = brakingTorque;
            Sync.Layer.SendMessageToServer(ref msg);
        }
        public void ChangeStatorTargetVelocity(float targetVelocity)
        {
            var msg = new ChangeStatorTargetVelocityMsg();
            msg.EntityId = Entity.EntityId;
            msg.TargetVelocity = targetVelocity;
            Sync.Layer.SendMessageToServer(ref msg);
        }
        public void ChangeStatorMinAngle(float minAngleNormalized)
        {
            var msg = new ChangeStatorMinAngleMsg();
            msg.EntityId = Entity.EntityId;
            msg.MinAngleNormalized = minAngleNormalized;
            Sync.Layer.SendMessageToServer(ref msg);
        }
        public void ChangeStatorMaxAngle(float maxAngleNormalized)
        {
            var msg = new ChangeStatorMaxAngleMsg();
            msg.EntityId = Entity.EntityId;
            msg.MaxAngleNormalized = maxAngleNormalized;
            Sync.Layer.SendMessageToServer(ref msg);
        }

        private static void OnChangeStatorTorque(MySyncMotorStator block, ref ChangeStatorTorqueMsg msg, MyNetworkClient sender)
        {
            MyMotorStator stator = (MyMotorStator)block.Entity;
            stator.Torque = msg.Torque;
            if (Sync.IsServer)
                Sync.Layer.SendMessageToAll(ref msg);
        }
        private static void OnChangeStatorBrakingTorque(MySyncMotorStator block, ref ChangeStatorBrakingTorqueMsg msg, MyNetworkClient sender)
        {
            MyMotorStator stator = (MyMotorStator)block.Entity;
            stator.BrakingTorque = msg.BrakingTorque;
            if (Sync.IsServer)
                Sync.Layer.SendMessageToAll(ref msg);
        }
        private static void OnChangeStatorTargetVelocity(MySyncMotorStator block, ref ChangeStatorTargetVelocityMsg msg, MyNetworkClient sender)
        {
            MyMotorStator stator = (MyMotorStator)block.Entity;
            stator.TargetVelocity = msg.TargetVelocity;
            if (Sync.IsServer)
                Sync.Layer.SendMessageToAll(ref msg);
        }
        private static void OnChangeStatorMinAngle(MySyncMotorStator block, ref ChangeStatorMinAngleMsg msg, MyNetworkClient sender)
        {
            MyMotorStator stator = (MyMotorStator)block.Entity;
            stator.MinAngle = msg.MinAngleNormalized;
            if (Sync.IsServer)
                Sync.Layer.SendMessageToAll(ref msg);
        }
        private static void OnChangeStatorMaxAngle(MySyncMotorStator block, ref ChangeStatorMaxAngleMsg msg, MyNetworkClient sender)
        {
            MyMotorStator stator = (MyMotorStator)block.Entity;
            stator.MaxAngle = msg.MaxAngleNormalized;
            if (Sync.IsServer)
                Sync.Layer.SendMessageToAll(ref msg);
        }

        public void ChangeRotorDisplacement(float newDisplacement)
        {
            var msg = new SetRotorDisplacementMsg();
            msg.EntityId = Entity.EntityId;
            msg.Displacement = newDisplacement;

            Sync.Layer.SendMessageToServer(ref msg);
        }

        public void DetachRotor()
        {
            var msg = new DetachRotorMsg();
            msg.EntityId = Entity.EntityId;
            Sync.Layer.SendMessageToServer(ref msg);
        }

        public void AttachRotor()
        {
            var msg = new AttachRotorMsg();
            msg.EntityId = Entity.EntityId;
            Sync.Layer.SendMessageToServer(ref msg);
        }

        private static void OnChangeRotorDisplacement(MySyncMotorStator sync, ref SetRotorDisplacementMsg msg, MyNetworkClient sender)
        {
            MyMotorStator stator = (MyMotorStator)sync.Entity;
            stator.DummyDisplacement = msg.Displacement;

            if (Sync.IsServer)
            {
                Sync.Layer.SendMessageToAll(ref msg);
            }
        }

        internal void SetCurrentAngle(float currentAngle)
        {
            var msg = new SetCurrentAngleMsg();
            msg.EntityId = Entity.EntityId;
            msg.CurrentAngle = currentAngle;

            Sync.Layer.SendMessageToAll<SetCurrentAngleMsg>(ref msg);
        }

        static void OnSetCurrentAngle(MySyncMotorStator sync, ref SetCurrentAngleMsg msg, MyNetworkClient sender)
        {
            sync.SetAngle(msg.CurrentAngle);
        }

        private static void OnDetachRotor(MySyncMotorStator block, ref DetachRotorMsg msg, MyNetworkClient sender)
        {
            MyMotorStator stator = (MyMotorStator)block.Entity;
            stator.DetachRotor();
            if (Sync.IsServer)
            {
                Sync.Layer.SendMessageToAll(ref msg);
            }
        }

        private static void OnAttachRotor(MySyncMotorStator block, ref AttachRotorMsg msg, MyNetworkClient sender)
        {
            MyMotorStator stator = (MyMotorStator)block.Entity;
            stator.AttachRotor();
            if (Sync.IsServer)
            {
                Sync.Layer.SendMessageToAll(ref msg);
            }
        }
    }
}
