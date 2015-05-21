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
    }
}
