using Sandbox.ModAPI.Ingame;
using SpaceEngineers.ObjectBuilders.Definitions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sandbox.ModAPI.Ingame;

namespace SpaceEngineers.Game.ModAPI.Ingame
{
    /// <summary>
    /// Interface to access module upgrades properties
    /// </summary>
    public interface IMyUpgradeModule : IMyFunctionalBlock
    {
        /// <summary>
        /// Retrieve list of upgrades from this block (r/o), see <see cref='Sandbox.Common.ObjectBuilders.Definitions.MyUpgradeModuleInfo'>MyUpgradeModuleInfo</see> for details
        /// </summary>
        void GetUpgradeList(out List<MyUpgradeModuleInfo> upgrades);
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
