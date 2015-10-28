using Sandbox.Game.Entities.Cube;
using Sandbox.Game.Gui;
using Sandbox.Graphics.GUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage;
using VRage.Utils;

namespace Sandbox.Game.Screens.Terminal.Controls
{
    public class MyTerminalControlLabel<TBlock> : MyTerminalControl<TBlock>
        where TBlock : MyTerminalBlock
    {
        public readonly MyStringId Label;
        MyGuiControlLabel m_label;
        public MyTerminalControlLabel(MyStringId label)
            : base("Label")
        {
            Label = label;
        }

        protected override MyGuiControlBase CreateGui()
        {
            m_label = new MyGuiControlLabel();
            return new MyGuiControlBlockProperty(MyTexts.GetString(Label), null, m_label, MyGuiControlBlockPropertyLayoutEnum.Horizontal);
        }
    }
}
