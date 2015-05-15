using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sandbox.ModAPI.Ingame
{
    public interface IMySolarPanel : IMyTerminalBlock
    {
        float CurrentPowerOutput { get; }
        float MaxPowerOutput { get; }
    }
}
