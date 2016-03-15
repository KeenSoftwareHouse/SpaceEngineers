using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sandbox.ModAPI.Ingame
{
    public interface IMyShipController : IMyTerminalBlock
    {
        /// <summary>
        /// Indicates whether a block is locally or remotely controlled.
        /// </summary>
        bool IsUnderControl { get; }
        /// <summary>
        /// Indicates whether the block is controlling wheels or not. True if it is controlling wheels.
        /// </summary>
        bool ControlWheels { get; }
        /// <summary>
        /// Indicates whether the block is controlling thruster or not. True if it is controlling thruster.
        /// </summary>
        bool ControlThrusters { get; }
        /// <summary>
        /// Indicates whether the wheel handbrake is currently enabled or not. True if it is enabled.
        /// </summary>
        bool HandBrake { get; }
        /// <summary>
        /// Indicates whether the thruster dampening is currently enabled or not. True if it is enabled.
        /// </summary>
        bool DampenersOverride { get; }
        /// <summary>
        /// Returns the current translation input for that block. +/- X = right/right, +/- Y = up/down, +/- Z = backward/forward. 
        /// </summary>
        VRageMath.Vector3 Translation { get; }
        /// <summary>
        /// Returns the current rotation input for that block. X = pitch, Y = yaw, Z = roll.
        /// </summary>
        VRageMath.Vector3 Rotation { get; }
    }
}