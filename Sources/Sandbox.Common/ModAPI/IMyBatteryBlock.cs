using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sandbox.ModAPI
{
    public interface IMyBatteryBlock : Sandbox.ModAPI.Ingame.IMyBatteryBlock
    {
        float CurrentStoredPower { get; set; }
  
    }
}
