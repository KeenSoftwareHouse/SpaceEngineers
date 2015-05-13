using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sandbox.ModAPI.Ingame
{
    public interface IMyFunctionalBlock : IMyTerminalBlock
    {
        bool Enabled { get; }
        void RequestEnable(bool enable);
        event Action<IMyTerminalBlock> EnabledChanged;
    }
}
