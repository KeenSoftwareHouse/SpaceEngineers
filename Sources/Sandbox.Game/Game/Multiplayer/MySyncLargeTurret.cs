using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Sandbox.Engine.Multiplayer;
using Sandbox.Game.Entities;
using Sandbox.Game.Weapons;
using Sandbox.Game.World;
using SteamSDK;
using VRageMath;

namespace Sandbox.Game.Multiplayer
{
    [PreloadRequired]
    class MySyncLargeTurret : MySyncControllableEntity
    {
        private MyLargeTurretBase m_turret;

        [MessageId(686, P2PMessageEnum.Reliable)]
        protected struct ChangeTargetMsg : IEntityMessage
        {
            public long EntityId;

            public long GetEntityId() { return EntityId; }

            public long Target;
            public BoolBlit IsPotential;

            public override string ToString()
            {
                return String.Format("{0}, {1}", this.GetType().Name, this.GetEntityText());
            }
        }

        [MessageId(687, P2PMessageEnum.Reliable)]
        protected struct ChangeRangeMsg : IEntityMessage
        {
            public long EntityId;

            public long GetEntityId() { return EntityId; }

            public float Range;

            public override string ToString()
            {
                return String.Format("{0}, {1}", this.GetType().Name, this.GetEntityText());
            }
        }

        [MessageId(688, P2PMessageEnum.Reliable)]
        protected struct ChangeTargetingMsg : IEntityMessage
        {
            public long EntityId;

            public long GetEntityId() { return EntityId; }

            public MyTurretTargetFlags TargetFlags;

            public override string ToString()
            {
                return String.Format("{0}, {1}", this.GetType().Name, this.GetEntityText());
            }
        }

        [MessageId(689, P2PMessageEnum.Unreliable)]
        protected struct UpdateRotationAndElevationMsg : IEntityMessage
        {
            public long EntityId;

            public long GetEntityId() { return EntityId; }

            public float Rotation;
            public float Elevation;

            public override string ToString()
            {
                return String.Format("{0}, {1}", this.GetType().Name, this.GetEntityText());
            }
        }


        [MessageId(690, P2PMessageEnum.Reliable)]
        protected struct SetTargetMsg : IEntityMessage
        {
            public long EntityId;

            public long GetEntityId() { return EntityId; }

            public long Target;
            public BoolBlit UsePrediction;

            public override string ToString()
            {
                return String.Format("{0}, {1}", this.GetType().Name, this.GetEntityText());
            }
        }

        [MessageId(691, P2PMessageEnum.Reliable)]
        protected struct TargetPositionMsg : IEntityMessage
        {
            public long EntityId;

            public long GetEntityId() { return EntityId; }

            public Vector3D TargetPos;
            public Vector3 TargetVelocity;
            public BoolBlit UsePrediction;

            public override string ToString()
            {
                return String.Format("{0}, {1}", this.GetType().Name, this.GetEntityText());
            }
        }

        [MessageId(692, P2PMessageEnum.Reliable)]
        protected struct ChangeIdleRotationMsg : IEntityMessage
        {
            public long EntityId;

            public long GetEntityId() { return EntityId; }

            public BoolBlit Enable;

            public override string ToString()
            {
                return String.Format("{0}, {1}", this.GetType().Name, this.GetEntityText());
            }
        }

        [MessageId(693, P2PMessageEnum.Reliable)]
        protected struct ResetParamsMsg : IEntityMessage
        {
            public long EntityId;

            public long GetEntityId() { return EntityId; }


            public override string ToString()
            {
                return String.Format("{0}, {1}", this.GetType().Name, this.GetEntityText());
            }
        }

        [MessageId(694, P2PMessageEnum.Reliable)]
        protected struct SetManualAzimuthMsg : IEntityMessage
        {
            public long EntityId;

            public long GetEntityId() { return EntityId; }

            public float Azimuth;

            public override string ToString()
            {
                return String.Format("{0}, {1}", this.GetType().Name, this.GetEntityText());
            }
        }

        [MessageId(695, P2PMessageEnum.Reliable)]
        protected struct SetManualElevationMsg : IEntityMessage
        {
            public long EntityId;

            public long GetEntityId() { return EntityId; }

            public float Elevation;

            public override string ToString()
            {
                return String.Format("{0}, {1}", this.GetType().Name, this.GetEntityText());
            }
        }
        static MySyncLargeTurret()
        {
            MySyncLayer.RegisterMessage<ChangeTargetMsg>(OnChangeTarget, MyMessagePermissions.FromServer, MyTransportMessageEnum.Success);

            MySyncLayer.RegisterMessage<ChangeRangeMsg>(ChangeRangeRequest, MyMessagePermissions.ToServer, MyTransportMessageEnum.Request);
            MySyncLayer.RegisterMessage<ChangeRangeMsg>(ChangeRangeSuccess, MyMessagePermissions.FromServer, MyTransportMessageEnum.Success);

            MySyncLayer.RegisterMessage<ChangeTargetingMsg>(ChangeTargetingRequest, MyMessagePermissions.ToServer, MyTransportMessageEnum.Request);
            MySyncLayer.RegisterMessage<ChangeTargetingMsg>(ChangeTargetingSuccess, MyMessagePermissions.FromServer, MyTransportMessageEnum.Success);

            MySyncLayer.RegisterMessage<UpdateRotationAndElevationMsg>(OnRotationAndElevationReceived, MyMessagePermissions.ToServer | MyMessagePermissions.FromServer);

            MySyncLayer.RegisterMessage<SetTargetMsg>(SetTargetRequest, MyMessagePermissions.ToServer, MyTransportMessageEnum.Request);
            MySyncLayer.RegisterMessage<SetTargetMsg>(SetTargetSuccess, MyMessagePermissions.FromServer, MyTransportMessageEnum.Success);

            MySyncLayer.RegisterMessage<TargetPositionMsg>(TargetPositionRequest, MyMessagePermissions.ToServer, MyTransportMessageEnum.Request);
            MySyncLayer.RegisterMessage<TargetPositionMsg>(TargetPositionSuccess, MyMessagePermissions.FromServer, MyTransportMessageEnum.Success);

            MySyncLayer.RegisterMessage<ChangeIdleRotationMsg>(ChangeIdleRotationRequest, MyMessagePermissions.ToServer, MyTransportMessageEnum.Request);
            MySyncLayer.RegisterMessage<ChangeIdleRotationMsg>(ChangeIdleRotationSuccess, MyMessagePermissions.FromServer, MyTransportMessageEnum.Success);

            MySyncLayer.RegisterMessage<ResetParamsMsg>(ResetTargetParamsRequest, MyMessagePermissions.ToServer, MyTransportMessageEnum.Request);
            MySyncLayer.RegisterMessage<ResetParamsMsg>(ResetTargetParamsSuccess, MyMessagePermissions.FromServer, MyTransportMessageEnum.Success);

            MySyncLayer.RegisterMessage<SetManualElevationMsg>(SetManualElevationRequest, MyMessagePermissions.ToServer, MyTransportMessageEnum.Request);
            MySyncLayer.RegisterMessage<SetManualElevationMsg>(SetManualElevationSuccess, MyMessagePermissions.FromServer, MyTransportMessageEnum.Success);

            MySyncLayer.RegisterMessage<SetManualAzimuthMsg>(SetManualAzimuthRequest, MyMessagePermissions.ToServer, MyTransportMessageEnum.Request);
            MySyncLayer.RegisterMessage<SetManualAzimuthMsg>(SetManualAzimuthSuccess, MyMessagePermissions.FromServer, MyTransportMessageEnum.Success);
        }
     
        public MySyncLargeTurret(MyLargeTurretBase turret)  : base(turret)
        {
            m_turret = turret;
        }

        public void SendChangeTarget(long target, bool isPotentialTarget)
        {
            var msg = new ChangeTargetMsg();

            msg.EntityId = m_turret.EntityId;
            msg.Target = target;
            msg.IsPotential = isPotentialTarget;
  
            Sync.Layer.SendMessageToAll(ref msg, MyTransportMessageEnum.Success);
        }


        static void OnChangeTarget(ref ChangeTargetMsg msg, MyNetworkClient sender)
        {
            MyEntity entity;
            MyEntities.TryGetEntityById(msg.EntityId, out entity);
            var turret = entity as MyLargeTurretBase;
            if (turret != null)
            {
                MyEntity target = null;
                if (msg.Target != 0)
                    MyEntities.TryGetEntityById(msg.Target, out target);
                
                turret.SetTarget(target, msg.IsPotential);                
            }
        }

        public void SendChangeRangeRequest(float range)
        {
            var msg = new ChangeRangeMsg();

            msg.EntityId = m_turret.EntityId;
            msg.Range = range;

            Sync.Layer.SendMessageToServer(ref msg, MyTransportMessageEnum.Request);
        }

        static void ChangeRangeRequest(ref ChangeRangeMsg msg, MyNetworkClient sender)
        {
            MyEntity entity;
            MyEntities.TryGetEntityById(msg.EntityId, out entity);
            if (entity is MyLargeTurretBase)
            {
                Sync.Layer.SendMessageToAllAndSelf(ref msg, MyTransportMessageEnum.Success);
            }
        }

        static void ChangeRangeSuccess(ref ChangeRangeMsg msg, MyNetworkClient sender)
        {
            MyEntity entity;
            MyEntities.TryGetEntityById(msg.EntityId, out entity);
            var turret = entity as MyLargeTurretBase;
            if (turret != null)
            {
                turret.ShootingRange = msg.Range;
            }
        }

        public void SendChangeTargetingRequest(MyTurretTargetFlags flags)
        {
            var msg = new ChangeTargetingMsg();

            msg.EntityId = m_turret.EntityId;
            msg.TargetFlags = flags;

            Sync.Layer.SendMessageToServer(ref msg, MyTransportMessageEnum.Request);
        }

        static void ChangeTargetingRequest(ref ChangeTargetingMsg msg, MyNetworkClient sender)
        {
            MyEntity entity;
            MyEntities.TryGetEntityById(msg.EntityId, out entity);
            if (entity is MyLargeTurretBase)
            {
                Sync.Layer.SendMessageToAllAndSelf(ref msg, MyTransportMessageEnum.Success);
            }
        }

        static void ChangeTargetingSuccess(ref ChangeTargetingMsg msg, MyNetworkClient sender)
        {
            MyEntity entity;
            MyEntities.TryGetEntityById(msg.EntityId, out entity);
            var turret = entity as MyLargeTurretBase;
            if (turret != null)
            {
                turret.TargetFlags = msg.TargetFlags;
            }
        }

        public void SendRotationAndElevation(float rotation, float elevation)
        {
            var msg = new UpdateRotationAndElevationMsg();
            msg.EntityId = Entity.Entity.EntityId;
            msg.Rotation = rotation;
            msg.Elevation = elevation;

            Sync.Layer.SendMessageToServer(ref msg);
        }

        private static void OnRotationAndElevationReceived(ref UpdateRotationAndElevationMsg msg, MyNetworkClient sender)
        {
            MyEntity entity;
            MyEntities.TryGetEntityById(msg.EntityId, out entity);
            var turret = entity as MyLargeTurretBase;
            if (turret != null)
            {
                turret.UpdateRotationAndElevation(msg.Rotation, msg.Elevation);
                if (Sync.IsServer)
                {
                    Sync.Layer.SendMessageToAllButOne(ref msg, sender.SteamUserId);
                }
            }
        }

        public void SendSetTarget(long target, bool usePrediction)
        {
            var msg = new SetTargetMsg();

            msg.EntityId = m_turret.EntityId;
            msg.Target = target;
            msg.UsePrediction = usePrediction;

            Sync.Layer.SendMessageToServer(ref msg, MyTransportMessageEnum.Request);
        }

        static void SetTargetRequest(ref SetTargetMsg msg,MyNetworkClient sender)
        {
            MyEntity entity;
            MyEntities.TryGetEntityById(msg.EntityId, out entity);
            if (entity is MyLargeTurretBase)
            {
                Sync.Layer.SendMessageToAllAndSelf(ref msg, MyTransportMessageEnum.Success);
            }
        }

        static void SetTargetSuccess(ref SetTargetMsg msg,MyNetworkClient sender)
        {
            MyEntity entity;
            MyEntities.TryGetEntityById(msg.EntityId, out entity);
            var turret = entity as MyLargeTurretBase;
            if (turret != null)
            {
                MyEntity target = null;
                if (msg.Target != 0)
                    MyEntities.TryGetEntityById(msg.Target, out target);
                
                turret.ForceTarget(target, msg.UsePrediction);                
            }
        }

        public void SendTargetPosition(Vector3D targetPos,Vector3? targetVelocity = null)
        {
            var msg = new TargetPositionMsg();

            msg.EntityId = m_turret.EntityId;
            msg.TargetPos = targetPos;
            if (targetVelocity.HasValue)
            {
                msg.TargetVelocity = targetVelocity.Value;
                msg.UsePrediction = true;
            }
            else
            {
                msg.UsePrediction = false;
            }

            Sync.Layer.SendMessageToServer(ref msg, MyTransportMessageEnum.Request);
        }

        static void TargetPositionRequest(ref TargetPositionMsg msg, MyNetworkClient sender)
        {
            MyEntity entity;
            MyEntities.TryGetEntityById(msg.EntityId, out entity);
            if (entity is MyLargeTurretBase)
            {
                Sync.Layer.SendMessageToAllAndSelf(ref msg, MyTransportMessageEnum.Success);
            }
        }

        static void TargetPositionSuccess(ref TargetPositionMsg msg, MyNetworkClient sender)
        {
            MyEntity entity;
            MyEntities.TryGetEntityById(msg.EntityId, out entity);
            var turret = entity as MyLargeTurretBase;
            if (turret != null)
            {            
                turret.TargetPosition(msg.TargetPos,msg.TargetVelocity,msg.UsePrediction);
            }
        }

        public void SendIdleRotationChanged(bool enable)
        {
            var msg = new ChangeIdleRotationMsg();

            msg.EntityId = m_turret.EntityId;
            msg.Enable = enable;
          
            Sync.Layer.SendMessageToServer(ref msg, MyTransportMessageEnum.Request);
        }

        static void ChangeIdleRotationRequest(ref ChangeIdleRotationMsg msg, MyNetworkClient sender)
        {
            MyEntity entity;
            MyEntities.TryGetEntityById(msg.EntityId, out entity);
            if (entity is MyLargeTurretBase)
            {
                Sync.Layer.SendMessageToAllAndSelf(ref msg, MyTransportMessageEnum.Success);
            }
        }

        static void ChangeIdleRotationSuccess(ref ChangeIdleRotationMsg msg, MyNetworkClient sender)
        {
            MyEntity entity;
            MyEntities.TryGetEntityById(msg.EntityId, out entity);
            var turret = entity as MyLargeTurretBase;
            if (turret != null)
            {
                turret.ChangeIdleRotation(msg.Enable);
            }
        }


        public void SendResetTargetParams()
        {
            var msg = new ResetParamsMsg();

            msg.EntityId = m_turret.EntityId;

            Sync.Layer.SendMessageToServer(ref msg, MyTransportMessageEnum.Request);
        }

        static void ResetTargetParamsRequest(ref ResetParamsMsg msg, MyNetworkClient sender)
        {
            MyEntity entity;
            MyEntities.TryGetEntityById(msg.EntityId, out entity);
            if (entity is MyLargeTurretBase)
            {
                Sync.Layer.SendMessageToAllAndSelf(ref msg, MyTransportMessageEnum.Success);
            }
        }

        static void ResetTargetParamsSuccess(ref ResetParamsMsg msg, MyNetworkClient sender)
        {
            MyEntity entity;
            MyEntities.TryGetEntityById(msg.EntityId, out entity);
            var turret = entity as MyLargeTurretBase;
            if (turret != null)
            {
                turret.ResetTargetParams();
            }
        }

        public void SendManualAzimutAngle(float azimuth)
        {
            var msg = new SetManualAzimuthMsg();

            msg.EntityId = m_turret.EntityId;
            msg.Azimuth = azimuth;

            Sync.Layer.SendMessageToServer(ref msg, MyTransportMessageEnum.Request);
        }

        static void SetManualAzimuthRequest(ref SetManualAzimuthMsg msg, MyNetworkClient sender)
        {
            MyEntity entity;
            MyEntities.TryGetEntityById(msg.EntityId, out entity);
            if (entity is MyLargeTurretBase)
            {
                Sync.Layer.SendMessageToAllAndSelf(ref msg, MyTransportMessageEnum.Success);
            }
        }

        static void SetManualAzimuthSuccess(ref SetManualAzimuthMsg msg, MyNetworkClient sender)
        {
            MyEntity entity;
            MyEntities.TryGetEntityById(msg.EntityId, out entity);
            var turret = entity as MyLargeTurretBase;
            if (turret != null)
            {
                turret.SetManualElevation(msg.Azimuth);
            }
        }

        public void SendSetManualElevationAngle(float elevation)
        {
            var msg = new SetManualElevationMsg();

            msg.EntityId = m_turret.EntityId;
            msg.Elevation = elevation;

            Sync.Layer.SendMessageToServer(ref msg, MyTransportMessageEnum.Request);
        }

        static void SetManualElevationRequest(ref SetManualElevationMsg msg, MyNetworkClient sender)
        {
            MyEntity entity;
            MyEntities.TryGetEntityById(msg.EntityId, out entity);
            if (entity is MyLargeTurretBase)
            {
                Sync.Layer.SendMessageToAllAndSelf(ref msg, MyTransportMessageEnum.Success);
            }
        }

        static void SetManualElevationSuccess(ref SetManualElevationMsg msg, MyNetworkClient sender)
        {
            MyEntity entity;
            MyEntities.TryGetEntityById(msg.EntityId, out entity);
            var turret = entity as MyLargeTurretBase;
            if (turret != null)
            {
                turret.SetManualElevation(msg.Elevation);
            }
        }

    }
}
