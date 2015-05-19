using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sandbox.ModAPI
{
    public interface IMyOxygenGenerator : Sandbox.ModAPI.Ingame.IMyOxygenGenerator, Sandbox.ModAPI.IMyFunctionalBlock
    {
        float ProductionCapacityMultiplier { get; set; }
        float PowerConsumptionMultiplier { get; set; }
    }
}
