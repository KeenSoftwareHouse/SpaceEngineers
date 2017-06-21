using Sandbox.Game.Entities.Cube;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sandbox.ModAPI.Ingame;
using VRage.Collections;

namespace Sandbox.Game.Gui
{
    public partial class MyTerminalAction<TBlock>
    {
        void ModAPI.Interfaces.ITerminalAction.Apply(VRage.Game.ModAPI.Ingame.IMyCubeBlock block)
        {
            if (block is TBlock)
                Apply(block as MyTerminalBlock);
        }

        void ModAPI.Interfaces.ITerminalAction.Apply(VRage.Game.ModAPI.Ingame.IMyCubeBlock block, ListReader<TerminalActionParameter> parameters)
        {
            if (block is TBlock)
                Apply(block as MyTerminalBlock, parameters);
        }

        void ModAPI.Interfaces.ITerminalAction.WriteValue(VRage.Game.ModAPI.Ingame.IMyCubeBlock block, StringBuilder appendTo)
        {
            if (block is TBlock)
                WriteValue(block as MyTerminalBlock, appendTo);
        }

        bool ModAPI.Interfaces.ITerminalAction.IsEnabled(VRage.Game.ModAPI.Ingame.IMyCubeBlock block)
        {
            if (block is TBlock)
                return IsEnabled(block as MyTerminalBlock);
            return false;
        }
    }
}
