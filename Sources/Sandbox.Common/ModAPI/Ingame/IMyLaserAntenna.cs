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
        /// get target coordinates (r/o)
        /// </summary>
        Vector3D TargetCoords { get; }

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
        /// Connection is permanent (r/o)
        /// </summary>
        bool IsPermanent { get; }

        /// <summary>
        /// Returns antenna block on other end (r/o) - requires LoS and ownership of other end
        /// </summary>
        IMyLaserAntenna OtherAntenna { get; }
    }
}
