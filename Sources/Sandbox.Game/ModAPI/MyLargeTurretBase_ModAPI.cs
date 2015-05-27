using Sandbox.Engine.Utils;
using Sandbox.Game.Multiplayer;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.Utils;
using VRageMath;

namespace Sandbox.Game.Weapons
{
    partial class MyLargeTurretBase : Sandbox.ModAPI.Ingame.IMyLargeTurretBase
    {
        void Sandbox.ModAPI.Ingame.IMyLargeTurretBase.TrackTarget(IMyEntity entity)
        {
            if (entity != null)
            {
                SyncObject.SendSetTarget(entity.EntityId,true);
            }          
        }

        void Sandbox.ModAPI.Ingame.IMyLargeTurretBase.TrackTarget(Vector3D pos, Vector3 velocity)
        {
            SyncObject.SendTargetPosition(pos, velocity);
        }

        void Sandbox.ModAPI.Ingame.IMyLargeTurretBase.SetTarget(IMyEntity entity)
        {
            if (entity != null)
            {
                SyncObject.SendSetTarget(entity.EntityId, false);
            }
        }

        void Sandbox.ModAPI.Ingame.IMyLargeTurretBase.SetTarget(Vector3D pos)
        {
            SyncObject.SendTargetPosition(pos);       
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
            SyncObject.SendResetTargetParams();
        }

        void Sandbox.ModAPI.Ingame.IMyLargeTurretBase.SyncEnableIdleRotation()
        {
            SyncObject.SendIdleRotationChanged(m_enableIdleRotation);
        }

        void Sandbox.ModAPI.Ingame.IMyLargeTurretBase.SyncAzimuth()
        {
            SyncObject.SendManualAzimutAngle(m_rotation);
        }

        void Sandbox.ModAPI.Ingame.IMyLargeTurretBase.SyncElevation()
        {
            SyncObject.SendManualAzimutAngle(m_elevation);
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
