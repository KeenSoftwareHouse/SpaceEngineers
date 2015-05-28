using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sandbox.ModAPI.Ingame
{
    /// <summary>
    /// interface to retrieve upgrade effects from block
    /// </summary>
    public interface IMyUpgradableBlock
    {
        /// <summary>
        /// list of upgrades
        /// </summary>
        Dictionary<string, float> Upgrades { get; }

        /// <summary>
        /// number of upgrades applied
        /// </summary>
        uint UpgradeCount { get; }
    }
}
