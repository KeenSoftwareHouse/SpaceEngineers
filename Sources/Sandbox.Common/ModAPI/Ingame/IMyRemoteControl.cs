using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRageMath;

namespace Sandbox.ModAPI.Ingame
{
    /// <summary>
    /// Provides basic information about a waypoint.
    /// </summary>
    public struct MyWaypointInfo
    {
        /// <summary>
        /// The waypoint name
        /// </summary>
        public readonly string Name;

        /// <summary>
        /// The coordinates of this waypoint
        /// </summary>
        public readonly Vector3D Coords;

        public MyWaypointInfo(string name, Vector3D coords)
        {
            Name = name;
            Coords = coords;
        }
    }

    public interface IMyRemoteControl : IMyShipController
    {
        // Gets the nearest player's position. Will only work if the remote control belongs to an NPC
        bool GetNearestPlayer(out Vector3D playerPosition);

        /// <summary>
        /// Removes all existing waypoints.
        /// </summary>
        void ClearWaypoints();

        /// <summary>
        /// Gets basic information about the currently configured waypoints.
        /// </summary>
        /// <param name="waypoints"></param>
        void GetWaypointInfo(List<MyWaypointInfo> waypoints);

        /// <summary>
        /// Adds a new waypoint.
        /// </summary>
        /// <param name="coords"></param>
        /// <param name="name"></param>
        void AddWaypoint(Vector3D coords, string name);

        /// <summary>
        /// Enables or disables the autopilot.
        /// </summary>
        /// <param name="enabled"></param>
        void SetAutoPilotEnabled(bool enabled);

        /// <summary>
        /// Determines whether the autopilot is currently enabled.
        /// </summary>
        bool IsAutoPilotEnabled { get; }
    }
}
