using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sandbox.ModAPI
{
    /// <summary>
    /// Oxygen generator interface
    /// </summary>
    public interface IMyOxygenGenerator : Ingame.IMyOxygenGenerator, IMyFunctionalBlock
    {
        /// <summary>
        /// Increase/decrese O2 produced
        /// </summary>
        float ProductionCapacityMultiplier { get; set; }
        /// <summary>
        /// Increase/decrese power consumption
        /// </summary>
        float PowerConsumptionMultiplier { get; set; }
    }
}
