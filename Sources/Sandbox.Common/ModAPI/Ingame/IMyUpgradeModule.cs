using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sandbox.ModAPI.Ingame
{
    /// <summary>
    /// Interface to access module upgrades properties
    /// </summary>
    public interface IMyUpgradeModule
    {
        /// <summary>
        /// Retrieve list of upgrades from this block (r/o)
        /// </summary>
        List<IMyUpgradeInfo> UpgradeList { get; }
        /// <summary>
        /// Retrieve number of upgrade effects this block has (r/o)
        /// </summary>
        uint UpgradeCount { get; }
        /// <summary>
        /// Retrieve number of blocks this block is connected to (r/o)
        /// </summary>
        uint Connections { get; }
    }
}
