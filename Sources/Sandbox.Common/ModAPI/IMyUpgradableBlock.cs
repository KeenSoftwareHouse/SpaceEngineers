using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.Game.ModAPI;

namespace Sandbox.ModAPI
{
    /// <summary>
    /// interface to retrieve upgrade effects from block <see cref="Ingame.IMyUpgradableBlock"/>
    /// </summary>
    public interface IMyUpgradableBlock : IMyCubeBlock, Ingame.IMyUpgradableBlock
    {
    }
}
