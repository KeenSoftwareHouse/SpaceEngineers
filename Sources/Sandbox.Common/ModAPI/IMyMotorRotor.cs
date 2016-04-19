using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.Game.ModAPI;

namespace Sandbox.ModAPI
{
    public interface IMyMotorRotor : IMyCubeBlock, Ingame.IMyMotorRotor
    {
        /// <summary>
        /// Gets the attached stator/suspension block
        /// </summary>
        IMyMotorBase Stator { get; }
    }
}
