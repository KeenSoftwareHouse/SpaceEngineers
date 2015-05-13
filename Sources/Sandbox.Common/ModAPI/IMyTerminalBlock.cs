using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sandbox.ModAPI
{
    public interface IMyTerminalBlock:Sandbox.ModAPI.Ingame.IMyTerminalBlock
    {
        event Action<IMyTerminalBlock> CustomNameChanged;
        event Action<IMyTerminalBlock> OwnershipChanged;
        event Action<IMyTerminalBlock> PropertiesChanged;
        event Action<IMyTerminalBlock> ShowOnHUDChanged;
        event Action<IMyTerminalBlock> VisibilityChanged;
    }
}
