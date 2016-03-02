using Sandbox.ModAPI.Interfaces;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.Gui;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.Library.Collections;

namespace Sandbox.Game.Screens.Terminal.Controls
{
    interface ITerminalValueControl<TBlock, TValue> : ITerminalProperty<TValue>, ITerminalControl, ITerminalControlSync
        where TBlock : MyTerminalBlock
    {
        TValue GetValue(TBlock block);
        void SetValue(TBlock block, TValue value);

        TValue GetDefaultValue(TBlock block);
        [Obsolete("Use GetMinimum instead")]
        TValue GetMininum(TBlock block);
        TValue GetMinimum(TBlock block);
        TValue GetMaximum(TBlock block);

        void Serialize(BitStream stream, TBlock block);
    }
}
