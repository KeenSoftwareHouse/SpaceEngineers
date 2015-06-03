using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sandbox.ModAPI.Ingame
{
    public interface IMyPowerProducer
    {
        /// <summary>
        /// Currently used power output of the producer in [MW].
        /// </summary>
        float CurrentPowerOutput { get; }

        /// <summary>
        /// Maximum power output of the producer in [MW].
        /// </summary>
        float MaxPowerOutput { get; }

        /// <summary>
        /// Max power output defined in definition [MW].
        /// </summary>
        float DefinedPowerOutput { get; }

        /// <summary>
        /// Power production is enabled
        /// </summary>
        bool ProductionEnabled { get; }
    }
}
