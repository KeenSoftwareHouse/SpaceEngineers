using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.Game.ModAPI;

namespace Sandbox.ModAPI
{
    public interface IMyPistonTop : IMyCubeBlock, Ingame.IMyPistonTop
    {
        /// <summary>
        /// Gets the attached piston block
        /// </summary>
        IMyPistonBase Piston { get; }
    }
}
