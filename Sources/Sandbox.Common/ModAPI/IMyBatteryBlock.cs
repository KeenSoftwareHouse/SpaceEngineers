using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sandbox.ModAPI
{
    public interface IMyBatteryBlock : IMyFunctionalBlock, Ingame.IMyPowerProducer, Ingame.IMyBatteryBlock
    {
        void SetCurrentStoredPower(float newPower);
    }
}
