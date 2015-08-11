using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sandbox.Game.Entities.Cube
{
    partial class MyFunctionalBlock : Sandbox.ModAPI.IMyFunctionalBlock
    {
        Action<MyTerminalBlock> GetDelegate(Action<ModAPI.IMyTerminalBlock> value)
        {
            return (Action<MyTerminalBlock>)Delegate.CreateDelegate(typeof(Action<MyTerminalBlock>), value.Target, value.Method);
        }

        event Action<ModAPI.IMyTerminalBlock> ModAPI.IMyFunctionalBlock.EnabledChanged
        {
            add { EnabledChanged += GetDelegate(value); }
            remove { EnabledChanged -= GetDelegate(value); }
        }

        void Sandbox.ModAPI.Ingame.IMyFunctionalBlock.RequestEnable(bool enable)
        {
            if (this.IsAccessibleForProgrammableBlock)
                RequestEnable(enable);
        }
    }
}
