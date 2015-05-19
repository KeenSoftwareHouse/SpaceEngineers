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
        /// Runs the script of a specified programmable block connected to the current grid via laser antenna with a custom argument.
        /// </summary>
        /// <param name="pb_name">The name of the target programmable block for this message.</param>
        /// <param name="message">The custom argument string.</param>
        /// <returns>Returns true if the pb could be reached. False otherwise.</returns>
        bool SendMessage(string pb_name, string message);

        /// <summary>
        /// Connection is permanent
        /// </summary>
        bool IsPermanent { get; }
    }
}
