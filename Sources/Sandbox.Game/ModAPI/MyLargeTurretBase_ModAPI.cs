using Sandbox.Engine.Multiplayer;
using Sandbox.Engine.Utils;
using Sandbox.Game.Entities;
using Sandbox.Game.Multiplayer;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.Game.Entity;
using VRage.ModAPI;
using VRage.Network;
using VRage.Utils;
using VRageMath;

namespace Sandbox.Game.Weapons
{
    public partial class MyLargeTurretBase : IMyLargeTurretBase
    {
        void Sandbox.ModAPI.Ingame.IMyLargeTurretBase.TrackTarget(IMyEntity entity)
        {
            if (entity != null)
            {
                MyMultiplayer.RaiseEvent(this, x => x.SetTargetRequest, entity.EntityId, true);
            }          
        }

        [Event,Reliable,Server,Broadcast]
        void SetTargetRequest(long entityId, bool usePrediction)
        {
            MyEntity target = null;
            if (entityId != 0)
            {
                MyEntities.TryGetEntityById(entityId, out target);
            }

            ForceTarget(target, usePrediction);
        }

        void Sandbox.ModAPI.Ingame.IMyLargeTurretBase.TrackTarget(Vector3D pos, Vector3 velocity)
        {
            MyMultiplayer.RaiseEvent(this, x => x.SetTargetPosition, pos,velocity, true);
        }

        void Sandbox.ModAPI.Ingame.IMyLargeTurretBase.SetTarget(Vector3D pos)
        {
            MyMultiplayer.RaiseEvent(this, x => x.SetTargetPosition, pos, Vector3.Zero, false);      
        }

        [Event, Reliable, Server, Broadcast]
        void SetTargetPosition(Vector3D targetPos, Vector3 velocity,bool usePrediction)
        {
            TargetPosition(targetPos, velocity, usePrediction);
        }

        void Sandbox.ModAPI.Ingame.IMyLargeTurretBase.SetTarget(IMyEntity entity)
        {
            if (entity != null)
            {
                MyMultiplayer.RaiseEvent(this, x => x.SetTargetRequest, entity.EntityId, false);
            } 
        }

        float Sandbox.ModAPI.Ingame.IMyLargeTurretBase.Elevation 
        {
            get
            {
                return m_elevation;
            }
            set
            {
                SetManualElevation(value);
            }
        }

        float Sandbox.ModAPI.Ingame.IMyLargeTurretBase.Azimuth
        {
            get
            {
                return m_rotation;
            }
            set
            {
                SetManualAzimuth(value);
            }
        }

        bool Sandbox.ModAPI.Ingame.IMyLargeTurretBase.EnableIdleRotation 
        { 
            get
            {
                return EnableIdleRotation;
            }
            set
            {
                EnableIdleRotation = value;
            }
        }

        bool Sandbox.ModAPI.Ingame.IMyLargeTurretBase.AIEnabled 
        {
            get
            {
                return AiEnabled;
            }
 
        }

        bool Sandbox.ModAPI.Ingame.IMyLargeTurretBase.IsUnderControl
        {
            get { return ControllerInfo.Controller != null; }
        }
        bool Sandbox.ModAPI.Ingame.IMyLargeTurretBase.CanControl
        {
            get { return CanControl(); }
        }

        float Sandbox.ModAPI.Ingame.IMyLargeTurretBase.Range
        {
            get { return ShootingRange; }
        }

        void Sandbox.ModAPI.Ingame.IMyLargeTurretBase.ResetTargetingToDefault()
        {
            MyMultiplayer.RaiseEvent(this, x => x.ResetTargetParams);
        }

        void Sandbox.ModAPI.Ingame.IMyLargeTurretBase.SyncEnableIdleRotation()
        {
        }

        void Sandbox.ModAPI.Ingame.IMyLargeTurretBase.SyncAzimuth()
        {
            SyncRotationAndOrientation();
        }

        void Sandbox.ModAPI.Ingame.IMyLargeTurretBase.SyncElevation()
        {
            SyncRotationAndOrientation();
        }

        public MyEntityCameraSettings GetCameraEntitySettings()
        {
            return null;
        }

        public MyStringId ControlContext
        {
            get { return MySpaceBindingCreator.CX_SPACESHIP; }
        }
    }
}
