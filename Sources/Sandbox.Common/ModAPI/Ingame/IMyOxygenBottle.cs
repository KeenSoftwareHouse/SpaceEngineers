using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sandbox.ModAPI.Ingame
{
    /// <summary>
    /// Interface for accessing oxygen bottle properties
    /// </summary>
    public interface IMyOxygenBottle : Interfaces.IMyInventoryItem
    {
        /// <summary>
        /// Current oxygen level
        /// </summary>
        float OxygenLevel { get; }
        /// <summary>
        /// Bottle capacity (in O2 units)
        /// </summary>
        float Capacity { get; }
    }
}
