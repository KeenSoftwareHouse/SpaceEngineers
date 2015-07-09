using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using VRageMath;

namespace Sandbox.ModAPI.Ingame
{
    public interface IMyJumpDrive : IMyFunctionalBlock
    {
        /// <summary>
        /// If the jump drive is currently charging.
        /// </summary>
        bool IsCharging { get; }

        /// <summary>
        /// Set the charging state of the jump drive.
        /// 
        /// NOTE: Sends network data.
        /// </summary>
        /// <param name="set"></param>
        void SetCharging(bool set);

        /// <summary>
        /// The estimated time until the jump drive is charged, in seconds.
        /// </summary>
        float TimeUntilCharged { get; }

        float CurrentStoredPower { get; }

        float MaxStoredPower { get; }

        /// <summary>
        /// If the jump drive is fully charged.
        /// </summary>
        bool IsFull { get; }

        /// <summary>
        /// The jump distance percent modifier.
        /// Value returned is from 0 to 100.
        /// </summary>
        float JumpDistancePercent { get; }

        /// <summary>
        /// Set the jump distance percent of the jump drive.
        /// 
        /// NOTE: Sends network data.
        /// </summary>
        /// <param name="percent"></param>
        void SetJumpDistancePercent(float percent);

        /// <summary>
        /// If the jump drive can jump.
        /// </summary>
        bool CanJump { get; }

        /// <summary>
        /// Starts the jump sequence towards the specified coordinates.
        /// 
        /// If the coordinates are too close or too far it will not do anything.
        /// </summary>
        /// <param name="coords"></param>
        void JumpTo(Vector3D coords);

        /// <summary>
        /// The name of the target GPS.
        /// </summary>
        string TargetName { get; }

        /// <summary>
        /// The coordinates of the target GPS set in this jump drive, or null.
        /// </summary>
        Vector3D? TargetCoords { get; }

        /// <summary>
        /// The minimum jump distance for any jump drive or grid.
        /// </summary>
        /// <returns></returns>
        double GetMinJumpDistance();

        /// <summary>
        /// The maximum jump distance for the entire grid.
        /// </summary>
        /// <returns></returns>
        double GetMaxJumpDistance();
    }
}
