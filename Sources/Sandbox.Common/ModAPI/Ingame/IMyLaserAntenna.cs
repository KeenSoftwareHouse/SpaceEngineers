using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRageMath;

namespace Sandbox.ModAPI.Ingame
{
    /// <summary>
    /// Laser antenna block interface
    /// </summary>
    public interface IMyLaserAntenna : IMyFunctionalBlock
    {
        /// <summary>
        /// get target coordinates
        /// </summary>
        Vector3D TargetCoords
        {
            get;
        }

        /// <summary>
        /// Set coordinates of target
        /// </summary>
        /// <param name="coords">GPS coordinates string</param>
        void SetTargetCoords(string coords);

        /// <summary>
        /// Connect to target defined by SetTargetCoords
        /// </summary>
        void Connect();

        /// <summary>
        /// Connection is permanent
        /// </summary>
        bool IsPermanent { get; }

        /// <summary>
        /// Target is outside movement limits of antenna
        /// </summary>
        bool IsOutsideLimits { get; }
    }
}
