using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sandbox.ModAPI
{
    public interface IMyFunctionalBlock : Sandbox.ModAPI.Ingame.IMyFunctionalBlock
    {
        event Action<IMyTerminalBlock> EnabledChanged;
    }
}
