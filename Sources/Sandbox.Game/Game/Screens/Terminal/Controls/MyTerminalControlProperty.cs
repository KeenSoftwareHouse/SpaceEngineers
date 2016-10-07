using Sandbox.Game.Entities.Cube;
using Sandbox.Graphics.GUI;
using System;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces.Terminal;

namespace Sandbox.Game.Screens.Terminal.Controls
{
    public class MyTerminalControlProperty<TBlock, TValue> : MyTerminalValueControl<TBlock, TValue>, IMyTerminalControlProperty<TValue>, IMyTerminalValueControl<TValue>
        where TBlock : MyTerminalBlock
    {
        public MyTerminalControlProperty(string id) : base(id)
        {
            Visible = (x) => false;
        }

        public override TValue GetDefaultValue(TBlock block)
        {
            return default(TValue);
        }

        public override TValue GetMaximum(TBlock block)
        {
            return GetDefaultValue(block);
        }

        public override TValue GetMinimum(TBlock block)
        {
            return GetDefaultValue(block);
        }

        protected override MyGuiControlBase CreateGui()
        {
            return null;
        }
    }
}
