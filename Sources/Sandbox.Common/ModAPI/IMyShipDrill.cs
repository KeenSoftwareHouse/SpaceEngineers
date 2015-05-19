using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sandbox.ModAPI
{
    public interface IMyShipDrill : Sandbox.ModAPI.Ingame.IMyShipDrill, Sandbox.ModAPI.IMyFunctionalBlock
    {
        float DrillHarvestMultiplier { get; set; }
        float PowerConsumptionMultiplier { get; set; }
    }
}
