using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sandbox.Game.Entities.Cube
{
    partial class MyFunctionalBlock : Sandbox.ModAPI.IMyFunctionalBlock
    {
        event Action<ModAPI.IMyTerminalBlock> ModAPI.IMyFunctionalBlock.EnabledChanged
        {
            add { EnabledChanged += GetDelegate(value); }
            remove { EnabledChanged -= GetDelegate(value); }
        }

        Action<MyTerminalBlock> GetDelegate(Action<ModAPI.IMyTerminalBlock> value)
        {
            return (Action<MyTerminalBlock>)Delegate.CreateDelegate(typeof(Action<MyTerminalBlock>), value.Target, value.Method);
        }

        void ModAPI.Ingame.IMyFunctionalBlock.RequestEnable(bool enable)
        {
            Enabled = enable;
        }
    }
}
