using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sandbox.ModAPI.Ingame
{
    /// <summary>
    /// Refinery block interface
    /// </summary>
    public interface IMyRefinery : IMyProductionBlock
    {
        /// <summary>
        /// Ore conversion Effectiveness - in decimals (0.5=50%)
        /// </summary>
        float Effectiveness { get; }
        /// <summary>
        /// Production speed - in decimals (0.5=50%)
        /// </summary>
        float Productivity { get; }
        /// <summary>
        /// Power efficiency - in decimals (0.5=50%)
        /// </summary>
        float PowerEfficiency { get; }
    }
}
