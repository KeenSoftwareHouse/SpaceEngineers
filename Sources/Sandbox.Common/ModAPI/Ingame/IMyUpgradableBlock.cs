using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.Game.ModAPI.Ingame;

namespace Sandbox.ModAPI.Ingame
{
    /// <summary>
    /// interface to retrieve upgrade effects on block
    /// </summary>
    public interface IMyUpgradableBlock : IMyCubeBlock
    {
        /// <summary>
        /// get list of upgrades (r/o);
        /// string - upgrade type, float - effect value as float (1 = 100%)
        /// </summary>
        void GetUpgrades(out Dictionary<string, float> upgrades);

        /// <summary>
        /// number of upgrades applied
        /// </summary>
        uint UpgradeCount { get; }
    }
}
