using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sandbox.ModAPI
{
    /// <summary>
    /// ModAPI laserantenna block interface
    /// </summary>
    public interface IMyLaserAntenna : IMyFunctionalBlock, Ingame.IMyLaserAntenna
    {
        /// <summary>
        /// Flag if antenna requires LoS - for modded antenas
        /// </summary>
        bool RequireLoS { get; }
    }
}
