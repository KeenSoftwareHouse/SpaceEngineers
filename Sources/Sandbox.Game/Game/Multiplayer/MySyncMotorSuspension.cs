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
    public class MySyncMotorSuspension : MySyncMotorBase
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
        struct UpdateSliderMsg : IEntityMessage
        {
            public long EntityId;

            public long GetEntityId()
            {
                return EntityId;
            }

            public SliderEnum Slider;
            public float Value;
        }

        public enum SliderEnum
        {
            Height,
            MaxSteerAngle,
            SteerSpeed,
            SteerReturnSpeed,
            InvertSteer,
            SuspensionTravel
        }

        static MySyncMotorSuspension()
        {
            MySyncLayer.RegisterEntityMessage<MySyncMotorSuspension, SteeringMsg>(OnChangeControllable, MyMessagePermissions.ToServer|MyMessagePermissions.FromServer|MyMessagePermissions.ToSelf);
            MySyncLayer.RegisterEntityMessage<MySyncMotorSuspension, DampingMsg>(OnChangeDamping, MyMessagePermissions.ToServer | MyMessagePermissions.FromServer | MyMessagePermissions.ToSelf);
            MySyncLayer.RegisterEntityMessage<MySyncMotorSuspension, StrengthMsg>(OnChangeStrength, MyMessagePermissions.ToServer | MyMessagePermissions.FromServer | MyMessagePermissions.ToSelf);
            MySyncLayer.RegisterEntityMessage<MySyncMotorSuspension, PropulsionMsg>(OnChangePropulsion, MyMessagePermissions.ToServer | MyMessagePermissions.FromServer | MyMessagePermissions.ToSelf);
            MySyncLayer.RegisterEntityMessage<MySyncMotorSuspension, FrictionMsg>(OnChangeFriction, MyMessagePermissions.ToServer | MyMessagePermissions.FromServer | MyMessagePermissions.ToSelf);
            MySyncLayer.RegisterEntityMessage<MySyncMotorSuspension, PowerMsg>(OnChangePower, MyMessagePermissions.ToServer | MyMessagePermissions.FromServer | MyMessagePermissions.ToSelf);
            MySyncLayer.RegisterEntityMessage<MySyncMotorSuspension, SteerMsg>(OnUpdateSteer, MyMessagePermissions.ToServer | MyMessagePermissions.FromServer);
            MySyncLayer.RegisterEntityMessage<MySyncMotorSuspension, UpdateSliderMsg>(OnUpdateSlider, MyMessagePermissions.ToServer | MyMessagePermissions.FromServer | MyMessagePermissions.ToSelf);
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

            Sync.Layer.SendMessageToServerAndSelf(ref msg);
        }

        static void OnChangeControllable(MySyncMotorSuspension sync, ref SteeringMsg msg, MyNetworkClient sender)
        {
            sync.Entity.Steering = msg.Steering;
            if (Sync.IsServer)
            {
                Sync.Layer.SendMessageToAllButOne(ref msg,sender.SteamUserId);
            }
        }


        internal void ChangeDamping(float v)
        {
            var msg = new DampingMsg();
            msg.EntityId = Entity.EntityId;
            msg.Damping = v;

            Sync.Layer.SendMessageToServerAndSelf(ref msg);
        }

        static void OnChangeDamping(MySyncMotorSuspension sync, ref DampingMsg msg, MyNetworkClient sender)
        {
            sync.Entity.Damping = msg.Damping;
            if (Sync.IsServer)
            {
                Sync.Layer.SendMessageToAllButOne(ref msg, sender.SteamUserId);
            }
        }

        internal void ChangeStrength(float v)
        {
            var msg = new StrengthMsg();
            msg.EntityId = Entity.EntityId;
            msg.Strength = v;

            Sync.Layer.SendMessageToServerAndSelf(ref msg);
        }

        static void OnChangeStrength(MySyncMotorSuspension sync, ref StrengthMsg msg, MyNetworkClient sender)
        {
            sync.Entity.Strength = msg.Strength;
            if (Sync.IsServer)
            {
                Sync.Layer.SendMessageToAllButOne(ref msg, sender.SteamUserId);
            }
        }

        internal void ChangePropulsion(bool v)
        {
            var msg = new PropulsionMsg();
            msg.EntityId = Entity.EntityId;
            msg.Propulsion = v;

            Sync.Layer.SendMessageToServerAndSelf(ref msg);
        }

        static void OnChangePropulsion(MySyncMotorSuspension sync, ref PropulsionMsg msg, MyNetworkClient sender)
        {
            sync.Entity.Propulsion = msg.Propulsion;
            if (Sync.IsServer)
            {
                Sync.Layer.SendMessageToAllButOne(ref msg, sender.SteamUserId);
            }
        }

        internal void ChangeFriction(float v)
        {
            var msg = new FrictionMsg();
            msg.EntityId = Entity.EntityId;
            msg.Friction = v;

            Sync.Layer.SendMessageToServerAndSelf(ref msg);
        }

        static void OnChangeFriction(MySyncMotorSuspension sync, ref FrictionMsg msg, MyNetworkClient sender)
        {
            sync.Entity.Friction = msg.Friction;
            if (Sync.IsServer)
            {
                Sync.Layer.SendMessageToAllButOne(ref msg, sender.SteamUserId);
            }
        }



        internal void ChangePower(float v)
        {
            var msg = new PowerMsg();
            msg.EntityId = Entity.EntityId;
            msg.Power = v;

            Sync.Layer.SendMessageToServerAndSelf(ref msg);
        }

        static void OnChangePower(MySyncMotorSuspension sync, ref PowerMsg msg, MyNetworkClient sender)
        {
            sync.Entity.Power = msg.Power;
            if (Sync.IsServer)
            {
                Sync.Layer.SendMessageToAllButOne(ref msg, sender.SteamUserId);
            }
        }

        internal void UpdateSteer(float angle)
        {
            var msg = new SteerMsg();
            msg.EntityId = Entity.EntityId;
            msg.Steer = angle;

            Sync.Layer.SendMessageToServer(ref msg);
        }

        static void OnUpdateSteer(MySyncMotorSuspension sync, ref SteerMsg msg, MyNetworkClient sender)
        {
            sync.Entity.SteerAngle = msg.Steer;
            if (Sync.IsServer)
            {
                Sync.Layer.SendMessageToAllButOne(ref msg, sender.SteamUserId);
            }
        }

        internal void ChangeSlider(MySyncMotorSuspension.SliderEnum slider, float v)
        {
            var msg = new UpdateSliderMsg();
            msg.EntityId = Entity.EntityId;
            msg.Slider = slider;
            msg.Value = v;

            Sync.Layer.SendMessageToServerAndSelf(ref msg);
        }

        static void OnUpdateSlider(MySyncMotorSuspension sync, ref UpdateSliderMsg msg, MyNetworkClient sender)
        {
            switch (msg.Slider)
            {
                case SliderEnum.Height:
                    sync.Entity.Height = msg.Value;
                    break;
                case SliderEnum.MaxSteerAngle:
                    sync.Entity.MaxSteerAngle = msg.Value;
                    break;
                case SliderEnum.SteerSpeed:
                    sync.Entity.SteerSpeed = msg.Value;
                    break;
                case SliderEnum.SteerReturnSpeed:
                    sync.Entity.SteerReturnSpeed = msg.Value;
                    break;
                case SliderEnum.InvertSteer:
                    sync.Entity.InvertSteer = msg.Value > 0;
                    break;
                case SliderEnum.SuspensionTravel:
                    sync.Entity.SuspensionTravel = msg.Value;
                    break;
            }

            if (Sync.IsServer)
            {
                Sync.Layer.SendMessageToAllButOne(ref msg, sender.SteamUserId);
            }
        }
    }
}
