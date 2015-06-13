using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRageMath;
using Sandbox.ModAPI;
using VRage.ModAPI;

namespace Sandbox.ModAPI.Ingame
{
    public interface IMyLargeTurretBase : IMyUserControllableGun
    {
        /// <summary>
        /// Indicates whether a block is locally or remotely controlled.
        /// </summary>
        bool IsUnderControl { get; }
        bool CanControl { get; }
        float Range { get; }

        /// <summary>
        /// Tracks entity with enabled position prediction
        /// </summary>
        /// <param name="entity"></param>
        void TrackTarget(IMyEntity entity);
        /// <summary>
        /// Tracks given target with enabled position prediction
        /// </summary>
        /// <param name="pos"></param>
        /// <param name="velocity"></param>
        void TrackTarget(Vector3D pos, Vector3 velocity);
        /// <summary>
        /// Tracks target without position prediction
        /// </summary>
        /// <param name="Entity"></param>
        void SetTarget(IMyEntity Entity);
        /// <summary>
        /// Targets given position
        /// </summary>
        /// <param name="pos"></param>
        void SetTarget(Vector3D pos);
        /// <summary>
        /// Sets/gets elevation of turret, this method is not synced, you need to sync elevation manually
        /// </summary>
        float Elevation { get; set; }
        /// <summary>
        /// method used to sync elevation of turret , you need to call it to sync elevation for other clients/server
        /// </summary>
        void SyncElevation();
        /// <summary>
        /// Sets/gets azimuth of turret, this method is not synced, you need to sync azimuth manually
        /// </summary>
        float Azimuth { get; set; }
        /// <summary>
        /// method used to sync azimuth, you need to call it to sync azimuth for other clients/server
        /// </summary>
        void SyncAzimuth();
        /// <summary>
        /// enable/disable idle rotation for turret, this method is not synced, you need to sync manually
        /// </summary>
        bool EnableIdleRotation { get; set; }
        /// <summary>
        /// method used to sync idle rotation and elevation, you need to call it to sync rotation and elevation for other clients/server
        /// </summary>
        void SyncEnableIdleRotation();
        /// <summary>
        /// Checks is AI is enabled for turret
        /// </summary>
        bool AIEnabled { get; }
        /// <summary>
        /// resert targeting to default values
        /// </summary>
        void ResetTargetingToDefault();
    }
}
