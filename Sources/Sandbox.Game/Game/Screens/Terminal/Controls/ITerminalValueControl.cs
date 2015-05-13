using Sandbox.ModAPI.Interfaces;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.Gui;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sandbox.Game.Screens.Terminal.Controls
{
    interface ITerminalValueControl<TBlock, TValue> : ITerminalControl, ITerminalProperty<TValue>
        where TBlock : MyTerminalBlock
    {
        TValue GetValue(TBlock block);
        void SetValue(TBlock block, TValue value);

        TValue GetDefaultValue(TBlock block);
        TValue GetMininum(TBlock block);
        TValue GetMaximum(TBlock block);
    }
}
