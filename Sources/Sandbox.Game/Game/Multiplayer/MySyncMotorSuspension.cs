using Sandbox.Engine.Multiplayer;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.World;
using SteamSDK;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sandbox.Game.Multiplayer
{
    [PreloadRequired]
    class MySyncMotorSuspension : MySyncMotorBase
    {
        [MessageId(224, P2PMessageEnum.Reliable)]
        struct SteeringMsg : IEntityMessage
        {
            public long EntityId;

            public long GetEntityId()
            {
                return EntityId;
            }

            public BoolBlit Steering;
        }

        [MessageId(227, P2PMessageEnum.Reliable)]
        struct PropulsionMsg : IEntityMessage
        {
            public long EntityId;

            public long GetEntityId()
            {
                return EntityId;
            }

            public BoolBlit Propulsion;
        }

        [MessageId(225, P2PMessageEnum.Reliable)]
        struct DampingMsg : IEntityMessage
        {
            public long EntityId;

            public long GetEntityId()
            {
                return EntityId;
            }

            public float Damping;
        }

        [MessageId(226, P2PMessageEnum.Reliable)]
        struct StrengthMsg : IEntityMessage
        {
            public long EntityId;

            public long GetEntityId()
            {
                return EntityId;
            }

            public float Strength;
        }

        [MessageId(228, P2PMessageEnum.Reliable)]
        struct FrictionMsg : IEntityMessage
        {
            public long EntityId;

            public long GetEntityId()
            {
                return EntityId;
            }

            public float Friction;
        }

        [MessageId(229, P2PMessageEnum.Reliable)]
        struct PowerMsg : IEntityMessage
        {
            public long EntityId;

            public long GetEntityId()
            {
                return EntityId;
            }

            public float Power;
        }

        [MessageId(230, P2PMessageEnum.Reliable)]
        struct SteerMsg : IEntityMessage
        {
            public long EntityId;

            public long GetEntityId()
            {
                return EntityId;
            }

            public float Steer;
        }

        [MessageId(239, P2PMessageEnum.Reliable)]
        struct HeightMsg : IEntityMessage
        {
            public long EntityId;

            public long GetEntityId()
            {
                return EntityId;
            }

            public float Height;
        }

        static MySyncMotorSuspension()
        {
            MySyncLayer.RegisterEntityMessage<MySyncMotorSuspension, SteeringMsg>(OnChangeControllable, MyMessagePermissions.Any);
            MySyncLayer.RegisterEntityMessage<MySyncMotorSuspension, DampingMsg>(OnChangeDamping, MyMessagePermissions.Any);
            MySyncLayer.RegisterEntityMessage<MySyncMotorSuspension, StrengthMsg>(OnChangeStrength, MyMessagePermissions.Any);
            MySyncLayer.RegisterEntityMessage<MySyncMotorSuspension, PropulsionMsg>(OnChangePropulsion, MyMessagePermissions.Any);
            MySyncLayer.RegisterEntityMessage<MySyncMotorSuspension, FrictionMsg>(OnChangeFriction, MyMessagePermissions.Any);
            MySyncLayer.RegisterEntityMessage<MySyncMotorSuspension, PowerMsg>(OnChangePower, MyMessagePermissions.Any);
            MySyncLayer.RegisterEntityMessage<MySyncMotorSuspension, SteerMsg>(OnUpdateSteer, MyMessagePermissions.Any);
            MySyncLayer.RegisterEntityMessage<MySyncMotorSuspension, HeightMsg>(OnChangeHeight, MyMessagePermissions.Any);
        }

        public new MyMotorSuspension Entity
        {
            get { return (MyMotorSuspension)base.Entity; }
        }

        public MySyncMotorSuspension(MyMotorSuspension block)
            :base(block)
        {
        }

        public void ChangeSteering(bool value)
        {
            var msg = new SteeringMsg();
            msg.EntityId = Entity.EntityId;
            msg.Steering = value;

            Sync.Layer.SendMessageToAllAndSelf(ref msg);
        }

        static void OnChangeControllable(MySyncMotorSuspension sync, ref SteeringMsg msg, MyNetworkClient sender)
        {
            sync.Entity.Steering = msg.Steering;
        }


        internal void ChangeDamping(float v)
        {
            var msg = new DampingMsg();
            msg.EntityId = Entity.EntityId;
            msg.Damping = v;

            Sync.Layer.SendMessageToAllAndSelf(ref msg);
        }

        static void OnChangeDamping(MySyncMotorSuspension sync, ref DampingMsg msg, MyNetworkClient sender)
        {
            sync.Entity.Damping = msg.Damping;
        }

        internal void ChangeStrength(float v)
        {
            var msg = new StrengthMsg();
            msg.EntityId = Entity.EntityId;
            msg.Strength = v;

            Sync.Layer.SendMessageToAllAndSelf(ref msg);
        }

        static void OnChangeStrength(MySyncMotorSuspension sync, ref StrengthMsg msg, MyNetworkClient sender)
        {
            sync.Entity.Strength = msg.Strength;
        }

        internal void ChangePropulsion(bool v)
        {
            var msg = new PropulsionMsg();
            msg.EntityId = Entity.EntityId;
            msg.Propulsion = v;

            Sync.Layer.SendMessageToAllAndSelf(ref msg);
        }

        static void OnChangePropulsion(MySyncMotorSuspension sync, ref PropulsionMsg msg, MyNetworkClient sender)
        {
            sync.Entity.Propulsion = msg.Propulsion;
        }

        internal void ChangeFriction(float v)
        {
            var msg = new FrictionMsg();
            msg.EntityId = Entity.EntityId;
            msg.Friction = v;

            Sync.Layer.SendMessageToAllAndSelf(ref msg);
        }

        static void OnChangeFriction(MySyncMotorSuspension sync, ref FrictionMsg msg, MyNetworkClient sender)
        {
            sync.Entity.Friction = msg.Friction;
        }


        internal void ChangePower(float v)
        {
            var msg = new PowerMsg();
            msg.EntityId = Entity.EntityId;
            msg.Power = v;

            Sync.Layer.SendMessageToAllAndSelf(ref msg);
        }

        static void OnChangePower(MySyncMotorSuspension sync, ref PowerMsg msg, MyNetworkClient sender)
        {
            sync.Entity.Power = msg.Power;
        }

        internal void UpdateSteer(float angle)
        {
            var msg = new SteerMsg();
            msg.EntityId = Entity.EntityId;
            msg.Steer = angle;

            Sync.Layer.SendMessageToAll(ref msg);
        }

        static void OnUpdateSteer(MySyncMotorSuspension sync, ref SteerMsg msg, MyNetworkClient sender)
        {
            sync.Entity.SteerAngle = msg.Steer;
        }

        internal void ChangeHeight(float v)
        {
            var msg = new HeightMsg();
            msg.EntityId = Entity.EntityId;
            msg.Height = v;

            Sync.Layer.SendMessageToAllAndSelf(ref msg);
        }

        static void OnChangeHeight(MySyncMotorSuspension sync, ref HeightMsg msg, MyNetworkClient sender)
        {
            sync.Entity.Height = msg.Height;
        }
    }
}
