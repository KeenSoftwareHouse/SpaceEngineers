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
    }
}
