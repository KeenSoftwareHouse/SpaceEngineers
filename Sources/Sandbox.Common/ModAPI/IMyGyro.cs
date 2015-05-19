using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sandbox.ModAPI
{
    public interface IMyGyro : Sandbox.ModAPI.Ingame.IMyGyro, Sandbox.ModAPI.IMyFunctionalBlock
    {
        float GyroStrengthMultiplier { get; set; }
        float PowerConsumptionMultiplier { get; set; }
    }
}
