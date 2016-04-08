using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sandbox.ModAPI
{
    public interface IMyBatteryBlock : Ingame.IMyBatteryBlock
    {
        new float CurrentStoredPower { get; set; }
  
    }
}
