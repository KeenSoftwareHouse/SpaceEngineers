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
        /// get list of upgrades (r/o)
        /// </summary>
        void GetUpgrades(out Dictionary<string, float> upgrades);

        /// <summary>
        /// number of upgrades applied
        /// </summary>
        uint UpgradeCount { get; }
    }
}
