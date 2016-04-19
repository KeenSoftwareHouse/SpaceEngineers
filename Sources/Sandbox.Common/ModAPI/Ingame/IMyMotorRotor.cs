using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.Game.ModAPI.Ingame;

namespace Sandbox.ModAPI.Ingame
{
    public interface IMyMotorRotor : IMyCubeBlock
    {
        /// <summary>
        /// Gets whether the rotor is attached to a stator/suspension block
        /// </summary>
        bool IsAttached { get; }
    }
}
