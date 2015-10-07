using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRageMath;

namespace Sandbox.ModAPI.Ingame
{
    public interface IMyRemoteControl : IMyShipController
    {
        // Gets the nearest player's position. Will only work if the remote control belongs to an NPC
        bool GetNearestPlayer(out Vector3D playerPosition);
        Vector3D GetNaturalGravity();

        void ClearWaypoints();
        void AddWaypoint(Vector3D point, string name);
        
        /// <summary>
        /// Requests a jump, can only be done my NPC factions.
        /// </summary>
        /// <param name="jumpDrive">Jumpdrive to use.</param>
        /// <returns>If the jump request was successful.</returns>
        bool RequestJump(IMyJumpDrive jumpDrive);
        
        void SetAutoPilotEnabled(bool enabled);

        // CH: TODO: Uncomment later for drones
        Vector3D GetFreeDestination(Vector3D originalDestination, float checkRadius, float shipRadius);
    }
}
