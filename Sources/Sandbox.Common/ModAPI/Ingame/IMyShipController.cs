using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using VRageMath;

namespace Sandbox.ModAPI.Ingame
{
    public struct MyShipMass
    {
        /// <summary>
        /// Gets the base mass of the ship.
        /// </summary>
        public readonly int BaseMass;

        /// <summary>
        /// Gets the total mass of the ship, including cargo.
        /// </summary>
        public readonly int TotalMass;
        
        public MyShipMass(int mass, int totalMass) : this()
        {
            BaseMass = mass;
            TotalMass = totalMass;
        }
    }

    /// <summary>
    /// Describes what detail level to retrieve the planet elevation for.
    /// </summary>
    public enum MyPlanetElevation
    {
        /// <summary>
        /// Only return the distance to the planetary sealevel.
        /// </summary>
        Sealevel,

        /// <summary>
        /// Return the distance to the closest point of the planet. This is the same value
        /// displayed in the HUD.
        /// </summary>
        Surface
    }

    public struct MyShipVelocities
    {
        /// <summary>
        /// Gets the ship's linear velocity (motion).
        /// </summary>
        public readonly Vector3D LinearVelocity;
        
        /// <summary>
        /// Gets the ship's angular velocity (rotation).
        /// </summary>
        public readonly Vector3D AngularVelocity;

        public MyShipVelocities(Vector3D linearVelocity, Vector3D angularVelocity) : this()
        {
            LinearVelocity = linearVelocity;
            AngularVelocity = angularVelocity;
        }
    }

    public interface IMyShipController : IMyTerminalBlock
    {
        /// <summary>
        /// Indicates whether a block is locally or remotely controlled.
        /// </summary>
        bool IsUnderControl { get; }

        /// <summary>
        /// Indicates whether wheels are being controlled by this controller.
        /// </summary>
        bool ControlWheels { get; }

        /// <summary>
        /// Indicates whether thrusters are being controlled by this controller.
        /// </summary>
        bool ControlThrusters { get; }

        /// <summary>
        /// Indicates the current state of the handbrake.
        /// </summary>
        bool HandBrake { get; }

        /// <summary>
        /// Indicates whether dampeners are currently enabled.
        /// </summary>
        bool DampenersOverride { get; }

        /// <summary>
        /// Gets the detected natural gravity vector and power at the current location.
        /// </summary>
        /// <returns></returns>
        Vector3D GetNaturalGravity();

        /// <summary>
        /// Gets the detected artificial gravity vector and power at the current location.
        /// </summary>
        /// <returns></returns>
        Vector3D GetArtificialGravity();

        /// <summary>
        /// Gets the total accumulated gravity vector and power at the current location, 
        /// taking both natural and artificial gravity into account.
        /// </summary>
        /// <returns></returns>
        Vector3D GetTotalGravity();

        /// <summary>
        /// Gets the basic ship speed in meters per second, for when you just need to know how fast you're going.
        /// </summary>
        /// <returns></returns>
        double GetShipSpeed();

        /// <summary>
        /// Determines the linear velocities in meters per second and angular velocities in radians per second. 
        /// Provides a more accurate representation of the directions and axis speeds.
        /// </summary>
        MyShipVelocities GetShipVelocities();

        /// <summary>
        /// Gets information about the current mass of the ship.
        /// </summary>
        /// <returns></returns>
        MyShipMass CalculateShipMass();

        /// <summary>
        /// Attempts to get the world position of the nearest planet. This method is only available when a ship is 
        /// within the gravity well of a planet.
        /// </summary>
        /// <param name="position"></param>
        /// <returns></returns>
        bool TryGetPlanetPosition(out Vector3D position);

        /// <summary>
        /// Attempts to get the elevation of the ship in relation to the nearest planet. This method is only available
        /// when a ship is within the gravity well of a planet.
        /// </summary>
        /// <param name="detail"></param>
        /// <param name="elevation"></param>
        /// <returns></returns>
        bool TryGetPlanetElevation(MyPlanetElevation detail, out double elevation);

        /// <summary>
        /// Directional input from user/autopilot. Values can be very large with high controller sensitivity
        /// </summary>
        Vector3 MoveIndicator { get; }

        /// <summary>
        /// Pitch, yaw input from user/autopilot. Values can be very large with high controller sensitivity
        /// </summary>
        Vector2 RotationIndicator { get; }

        /// <summary>
        /// Roll input from user/autopilot. Values can be very large with high controller sensitivity
        /// </summary>
        float RollIndicator { get; }
    }
}
