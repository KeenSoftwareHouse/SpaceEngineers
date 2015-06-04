using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Sandbox.Common;

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
        bool IsMain { get; }

        /// <summary>
        /// Sends a notification to the controller of this block
        /// </summary>
        /// <param name="msg">the message, max 500 characters</param>
        /// <param name="displayTimeMs">display time in miliseconds, max 30000</param>
        /// <param name="font">the font</param>
        void NotifyPilot(string message, int displayTimeMs = 2000, MyFontEnum font = MyFontEnum.White);
    }
}
