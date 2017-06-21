using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SpaceEngineers.Game.ModAPI
{
    /// <summary>
    /// Interface to access upgrade module properties <see cref="Ingame.IMyUpgradeModule"/>
    /// </summary>
    public interface IMyUpgradeModule : IMyFunctionalBlock, Ingame.IMyUpgradeModule
    {
    }
}
