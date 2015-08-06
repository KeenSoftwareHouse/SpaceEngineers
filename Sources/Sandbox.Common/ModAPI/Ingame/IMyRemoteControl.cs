using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRageMath;

namespace Sandbox.ModAPI.Ingame
{
    public interface IMyRemoteControl : IMyShipController
    {
        /// <summary>
        /// Adds a waypoint with given name and coords at the end of the waypoints list
        /// </summary>
        /// <param name="name"></param>
        /// <param name="coords"></param>
        void AddWaypoint(string name, Vector3D coords);
        
        /// <summary>
        /// Removes first waypoint with given name
        /// </summary>
        /// <param name="name"></param>
        void RemoveWaypoint(string name);

        /// <summary>
        /// Changes direction to fly in via code, alternatively use actions available on remote control to do this
        /// </summary>
        /// <param name="direction"></param>
        void ChangeDirection(Base6Directions.Direction direction);

        /// <summary>
        /// Change flight mode
        /// </summary>
        /// <param name="flightMode"></param>
        void ChangeFlightMode(FlightMode flightMode);

        /// <summary>
        /// Same function as "Reset" button in UE
        /// </summary>
        void ResetWaypoint();

        /// <summary>
        /// Called "Precision mode" in UI
        /// </summary>
        /// <param name="enabled"></param>
        void SetDockingMode(bool enabled);

        /// <summary>
        /// Starts or stops the auto pilot
        /// </summary>
        /// <param name="enabled"></param>
        void SetAutoPilotEnabled(bool enabled);

        /// <summary>
        /// Returns the names of all existing waypoints
        /// </summary>
        /// <returns></returns>
        string[] GetWaypointNames();

        /// <summary>
        /// Returns the coordinates for the waypoint with the given name. Returns Vector3D.Zero if name does not exist
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        Vector3D GetCoordinates(string name);
    }
    
    /// <summary>
    /// Flight mode. Moved here from MyRemoteControl.cs because it is required in the interface now
    /// </summary>
    public enum FlightMode : int
    {
        Patrol = 0,
        Circle = 1,
        OneWay = 2,
    }
}
