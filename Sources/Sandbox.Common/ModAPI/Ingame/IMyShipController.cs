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
    }
}
