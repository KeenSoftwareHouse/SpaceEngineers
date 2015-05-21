using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sandbox.Game.GameSystems.Electricity
{
    delegate void ProductionStateChangeDelegate(IMyPowerProducer stoppedProducer);

    interface IMyPowerProducer : Sandbox.ModAPI.Ingame.IMyPowerProducer
    {
        MyProducerGroupEnum Group { get; }

        /// <summary>
        /// Currently used power output of the producer in [MW].
        /// </summary>
        new float CurrentPowerOutput { get; set; }

        bool HasCapacityRemaining { get; }

        event Action<IMyPowerProducer> HasCapacityRemainingChanged;

        bool Enabled { get; set; }

        event Action<IMyPowerProducer> MaxPowerOutputChanged;

        /// <summary>
        /// Remaining capacity in MWh.
        /// </summary>
        float RemainingCapacity { get; }

        bool HasPlayerAccess(long playerId);
    }

    static class MyPowerProducerExtensions
    {
        public static bool IsInfiniteCapacity(this IMyPowerProducer self)
        {
            return float.IsInfinity(self.RemainingCapacity);
        }
    }

}
