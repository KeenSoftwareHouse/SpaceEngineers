using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sandbox.ModAPI
{
    public interface IMyThrust : Sandbox.ModAPI.Ingame.IMyThrust, Sandbox.ModAPI.IMyFunctionalBlock
    {
        float ThrustMultiplier { get; set; }
        float PowerConsumptionMultiplier { get; set; }
    }
}
