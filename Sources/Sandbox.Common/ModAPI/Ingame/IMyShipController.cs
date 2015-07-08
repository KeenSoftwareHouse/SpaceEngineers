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
        bool ControlWheels { get; }
        bool ControlThrusters { get; }
        bool HandBrake { get; }
        bool DampenersOverride { get; }
        /// <summary>
        /// Returns the current translational/movement input from this controller relative to itself.
        /// </summary>
        VRageMath.Vector3 Translation { get; }
        /// <summary>
        /// Returns the current rotational input from this controller relative to itself while taking the roll dampener into account.
        /// </summary>
        VRageMath.Vector3 Rotation { get; }
    }
}
